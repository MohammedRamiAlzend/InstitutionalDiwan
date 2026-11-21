import { useState } from 'react'
import { toast } from 'sonner'
import type { DriveItemResponse } from '@/entities/drive-item'
import { t } from '@/shared/lib'
import { toArabicErrorMessage } from '@/shared/api'
import { Button, Dialog, Label } from '@/shared/ui'
import { useMoveItemMutation } from '../api/driveItemMutations'
import { FolderTreePicker } from './FolderTreePicker'

interface MoveItemDialogProps {
  item: DriveItemResponse
  open: boolean
  onOpenChange: (open: boolean) => void
}

/**
 * Moves a folder or file to another folder of the same space (or the root). The keyboard/touch
 * alternative to drag-and-drop; cross-space moves and cycles are rejected by the server.
 */
export function MoveItemDialog({ item, open, onOpenChange }: MoveItemDialogProps) {
  const mutation = useMoveItemMutation()
  const currentParentId = item.parentId ?? ''
  const [selectedId, setSelectedId] = useState(currentParentId)
  const unchanged = selectedId === currentParentId

  const handleMove = async () => {
    try {
      await mutation.mutateAsync({ itemId: item.itemId, newParentId: selectedId || null })
      toast.success(t('drive.toast.moved'))
      onOpenChange(false)
    } catch (error) {
      toast.error(toArabicErrorMessage(error))
    }
  }

  return (
    <Dialog
      open={open}
      onOpenChange={(nextOpen) => {
        if (!nextOpen && mutation.isPending) return
        onOpenChange(nextOpen)
      }}
      title={t('drive.dialog.move-title')}
      description={item.name}
      size="md"
      footer={
        <>
          <Button
            type="button"
            variant="outline"
            onClick={() => onOpenChange(false)}
            disabled={mutation.isPending}
          >
            <Label k="common.action.cancel" />
          </Button>
          <Button
            type="button"
            onClick={() => void handleMove()}
            loading={mutation.isPending}
            disabled={unchanged}
          >
            <Label k="drive.action.move" />
          </Button>
        </>
      }
    >
      <div className="flex flex-col gap-3">
        <p className="text-xs font-semibold text-muted">
          <Label k="drive.dialog.move-destination" />
        </p>
        <FolderTreePicker
          spaceId={item.spaceId}
          movingItemId={item.itemId}
          selectedId={selectedId}
          onSelect={setSelectedId}
        />
        <p className="text-xs text-muted">
          <Label k="drive.dialog.move-hint" />
        </p>
      </div>
    </Dialog>
  )
}