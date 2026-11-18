import { toast } from 'sonner'
import type { SavedSearchResponse } from '@/entities/archive'
import { toArabicErrorMessage } from '@/shared/api'
import { t } from '@/shared/lib'
import { ConfirmDialog } from '@/shared/ui'
import { useDeleteSavedSearchMutation } from '../api/archiveMutations'

interface DeleteSavedSearchDialogProps {
  search: SavedSearchResponse
  open: boolean
  onOpenChange: (open: boolean) => void
}

/** Confirmation gate before permanently deleting one of the caller's saved searches. */
export function DeleteSavedSearchDialog({
  search,
  open,
  onOpenChange,
}: DeleteSavedSearchDialogProps) {
  const mutation = useDeleteSavedSearchMutation()

  const confirm = async () => {
    try {
      await mutation.mutateAsync(search.savedSearchId)
      toast.success(t('archive.delete.toast', { name: search.name }))
      onOpenChange(false)
    } catch (error) {
      toast.error(toArabicErrorMessage(error))
    }
  }

  return (
    <ConfirmDialog
      open={open}
      onOpenChange={(nextOpen) => {
        if (!nextOpen && mutation.isPending) return
        onOpenChange(nextOpen)
      }}
      title={t('archive.delete.title')}
      description={t('archive.delete.description', { name: search.name })}
      confirmLabel={t('archive.delete.confirm')}
      cancelLabel={t('archive.dismiss')}
      onConfirm={() => void confirm()}
      loading={mutation.isPending}
      destructive
    />
  )
}
