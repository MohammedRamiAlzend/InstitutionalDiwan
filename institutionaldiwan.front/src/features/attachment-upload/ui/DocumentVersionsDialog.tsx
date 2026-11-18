import { Download } from 'lucide-react'
import { toast } from 'sonner'
import {
  downloadFileVersion,
  type DocumentFileResponse,
  type DocumentFileVersionResponse,
} from '@/entities/document-file'
import { toArabicErrorMessage } from '@/shared/api'
import { formatDateTime, formatFileSize, formatShortId, t } from '@/shared/lib'
import { Badge, Button, Dialog, Label } from '@/shared/ui'
import { UserLabel } from '@/entities/user'

interface DocumentVersionsDialogProps {
  file: DocumentFileResponse
  open: boolean
  onOpenChange: (open: boolean) => void
}

function VersionField({ label, value }: { label: string; value: string }) {
  return (
    <p className="flex flex-wrap items-baseline gap-1.5 text-xs">
      <span className="text-muted">{label}:</span>
      <span dir="ltr" className="break-all text-foreground" title={value}>
        {value}
      </span>
    </p>
  )
}

/**
 * Every version one file has held, newest first as the list route returns them. Versions are
 * immutable, so an old one stays downloadable, and archiving changes nothing here — it only stops
 * new ones from appearing.
 */
export function DocumentVersionsDialog({
  file,
  open,
  onOpenChange,
}: DocumentVersionsDialogProps) {
  const downloadVersion = async (version: DocumentFileVersionResponse) => {
    try {
      const fileName = await downloadFileVersion(file, version)
      toast.success(t('attachment.versions.toast-downloaded', { version: version.versionNumber, name: fileName }))
    } catch (error) {
      toast.error(toArabicErrorMessage(error))
    }
  }

  return (
    <Dialog
      open={open}
      onOpenChange={onOpenChange}
      title={t('attachment.versions.title', { title: file.title })}
      description={t('attachment.versions.description', { count: file.versions.length })}
      size="lg"
      footer={
        <Button type="button" onClick={() => onOpenChange(false)}>
          <Label k="common.action.close" />
        </Button>
      }
    >
      {file.versions.length === 0 ? (
        <p className="rounded-lg border border-border bg-surface-muted/30 px-3 py-2 text-sm text-muted">
          <Label k="attachment.versions.empty" />
        </p>
      ) : (
        <ul className="flex flex-col gap-3">
          {file.versions.map((version) => (
            <li
              key={version.versionId}
              className="flex flex-col gap-2 rounded-lg border border-border bg-surface-muted/20 p-3"
            >
              <div className="flex flex-wrap items-center justify-between gap-2">
                <p className="flex items-center gap-2 text-sm font-semibold text-foreground">
                  <span dir="ltr">v{version.versionNumber}</span>
                  {version.isCurrent ? (
                    <Badge variant="success"><Label k="attachment.versions.current" /></Badge>
                  ) : (
                    <Badge variant="outline"><Label k="attachment.versions.replaced" /></Badge>
                  )}
                </p>
                <Button
                  type="button"
                  variant="outline"
                  size="sm"
                  onClick={() => void downloadVersion(version)}
                >
                  <Download className="size-4" aria-hidden />
                  <Label k="attachment.versions.download" />
                </Button>
              </div>

              <div className="grid gap-2 sm:grid-cols-2">
                <VersionField label={t('attachment.column.original-name')} value={version.originalFileName} />
                <VersionField label={t('attachment.column.size')} value={formatFileSize(version.fileSizeBytes)} />
                <VersionField label={t('attachment.column.checksum')} value={formatShortId(version.checksumSha256)} />
                <VersionField label={t('attachment.versions.field-uploaded-at')} value={formatDateTime(version.uploadedAt)} />
                <p className="flex flex-wrap items-baseline gap-1.5 text-xs">
                  <span className="text-muted"><Label k="attachment.versions.uploader" /></span>
                  <UserLabel userId={version.uploadedByUserId} />
                </p>
              </div>

              <p className="text-xs text-muted">
                <Label k="attachment.versions.change-notes" />{' '}
                <span className="text-foreground">
                  {version.changeNotes ?? t('attachment.versions.no-notes')}
                </span>
              </p>
            </li>
          ))}
        </ul>
      )}
    </Dialog>
  )
}
