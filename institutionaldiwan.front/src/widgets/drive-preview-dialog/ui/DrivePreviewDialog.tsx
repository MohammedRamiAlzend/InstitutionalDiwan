import { useCallback, useState } from 'react'
import { Download, FileWarning, Loader2 } from 'lucide-react'
import { toast } from 'sonner'
import { useDriveDownloadUrl, type DriveItemResponse } from '@/entities/drive-item'
import { DriveEditor } from '@/features/drive-editor'
import { downloadDriveFile } from '@/features/drive-upload'
import { toArabicErrorMessage } from '@/shared/api'
import { t } from '@/shared/lib'
import { Button, Dialog, GuideRegion, Label } from '@/shared/ui'
import { drivePreviewKind, isUrlPreviewable } from '../model/previewKind'

interface DrivePreviewDialogProps {
  item: DriveItemResponse
  open: boolean
  onOpenChange: (open: boolean) => void
}

/**
 * In-browser preview of a Drive file. Images/PDF/video/audio render from a short-lived presigned GET
 * (the gateway serves bytes directly, with Range for video); Office and other types fall back to an
 * Arabic «no preview» notice with a download. The OnlyOffice viewer for Office lands in Phase 6.
 */
export function DrivePreviewDialog({ item, open, onOpenChange }: DrivePreviewDialogProps) {
  const kind = drivePreviewKind(item.name)
  const isOffice = kind === 'office'
  const previewable = isUrlPreviewable(kind)
  const [isDownloading, setIsDownloading] = useState(false)

  const urlQuery = useDriveDownloadUrl(item.itemId, { enabled: open && previewable })

  const handleDownload = async () => {
    setIsDownloading(true)
    try {
      await downloadDriveFile(item)
    } catch (error) {
      toast.error(toArabicErrorMessage(error))
    } finally {
      setIsDownloading(false)
    }
  }

  const url = urlQuery.data?.url
  const closeEditor = useCallback(() => onOpenChange(false), [onOpenChange])

  return (
    <Dialog
      open={open}
      onOpenChange={onOpenChange}
      title={item.name}
      description={t('drive.dialog.preview-title')}
      size="lg"
      footer={
        <>
          <Button type="button" variant="outline" onClick={() => onOpenChange(false)}>
            <Label k="common.action.close" />
          </Button>
          <Button type="button" onClick={() => void handleDownload()} loading={isDownloading}>
            <Download className="size-4" aria-hidden />
            <Label k="drive.action.download" />
          </Button>
        </>
      }
    >
      <div className="flex min-h-48 flex-col items-center justify-center gap-3">
        {isOffice ? (
          <GuideRegion id="drive.editor">
            <DriveEditor item={item} open={open} onRequestClose={closeEditor} />
          </GuideRegion>
        ) : !previewable ? (
          <div className="flex flex-col items-center gap-3 py-8 text-center">
            <FileWarning className="size-10 text-muted" aria-hidden />
            <p className="max-w-md text-sm text-muted">
              <Label k="drive.preview.unsupported" />
            </p>
          </div>
        ) : urlQuery.isLoading ? (
          <div className="flex items-center gap-2 py-8 text-sm text-muted">
            <Loader2 className="size-5 animate-spin" aria-hidden />
            <Label k="drive.state.loading" />
          </div>
        ) : urlQuery.error || !url ? (
          <div className="flex flex-col items-center gap-3 py-8 text-center">
            <p className="max-w-md text-sm text-danger">
              {urlQuery.error ? toArabicErrorMessage(urlQuery.error) : t('drive.preview.link-expired')}
            </p>
            <Button type="button" variant="outline" onClick={() => void urlQuery.refetch()}>
              <Label k="drive.retry" />
            </Button>
          </div>
        ) : kind === 'image' ? (
          <img src={url} alt={item.name} className="max-h-[70vh] max-w-full rounded-lg object-contain" />
        ) : kind === 'pdf' ? (
          <iframe src={url} title={item.name} className="h-[70vh] w-full rounded-lg border border-border" />
        ) : kind === 'video' ? (
          <video src={url} controls className="max-h-[70vh] w-full rounded-lg" />
        ) : (
          <audio src={url} controls className="w-full" />
        )}
      </div>
    </Dialog>
  )
}