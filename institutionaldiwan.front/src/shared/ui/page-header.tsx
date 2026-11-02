import type { ReactNode } from 'react'
import { cn } from '@/shared/lib'
import { PageGuideTrigger } from './guide'

interface PageHeaderProps {
  title: string
  description?: string
  actions?: ReactNode
  badge?: ReactNode
  /** Opens the tutorial walkthrough for this page id; silent when the mode is off. */
  pageGuide?: string
  className?: string
}

/** Consistent Arabic page heading with an optional action cluster. */
export function PageHeader({
  title,
  description,
  actions,
  badge,
  pageGuide,
  className,
}: PageHeaderProps) {
  return (
    <header
      className={cn(
        'flex flex-col gap-3 sm:flex-row sm:items-start sm:justify-between',
        className,
      )}
    >
      <div className="flex min-w-0 flex-col gap-1">
        <div className="flex flex-wrap items-center gap-2">
          <h1 className="text-2xl font-bold text-foreground">{title}</h1>
          {badge}
        </div>
        {description && <p className="max-w-2xl text-sm text-muted">{description}</p>}
      </div>
      {(pageGuide || actions) && (
        <div className="flex flex-wrap items-center gap-2 sm:justify-end">
          {pageGuide && <PageGuideTrigger pageId={pageGuide} />}
          {actions}
        </div>
      )}
    </header>
  )
}
