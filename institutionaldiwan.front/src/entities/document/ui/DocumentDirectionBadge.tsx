import { ArrowDownLeft, ArrowUpRight, RefreshCw } from 'lucide-react'
import { cn } from '@/shared/lib'

const directionMeta = {
  1: { label: 'داخلي', icon: RefreshCw, className: 'bg-surface-muted text-foreground' },
  2: { label: 'وارد', icon: ArrowDownLeft, className: 'bg-accent/10 text-accent' },
  3: { label: 'صادر', icon: ArrowUpRight, className: 'bg-success/10 text-success' },
} as const

interface DocumentDirectionBadgeProps {
  /** Backend `DocumentDirection` id (1 internal, 2 incoming, 3 outgoing). */
  directionId: number
  className?: string
}

export function DocumentDirectionBadge({
  directionId,
  className,
}: DocumentDirectionBadgeProps) {
  const meta = directionMeta[directionId as keyof typeof directionMeta]
  const Icon = meta?.icon

  return (
    <span
      className={cn(
        'inline-flex items-center gap-1 whitespace-nowrap rounded-md px-2 py-0.5 text-xs font-medium',
        meta ? meta.className : 'bg-surface-muted text-foreground',
        className,
      )}
    >
      {meta && Icon ? (
        <>
          <Icon className="size-3.5 rtl:-scale-x-100" aria-hidden />
          {meta.label}
        </>
      ) : (
        `اتجاه #${directionId}`
      )}
    </span>
  )
}
