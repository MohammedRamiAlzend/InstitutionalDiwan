import { cn } from '@/shared/lib'
import { findLookupFallback } from '../model/constants'
import { useLookupEntries } from '../model/queries'

interface LookupLabelProps {
  /** Catalog slug, e.g. `user-scope-types`. */
  lookup: string
  id: number | null | undefined
  /** Rendered when `id` is null/undefined. */
  emptyLabel?: string
  className?: string
}

/** Arabic label for one lookup id, with seeded fallback and a `#id` last resort. */
export function LookupLabel({
  lookup,
  id,
  emptyLabel = '—',
  className,
}: LookupLabelProps) {
  const { data } = useLookupEntries(lookup)

  if (id === null || id === undefined) {
    return <span className={cn('text-muted', className)}>{emptyLabel}</span>
  }

  const entry = data?.find((item) => item.id === id) ?? findLookupFallback(lookup, id)

  return (
    <span className={cn('text-sm text-foreground', className)}>
      {entry?.descAr ?? `#${id}`}
    </span>
  )
}
