import { Check, type LucideIcon } from 'lucide-react'
import type { LabelKey } from '@/shared/config'
import { cn } from '@/shared/lib'
import { Label } from '@/shared/ui'

interface ChoiceCardProps {
  icon: LucideIcon
  titleKey: LabelKey
  hintKey: LabelKey
  selected: boolean
  onSelect: () => void
  disabled?: boolean
}

/** A radio option drawn as a card; place inside an element with `role="radiogroup"`. */
export function ChoiceCard({ icon: Icon, titleKey, hintKey, selected, onSelect, disabled = false }: ChoiceCardProps) {
  return (
    <button
      type="button"
      role="radio"
      aria-checked={selected}
      onClick={onSelect}
      disabled={disabled}
      className={cn(
        'relative flex items-start gap-3 rounded-xl border p-3.5 pe-9 text-start transition-colors',
        'focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-focus-ring',
        'disabled:cursor-not-allowed disabled:opacity-60',
        selected ? 'border-accent bg-accent/10' : 'border-border bg-surface hover:bg-surface-muted/60',
      )}
    >
      <span
        className={cn(
          'flex size-9 shrink-0 items-center justify-center rounded-lg transition-colors',
          selected ? 'bg-accent text-accent-contrast' : 'bg-surface-muted text-muted',
        )}
      >
        <Icon className="size-4.5" aria-hidden />
      </span>

      <span className="flex min-w-0 flex-col gap-0.5">
        <span className="text-sm font-semibold text-foreground"><Label k={titleKey} /></span>
        <span className="text-xs leading-5 text-muted"><Label k={hintKey} /></span>
      </span>

      <span
        aria-hidden
        className={cn(
          'absolute end-3 top-3 flex size-4.5 items-center justify-center rounded-full border transition-colors',
          selected ? 'border-accent bg-accent text-accent-contrast' : 'border-border',
        )}
      >
        {selected && <Check className="size-3" />}
      </span>
    </button>
  )
}
