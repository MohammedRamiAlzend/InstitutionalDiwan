import { Star } from 'lucide-react'
import { cn, useLabel } from '@/shared/lib'

/** Read-only star state of a Drive item for the caller; the toggle action lives in the features. */
export function StarIndicator({ starred, className }: { starred: boolean; className?: string }) {
  const label = useLabel(starred ? 'drive.action.unstar' : 'drive.action.star')

  return (
    <Star
      aria-label={label}
      className={cn('size-4 shrink-0', starred ? 'fill-warning text-warning' : 'text-muted', className)}
    />
  )
}