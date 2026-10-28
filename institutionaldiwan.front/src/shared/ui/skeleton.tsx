import { cn } from '@/shared/lib'

/** Neutral loading placeholder in the content region (never a blank page). */
export function Skeleton({ className }: { className?: string }) {
  return (
    <div
      aria-hidden
      className={cn('animate-pulse rounded-md bg-surface-muted', className)}
    />
  )
}
