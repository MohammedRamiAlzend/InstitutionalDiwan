import { useEffect, useMemo, useRef, useState, type KeyboardEvent, type MouseEvent } from 'react'
import { Check, RotateCcw, X } from 'lucide-react'
import { toast } from 'sonner'
import { toArabicErrorMessage } from '@/shared/api'
import { cn, t } from '@/shared/lib'
import type { LabelEditorProps } from '@/shared/ui'
import { useSetUiLabelMutation, useResetUiLabelMutation } from '../api/uiLabelMutations'
import { uiLabelValueSchema, UI_LABEL_MAX_LENGTH } from '../model/schemas'

const ICON_BUTTON =
  'inline-flex size-7 shrink-0 cursor-pointer items-center justify-center rounded-md border border-border bg-surface text-foreground transition-colors hover:bg-surface-muted disabled:pointer-events-none disabled:opacity-60'

/**
 * The in-place label editor: swaps the label text for an input with save/cancel/reset.
 * Enter saves, Esc cancels, focus is trapped within the cluster, and clicks are kept from
 * bubbling to (or navigating) the surrounding link. Validation runs before any write.
 */
export function LabelEditor({
  labelKey,
  defaultText,
  initialText,
  hasOverride,
  onClose,
}: LabelEditorProps) {
  const rootRef = useRef<HTMLSpanElement>(null)
  const inputRef = useRef<HTMLInputElement>(null)
  const [value, setValue] = useState(initialText)
  const [error, setError] = useState<string | null>(null)

  const schema = useMemo(() => uiLabelValueSchema(defaultText), [defaultText])
  const setLabel = useSetUiLabelMutation()
  const resetLabel = useResetUiLabelMutation()
  const busy = setLabel.isPending || resetLabel.isPending

  useEffect(() => {
    inputRef.current?.focus()
    inputRef.current?.select()
  }, [])

  const save = async () => {
    const parsed = schema.safeParse(value)
    if (!parsed.success) {
      setError(parsed.error.issues[0].message)
      inputRef.current?.focus()
      return
    }
    setError(null)
    try {
      await setLabel.mutateAsync({ key: labelKey, value: parsed.data })
      toast.success(t('label-editor.toast-saved'))
      onClose()
    } catch (err) {
      setError(toArabicErrorMessage(err))
    }
  }

  const restore = async () => {
    setError(null)
    try {
      await resetLabel.mutateAsync(labelKey)
      toast.success(t('label-editor.toast-reset'))
      onClose()
    } catch (err) {
      setError(toArabicErrorMessage(err))
    }
  }

  const onKeyDown = (event: KeyboardEvent<HTMLSpanElement>) => {
    event.stopPropagation()
    if (event.key === 'Escape') {
      event.preventDefault()
      onClose()
      return
    }
    if (event.key === 'Enter' && document.activeElement === inputRef.current) {
      event.preventDefault()
      void save()
      return
    }
    if (event.key === 'Tab') {
      const nodes = rootRef.current?.querySelectorAll<HTMLElement>(
        'input, button:not([disabled])',
      )
      if (!nodes || nodes.length === 0) return
      const list = Array.from(nodes)
      const first = list[0]
      const last = list[list.length - 1]
      if (event.shiftKey && document.activeElement === first) {
        event.preventDefault()
        last.focus()
      } else if (!event.shiftKey && document.activeElement === last) {
        event.preventDefault()
        first.focus()
      }
    }
  }

  // The label often sits inside a NavLink: cancel both the router handler (stopPropagation)
  // and the anchor's default navigation (preventDefault), then restore focus manually since
  // preventDefault also suppresses the browser's natural focus-on-click.
  const onClick = (event: MouseEvent<HTMLSpanElement>) => {
    event.preventDefault()
    event.stopPropagation()
    const target = event.target as HTMLElement
    if (target.tagName === 'BUTTON') (target as HTMLButtonElement).focus()
    else inputRef.current?.focus()
  }

  return (
    <span
      ref={rootRef}
      role="group"
      aria-label={t('label-editor.group-aria')}
      className="inline-flex w-full min-w-0 flex-col gap-1"
      onClick={onClick}
      onKeyDown={onKeyDown}
    >
      <span className="flex w-full min-w-0 items-center gap-1">
        <input
          ref={inputRef}
          type="text"
          value={value}
          disabled={busy}
          maxLength={UI_LABEL_MAX_LENGTH}
          aria-label={t('label-editor.input-aria')}
          aria-invalid={error ? true : undefined}
          onChange={(event) => {
            setValue(event.target.value)
            if (error) setError(null)
          }}
          className={cn(
            'h-8 min-w-0 flex-1 rounded-md border bg-surface px-2 text-sm text-foreground transition-colors focus:border-focus-ring focus:outline-none',
            error ? 'border-danger' : 'border-border',
          )}
        />
        <button
          type="button"
          className={cn(ICON_BUTTON, 'border-accent bg-accent text-accent-contrast hover:bg-accent-hover')}
          disabled={busy}
          aria-label={t('common.action.save')}
          title={t('label-editor.save-title')}
          onClick={() => void save()}
        >
          <Check className="size-4" aria-hidden />
        </button>
        {hasOverride && (
          <button
            type="button"
            className={ICON_BUTTON}
            disabled={busy}
            aria-label={t('label-editor.reset-aria')}
            title={t('label-editor.reset-title')}
            onClick={() => void restore()}
          >
            <RotateCcw className="size-4" aria-hidden />
          </button>
        )}
        <button
          type="button"
          className={ICON_BUTTON}
          disabled={busy}
          aria-label={t('common.action.cancel')}
          title={t('label-editor.cancel-title')}
          onClick={onClose}
        >
          <X className="size-4" aria-hidden />
        </button>
      </span>
      {error && (
        <span role="alert" className="text-[11px] leading-tight text-danger">
          {error}
        </span>
      )}
    </span>
  )
}