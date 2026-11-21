import { cn, formatFileSize, useLabel } from '@/shared/lib'

interface QuotaMeterProps {
  usedBytes: number
  /** The effective quota in force (override ?? institution default); `<= 0` reads as unlimited. */
  quotaBytes: number
  className?: string
}

/**
 * Storage usage of one Drive space. Trash keeps counting toward the quota until permanent purge, so
 * this meter reflects `usedBytes` as the server reports it. Latin-digit sizes with Arabic units.
 */
export function QuotaMeter({ usedBytes, quotaBytes, className }: QuotaMeterProps) {
  const unlimited = quotaBytes <= 0
  const percent = unlimited ? 0 : Math.min(100, Math.max(0, Math.round((usedBytes / quotaBytes) * 100)))
  const full = !unlimited && percent >= 100

  const title = useLabel('drive.quota')
  const unlimitedLabel = useLabel('drive.quota.unlimited')
  const quotaText = unlimited ? unlimitedLabel : formatFileSize(quotaBytes)
  const usage = useLabel('drive.quota.usage', { used: formatFileSize(usedBytes), quota: quotaText })
  const percentLabel = useLabel('drive.quota.percent', { percent: String(percent) })
  const fullLabel = useLabel('drive.quota.full')

  return (
    <div className={cn('flex flex-col gap-1', className)}>
      <div className="flex items-center justify-between text-xs text-muted">
        <span>{title}</span>
        <span dir="ltr">{unlimited ? usage : percentLabel}</span>
      </div>
      <div
        role="progressbar"
        aria-valuenow={percent}
        aria-valuemin={0}
        aria-valuemax={100}
        aria-label={title}
        className="h-1.5 w-full overflow-hidden rounded-full bg-surface-muted"
      >
        <div
          className={cn('h-full rounded-full transition-[width]', full ? 'bg-danger' : 'bg-accent')}
          style={{ width: `${unlimited ? 0 : percent}%` }}
        />
      </div>
      <div className="flex items-center justify-between text-xs">
        <span className="text-muted" dir="ltr">{usage}</span>
        {full ? <span className="text-danger">{fullLabel}</span> : null}
      </div>
    </div>
  )
}