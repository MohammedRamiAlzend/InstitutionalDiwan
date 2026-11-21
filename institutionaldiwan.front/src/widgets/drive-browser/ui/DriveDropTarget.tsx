import { useState, type ReactNode } from 'react'
import { cn } from '@/shared/lib'
import { isItemDrag, readDraggedItemId } from '../model/dnd'

interface DriveDropTargetProps {
  /** `null` drops at the space root; otherwise the destination folder id. */
  targetFolderId: string | null
  onDropItem: (itemId: string, targetFolderId: string | null) => void
  disabled?: boolean
  className?: string
  children: ReactNode
}

/**
 * A folder drop zone for drag-and-drop moves (a folder row/card or a breadcrumb crumb). Highlights
 * while an item drag hovers; dropping onto the dragged item itself is ignored (the server also
 * rejects cycles).
 */
export function DriveDropTarget({
  targetFolderId,
  onDropItem,
  disabled = false,
  className,
  children,
}: DriveDropTargetProps) {
  const [over, setOver] = useState(false)

  return (
    <div
      className={cn(
        className,
        over && 'rounded-lg ring-2 ring-accent ring-offset-1 ring-offset-surface',
      )}
      onDragOver={(event) => {
        if (disabled || !isItemDrag(event)) return
        event.preventDefault()
        event.dataTransfer.dropEffect = 'move'
        setOver(true)
      }}
      onDragLeave={() => setOver(false)}
      onDrop={(event) => {
        if (disabled || !isItemDrag(event)) return
        const itemId = readDraggedItemId(event)
        setOver(false)
        if (!itemId || itemId === targetFolderId) return
        event.preventDefault()
        onDropItem(itemId, targetFolderId)
      }}
    >
      {children}
    </div>
  )
}