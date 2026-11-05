import { useEffect, useRef } from 'react'

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
