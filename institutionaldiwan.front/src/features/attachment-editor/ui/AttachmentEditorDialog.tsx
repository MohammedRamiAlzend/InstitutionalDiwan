import { useCallback } from 'react'
import { Download } from 'lucide-react'
import { toast } from 'sonner'
import { type DocumentFileResponse, downloadFileVersion } from '@/entities/document-file'
import { t } from '@/shared/lib'
import { Button, Dialog, Label } from '@/shared/ui'
import { AttachmentEditor } from './AttachmentEditor'

interface AttachmentEditorDialogProps {
  file: DocumentFileResponse
  open: boolean
  onOpenChange: (open: boolean) => void
}

export function AttachmentEditorDialog({ file, open, onOpenChange }: AttachmentEditorDialogProps) {
  const closeEditor = useCallback(() => onOpenChange(false), [onOpenChange])

  const download = async () => {
    try {
      await downloadFileVersion(file)
    } catch {
      toast.error(t('attachment.editor.download-failed'))
    }
  }

  return (
    <Dialog
      open={open}
      onOpenChange={onOpenChange}
      title={file.title}
      description={t('attachment.editor.description', { version: file.currentVersionNumber })}
      size="xl"
      resizable
      className="h-[85dvh] w-[95vw]"
      footer={(
        <>
          <Button type="button" variant="outline" onClick={() => onOpenChange(false)}>
            <Label k="common.action.close" />
          </Button>
          <Button type="button" onClick={() => void download()}>
            <Download className="size-4" aria-hidden />
            <Label k="attachment.row.download" />
          </Button>
        </>
      )}
    >
      <div className="flex h-full min-h-48 w-full flex-col items-center justify-center">
        <AttachmentEditor file={file} open={open} onRequestClose={closeEditor} />
      </div>
    </Dialog>
  )
}
