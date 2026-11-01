import { Badge, type BadgeVariant } from '@/shared/ui'

interface ChecksumBadgeProps {
  /**
   * Result of a checksum comparison, or `null`/`undefined` when none was made yet — either the
   * server verify read or the local SHA-256 against the stored one.
   */
  isMatch?: boolean | null
  className?: string
}

const meta: Record<'match' | 'mismatch' | 'unknown', { label: string; variant: BadgeVariant }> = {
  match: { label: 'بصمة مطابقة', variant: 'success' },
  mismatch: { label: 'بصمة غير مطابقة', variant: 'danger' },
  unknown: { label: 'لم يُتحقق منها', variant: 'outline' },
}

/** SHA-256 integrity state of a document file version. */
export function ChecksumBadge({ isMatch, className }: ChecksumBadgeProps) {
  const resolved = isMatch === true ? 'match' : isMatch === false ? 'mismatch' : 'unknown'
  const { label, variant } = meta[resolved]

  return (
    <Badge variant={variant} className={className}>
      {label}
    </Badge>
  )
}
