import { toast } from 'sonner'
import type { DocumentFileResponse } from '@/entities/document-file'
import { toArabicErrorMessage } from '@/shared/api'
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
      toast.success(`أُرشف الملف «${file.title}»؛ لا تُقبل إصدارات جديدة له.`)
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
      title="أرشفة الملف"
      description={`أرشفة «${file.title}» تمنع رفع إصدارات جديدة له. إصداراته الحالية تبقى قابلة للقراءة والتنزيل ولا يُحذف منها شيء، ولا يمكن التراجع عن الأرشفة من هذه الواجهة.`}
      confirmLabel="أرشف الملف"
      cancelLabel="إلغاء"
      onConfirm={() => void confirm()}
      loading={archiveMutation.isPending}
      destructive
    />
  )
}
