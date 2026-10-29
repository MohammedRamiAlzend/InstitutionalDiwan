import { cn } from '@/shared/lib'

interface HashChainPillProps {
  /** Full 64-character hex hash; the component truncates it for display. */
  hash: string
  /** Accessible label, e.g. «تجزئة الحدث». */
  label: string
  className?: string
}

/** Truncated LTR monospace hash; the full value stays available via `title`. */
export function HashChainPill({ hash, label, className }: HashChainPillProps) {
  return (
    <span
      dir="ltr"
      title={hash}
      aria-label={`${label}: ${hash}`}
      className={cn(
        'inline-flex max-w-full items-center rounded-md bg-surface-muted px-2 py-0.5 font-mono text-xs text-muted',
        className,
      )}
    >
      <span className="truncate">
        {hash.slice(0, 10)}…{hash.slice(-6)}
      </span>
    </span>
  )
}
