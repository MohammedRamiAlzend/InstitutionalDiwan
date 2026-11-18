import type { LabelKey } from '@/shared/config'
import { useLabel } from '@/shared/lib'
import { Badge, type BadgeVariant } from '@/shared/ui'

interface ChecksumBadgeProps {
  /**
   * Result of a checksum comparison, or `null`/`undefined` when none was made yet — either the
   * server verify read or the local SHA-256 against the stored one.
   */
  isMatch?: boolean | null
  className?: string
}

const meta: Record<'match' | 'mismatch' | 'unknown', { key: LabelKey; variant: BadgeVariant }> = {
  match: { key: 'attachment.checksum.match', variant: 'success' },
  mismatch: { key: 'attachment.checksum.mismatch', variant: 'danger' },
  unknown: { key: 'attachment.checksum.unknown', variant: 'outline' },
}

/** SHA-256 integrity state of a document file version. */
export function ChecksumBadge({ isMatch, className }: ChecksumBadgeProps) {
  const resolved = isMatch === true ? 'match' : isMatch === false ? 'mismatch' : 'unknown'
  const { key, variant } = meta[resolved]
  const label = useLabel(key)

  return (
    <Badge variant={variant} className={className}>
      {label}
    </Badge>
  )
}
