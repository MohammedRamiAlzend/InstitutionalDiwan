import type { DragEvent } from 'react'

/** Custom MIME so a Drive item drag is distinguishable from file/text drags. */
export const DRIVE_ITEM_MIME = 'application/x-drive-item'

export function startItemDrag(event: DragEvent, itemId: string) {
  event.dataTransfer.setData(DRIVE_ITEM_MIME, itemId)
  event.dataTransfer.effectAllowed = 'move'
}

export function isItemDrag(event: DragEvent): boolean {
  return Array.from(event.dataTransfer.types).includes(DRIVE_ITEM_MIME)
}

export function readDraggedItemId(event: DragEvent): string | null {
  return event.dataTransfer.getData(DRIVE_ITEM_MIME) || null
}