import type { LucideIcon } from 'lucide-react'
import type { ReactNode } from 'react'
import { cn } from '@/shared/lib'

interface FormSectionProps {
  icon: LucideIcon
  title: string
  hint?: string
  /** Sits at the far end of the header: a badge or the switch that opens the section. */
  aside?: ReactNode
  children?: ReactNode
  className?: string
}

/** One titled group of the create form. Without children only its header shows. */
export function FormSection({ icon: Icon, title, hint, aside, children, className }: FormSectionProps) {
  return (
    <section className={cn('rounded-xl border border-border bg-surface', className)}>
      <header className="flex items-center gap-3 px-4 py-3">
        <span className="flex size-9 shrink-0 items-center justify-center rounded-lg bg-accent/10 text-accent">
          <Icon className="size-4.5" aria-hidden />
        </span>
        <div className="flex min-w-0 flex-1 flex-col">
          <h3 className="text-sm font-semibold text-foreground">{title}</h3>
          {hint && <p className="text-xs leading-5 text-muted">{hint}</p>}
        </div>
        {aside && <div className="shrink-0">{aside}</div>}
      </header>

      {children && <div className="grid gap-4 border-t border-border p-4 sm:grid-cols-2">{children}</div>}
    </section>
  )
}
