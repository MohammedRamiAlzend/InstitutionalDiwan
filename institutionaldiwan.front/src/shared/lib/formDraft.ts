import { useCallback, useEffect, useRef, useState } from 'react'
import {
  useForm,
  useFormState,
  useWatch,
  type FieldValues,
  type UseFormProps,
} from 'react-hook-form'
import { toast } from 'sonner'

let draftScope = 'anonymous'

/** Set by the app layer when the signed-in user changes, so drafts never leak between users. */
export function setFormDraftScope(userId: string | null | undefined): void {
  draftScope = userId ?? 'anonymous'
}

/** Saved form drafts live in `localStorage` as `diwan.formDraft.<formId>.<userId>.v1`. */
export function formDraftKey(formId: string, userId: string | null | undefined): string {
  return `diwan.formDraft.${formId}.${userId ?? 'anonymous'}.v1`
}

/** Reads a saved draft; returns `null` when missing, unreadable or storage is unavailable. */
export function readFormDraft<T>(key: string): T | null {
  if (typeof localStorage === 'undefined') return null
  try {
    const raw = localStorage.getItem(key)
    return raw ? (JSON.parse(raw) as T) : null
  } catch {
    return null
  }
}

export function writeFormDraft(key: string, value: unknown): void {
  if (typeof localStorage === 'undefined') return
  try {
    localStorage.setItem(key, JSON.stringify(value))
  } catch {
    // Quota or private-mode failures only cost the convenience of the draft.
  }
}

export function clearFormDraft(key: string): void {
  if (typeof localStorage === 'undefined') return
  try {
    localStorage.removeItem(key)
  } catch {
    // Nothing to do: the draft simply stays until the next successful clear.
  }
}

/**
 * Saves `value` under `key` shortly after it stops changing, so a form that is closed by mistake
 * can be restored. Pass `enabled = false` once the form is submitted to stop saving.
 */
export function useFormDraftAutosave(
  key: string,
  value: unknown,
  enabled: boolean,
  delay = 400,
): { discard: () => void } {
  const latest = useRef({ key, value, enabled })
  const discarded = useRef(false)
  useEffect(() => {
    latest.current = { key, value, enabled }
  })

  const serialized = JSON.stringify(value)
  useEffect(() => {
    if (!enabled || discarded.current) return
    const timer = window.setTimeout(() => writeFormDraft(key, JSON.parse(serialized)), delay)
    return () => window.clearTimeout(timer)
  }, [key, serialized, enabled, delay])

  // Closing right after typing must not lose the last debounce window.
  useEffect(
    () => () => {
      const current = latest.current
      if (current.enabled && !discarded.current) writeFormDraft(current.key, current.value)
    },
    [],
  )

  /** Deletes the saved draft and stops every later write (used after a successful submit). */
  const discard = () => {
    discarded.current = true
    clearFormDraft(key)
  }
  return { discard }
}

/** Never persisted: credentials, and files (not JSON-serialisable). */
function draftReplacer(key: string, value: unknown): unknown {
  if (/password|secret|token/i.test(key)) return undefined
  if (typeof Blob !== 'undefined' && value instanceof Blob) return undefined
  return value
}

interface DraftFormOptions {
  /** Stable id of the form; add the edited entity id for edit forms (`department:<id>`). */
  formId: string
  /** Hold restore/save until async data the form depends on has loaded. Default `true`. */
  enabled?: boolean
}

/**
 * `useForm` that keeps unsent input in `localStorage` (per user and form id) and restores it
 * after an accidental close, with a toast offering to start over. Call `draft.discard()` only
 * after a successful submit; cancelling or closing keeps the draft.
 */
export function useDraftForm<T extends FieldValues>(
  options: DraftFormOptions,
  props: UseFormProps<T>,
) {
  const form = useForm<T>(props)
  const { control, reset, getValues } = form
  const key = formDraftKey(options.formId, draftScope)
  const enabled = options.enabled ?? true

  const { isDirty } = useFormState({ control })
  const watched = useWatch({ control })
  const serialized = JSON.stringify(watched, draftReplacer)

  const [ready, setReady] = useState(false)
  const discarded = useRef(false)
  const latest = useRef({ key, serialized, isDirty, ready })
  useEffect(() => {
    latest.current = { key, serialized, isDirty, ready }
  })

  // Restore once the form (and any async data behind it) is ready. Deferred a tick so that
  // effects that reset the form from loaded data run first and cannot overwrite the draft.
  useEffect(() => {
    if (!enabled || ready) return
    const timer = window.setTimeout(() => {
      const saved = readFormDraft<Partial<T>>(key)
      if (saved && Object.keys(saved).length > 0) {
        reset({ ...getValues(), ...saved } as T, { keepDefaultValues: true })
        toast.info('استُعيدت بياناتك غير المرسلة.', {
          action: {
            label: 'مسح وبدء من جديد',
            onClick: () => {
              clearFormDraft(key)
              reset()
            },
          },
        })
      }
      setReady(true)
    }, 0)
    return () => window.clearTimeout(timer)
  }, [enabled, ready, key, reset, getValues])

  useEffect(() => {
    if (!ready || discarded.current) return
    if (!isDirty) {
      clearFormDraft(key)
      return
    }
    const timer = window.setTimeout(() => {
      if (!discarded.current) writeFormDraft(key, JSON.parse(serialized))
    }, 400)
    return () => window.clearTimeout(timer)
  }, [ready, isDirty, key, serialized])

  // Closing right after typing must not lose the last debounce window.
  useEffect(
    () => () => {
      const current = latest.current
      if (current.ready && current.isDirty && !discarded.current) {
        writeFormDraft(current.key, JSON.parse(current.serialized))
      }
    },
    [],
  )

  const discard = useCallback(() => {
    discarded.current = true
    clearFormDraft(key)
  }, [key])

  return { ...form, draft: { discard } }
}
