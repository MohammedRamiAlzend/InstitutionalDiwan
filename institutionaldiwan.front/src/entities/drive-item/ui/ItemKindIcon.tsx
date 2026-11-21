import { FileText, Folder } from 'lucide-react'
import { DRIVE_ITEM_KIND } from '@/shared/config'
import { cn, useLabel } from '@/shared/lib'

interface ItemKindIconProps {
  kindId: number
  className?: string
}

/** Folder/file glyph for a Drive node; the Arabic kind name is the accessible label. */
export function ItemKindIcon({ kindId, className }: ItemKindIconProps) {
  const isFolder = kindId === DRIVE_ITEM_KIND.Folder
  const label = useLabel(isFolder ? 'drive.item-kind.folder' : 'drive.item-kind.file')
  const Icon = isFolder ? Folder : FileText

  return (
    <Icon
      aria-label={label}
      className={cn('size-4 shrink-0', isFolder ? 'text-accent' : 'text-muted', className)}
    />
  )
}