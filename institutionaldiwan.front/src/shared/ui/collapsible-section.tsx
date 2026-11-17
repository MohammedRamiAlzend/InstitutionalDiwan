import { useState, type ReactNode } from 'react'
import { ChevronDown } from 'lucide-react'
import { cn } from '@/shared/lib'

interface CollapsibleSectionProps {
  title: string
  /** Short hint under the title, kept on one or two lines. */
  hint?: string
  /** Icon rendered before the title. */
  icon?: ReactNode
  /** Interactive controls on the far side of the header (outside the toggle button). */
  actions?: ReactNode
  /** Start expanded; the body is hidden with CSS so its state survives collapsing. */
  defaultOpen?: boolean
  /** Controlled mode: when given, the caller owns the open state. */
  open?: boolean
  onOpenChange?: (open: boolean) => void
  id?: string
  className?: string
  children: ReactNode
}

/**
 * A bordered section whose header toggles its body. Children stay mounted while collapsed,
 * so forms and queries inside keep their state when the section is reopened.
 */
export function CollapsibleSection({
  title,
  hint,
  icon,
  actions,
  defaultOpen = true,
  open: controlledOpen,
  onOpenChange,
  id,
  className,
  children,
}: CollapsibleSectionProps) {
  const [internalOpen, setInternalOpen] = useState(defaultOpen)
  const open = controlledOpen ?? internalOpen

  const toggle = () => {
    const next = !open
    onOpenChange?.(next)
    setInternalOpen(next)
  }

  return (
    <section id={id} className={cn('rounded-xl border border-border bg-surface', className)}>
      <div className="flex items-center justify-between gap-3 p-5">
        <button
          type="button"
          aria-expanded={open}
          onClick={toggle}
          className="flex min-w-0 flex-1 items-center gap-2 text-start"
        >
          {icon && <span className="shrink-0 text-accent" aria-hidden>{icon}</span>}
          <span className="flex min-w-0 flex-col gap-0.5">
            <span className="text-base font-semibold text-foreground">{title}</span>
            {hint && <span className="text-xs text-muted">{hint}</span>}
          </span>
          <ChevronDown
            className={cn(
              'size-4 shrink-0 text-muted transition-transform',
              open && 'rotate-180',
            )}
            aria-hidden
          />
        </button>
        {actions}
      </div>
      <div
        className={cn(
          'grid transition-[grid-template-rows,visibility] duration-200 ease-out',
          open ? 'grid-rows-[1fr]' : 'invisible grid-rows-[0fr]',
        )}
      >
        <div className="overflow-hidden">
          <div className="border-t border-border px-5 py-4">{children}</div>
        </div>
      </div>
    </section>
  )
}
