import type { ReactNode } from 'react'
import type { LabelKey } from '@/shared/config'
import { cn } from '@/shared/lib'
import { PageGuideTrigger } from './guide'
import { Label } from './label'

interface PageHeaderProps {
  title?: string
  /** Catalog key for the title; takes priority over `title` and is editable in place. */
  titleKey?: LabelKey
  description?: string
  /** Catalog key for the description; takes priority over `description`. */
  descriptionKey?: LabelKey
  actions?: ReactNode
  badge?: ReactNode
  /** Opens the tutorial walkthrough for this page id; silent when the mode is off. */
  pageGuide?: string
  className?: string
}

/** Consistent Arabic page heading with an optional action cluster. */
export function PageHeader({
  title,
  titleKey,
  description,
  descriptionKey,
  actions,
  badge,
  pageGuide,
  className,
}: PageHeaderProps) {
  const titleContent = titleKey ? <Label k={titleKey} /> : title
  const descriptionContent = descriptionKey ? <Label k={descriptionKey} /> : description

  return (
    <header
      className={cn(
        'flex flex-col gap-3 sm:flex-row sm:items-start sm:justify-between',
        className,
      )}
    >
      <div className="flex min-w-0 flex-col gap-1">
        <div className="flex flex-wrap items-center gap-2">
          <h1 className="text-2xl font-bold text-foreground">{titleContent}</h1>
          {badge}
        </div>
        {descriptionContent && (
          <p className="max-w-2xl text-sm text-muted">{descriptionContent}</p>
        )}
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