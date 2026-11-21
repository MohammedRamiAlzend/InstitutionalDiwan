import { DRIVE_VERSION_SOURCE, type LabelKey } from '@/shared/config'
import { useLabel } from '@/shared/lib'
import { Badge, type BadgeVariant } from '@/shared/ui'

const meta: Record<number, { key: LabelKey; variant: BadgeVariant }> = {
  [DRIVE_VERSION_SOURCE.Upload]: { key: 'drive.version-source.upload', variant: 'neutral' },
  [DRIVE_VERSION_SOURCE.EditorSave]: { key: 'drive.version-source.editor-save', variant: 'accent' },
  [DRIVE_VERSION_SOURCE.Restore]: { key: 'drive.version-source.restore', variant: 'neutral' },
}

/** How a committed version came to be: upload / editor save / restore-by-copy. */
export function VersionSourceBadge({ sourceId, className }: { sourceId: number; className?: string }) {
  const { key, variant } = meta[sourceId] ?? meta[DRIVE_VERSION_SOURCE.Upload]
  const label = useLabel(key)

  return (
    <Badge variant={variant} className={className}>
      {label}
    </Badge>
  )
}