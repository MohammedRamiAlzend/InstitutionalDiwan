import { toast } from 'sonner'
import type { DocumentFileResponse } from '@/entities/document-file'
import { toArabicErrorMessage } from '@/shared/api'
import { t } from '@/shared/lib'
import { ConfirmDialog } from '@/shared/ui'
import { useArchiveFileMutation } from '../api/attachmentMutations'

interface ArchiveFileDialogProps {
  file: DocumentFileResponse
  open: boolean
  onOpenChange: (open: boolean) => void
}

/**
 * The confirmation gate before archiving. Archiving is one-way from this screen — there is no
 * un-archive endpoint — and it stops only new versions: reading and downloading what already exists
 * keeps working, which is what the warning has to say plainly.
 */
export function ArchiveFileDialog({ file, open, onOpenChange }: ArchiveFileDialogProps) {
  const archiveMutation = useArchiveFileMutation()

  const confirm = async () => {
    try {
      await archiveMutation.mutateAsync(file.fileId)
      toast.success(t('attachment.archive.toast', { title: file.title }))
      onOpenChange(false)
    } catch (error) {
      // Stays open so the reason is readable: an archived file is already archived, and a document
      // the caller cannot manage comes back as an access denial.
      toast.error(toArabicErrorMessage(error))
    }
  }

  return (
    <ConfirmDialog
      open={open}
      onOpenChange={(nextOpen) => {
        if (!nextOpen && archiveMutation.isPending) return
        onOpenChange(nextOpen)
      }}
      title={t('attachment.row.archive')}
      description={t('attachment.archive.description', { title: file.title })}
      confirmLabel={t('attachment.archive.confirm')}
      cancelLabel={t('common.action.cancel')}
      onConfirm={() => void confirm()}
      loading={archiveMutation.isPending}
      destructive
    />
  )
}
