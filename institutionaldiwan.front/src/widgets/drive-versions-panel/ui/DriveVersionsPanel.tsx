import { useState } from 'react'
import { Download, RotateCcw } from 'lucide-react'
import { toast } from 'sonner'
import {
  useDriveVersions,
  useDriveEffectiveRole,
  type DriveItemResponse,
  type DriveVersionResponse,
} from '@/entities/drive-item'
import { useRestoreDriveVersionMutation } from '@/features/drive-item-manage'
import { downloadBlob, toArabicErrorMessage } from '@/shared/api'
import { DRIVE_ROLE } from '@/shared/config'
import { formatDateTime, formatFileSize, parseContentDisposition, saveBlobAsFile, t } from '@/shared/lib'
import { Badge, Button, Dialog, Label } from '@/shared/ui'

interface DriveVersionsPanelProps {
  item: DriveItemResponse
  open: boolean
  onOpenChange: (open: boolean) => void
}

export function DriveVersionsPanel({ item, open, onOpenChange }: DriveVersionsPanelProps) {
  const query = useDriveVersions(item.itemId, { enabled: open })
  const roleQuery = useDriveEffectiveRole(item.itemId, { enabled: open })
  const restore = useRestoreDriveVersionMutation(item.itemId)
  const [downloading, setDownloading] = useState<string | null>(null)

  const downloadVersion = async (version: DriveVersionResponse) => {
    setDownloading(version.versionId)
    try {
      const result = await downloadBlob(`/drive/files/${item.itemId}/versions/${version.versionId}/content`)
      saveBlobAsFile(result.blob, parseContentDisposition(result.contentDisposition) ?? item.name)
    } catch (error) {
      toast.error(toArabicErrorMessage(error))
    } finally {
      setDownloading(null)
    }
  }

  const restoreVersion = async (version: DriveVersionResponse) => {
    try {
      await restore.mutateAsync(version.versionId)
      toast.success(t('drive.toast.version-restored'))
    } catch (error) {
      toast.error(toArabicErrorMessage(error))
    }
  }

  return (
    <Dialog
      open={open}
      onOpenChange={onOpenChange}
      title={t('drive.dialog.versions-title')}
      description={item.name}
      size="lg"
      footer={<Button type="button" onClick={() => onOpenChange(false)}><Label k="common.action.close" /></Button>}
    >
      {query.isLoading ? <p className="py-8 text-center text-sm text-muted"><Label k="drive.state.loading" /></p> : query.error ? (
        <div className="flex flex-col items-center gap-3 py-8">
          <p className="text-sm text-danger">{toArabicErrorMessage(query.error)}</p>
          <Button variant="outline" onClick={() => void query.refetch()}><Label k="drive.retry" /></Button>
        </div>
      ) : (query.data ?? []).length === 0 ? (
        <p className="py-8 text-center text-sm text-muted"><Label k="drive.browser.empty-title" /></p>
      ) : (
        <ul className="flex flex-col gap-3">
          {(query.data ?? []).map((version) => (
            <li key={version.versionId} className="flex flex-wrap items-center justify-between gap-3 rounded-lg border border-border bg-surface-muted/20 p-3">
              <div className="min-w-0">
                <p className="flex flex-wrap items-center gap-2 text-sm font-semibold">
                  <span dir="ltr">{t('drive.version.number', { number: version.versionNumber })}</span>
                  {version.isCurrent && <Badge variant="success"><Label k="drive.version.current" /></Badge>}
                  <Badge variant="outline"><Label k={`drive.version-source.${version.sourceId === 2 ? 'editor-save' : version.sourceId === 3 ? 'restore' : 'upload'}`} /></Badge>
                </p>
                <p className="mt-1 text-xs text-muted">
                  {formatFileSize(version.sizeBytes)} · {formatDateTime(version.createdAt)}
                </p>
              </div>
              <div className="flex gap-2">
                <Button variant="outline" size="sm" onClick={() => void downloadVersion(version)} loading={downloading === version.versionId}>
                  <Download className="size-4" aria-hidden /><Label k="drive.action.download" />
                </Button>
                {!version.isCurrent && (roleQuery.data?.roleId ?? DRIVE_ROLE.None) >= DRIVE_ROLE.Editor && (
                  <Button variant="outline" size="sm" onClick={() => void restoreVersion(version)} loading={restore.isPending}>
                    <RotateCcw className="size-4" aria-hidden /><Label k="drive.action.restore-version" />
                  </Button>
                )}
              </div>
            </li>
          ))}
        </ul>
      )}
    </Dialog>
  )
}
