import { useEffect } from 'react'
import { Download, ExternalLink, FileText, Folder, FolderOpen, X } from 'lucide-react'
import { DRIVE_ITEM_KIND } from '@/shared/config'
import { cn, formatDateTime, formatFileSize, t } from '@/shared/lib'
import { Button, Label } from '@/shared/ui'
import type { DriveItemResponse } from '@/entities/drive-item'

interface DrivePreviewPanelProps {
  item: DriveItemResponse
  ownerName: string
  onClose: () => void
  onOpen?: () => void
  onDownload?: () => void
}

/**
 * Contextual metadata/actions for the selected file, rendered as an end-side drawer over the file
 * bed so opening details never squeezes the grid or the table. Esc and the scrim close it; full
 * media/Office preview remains in `DrivePreviewDialog`.
 */
export function DrivePreviewPanel({
  item,
  ownerName,
  onClose,
  onOpen,
  onDownload,
}: DrivePreviewPanelProps) {
  const isFolder = item.itemKindId === DRIVE_ITEM_KIND.Folder
  const Icon = isFolder ? Folder : FileText

  useEffect(() => {
    const onKeyDown = (event: KeyboardEvent) => {
      if (event.key === 'Escape') onClose()
    }
    window.addEventListener('keydown', onKeyDown)
    return () => window.removeEventListener('keydown', onKeyDown)
  }, [onClose])

  return (
    <>
      <div
        aria-hidden
        onClick={onClose}
        className="fixed inset-0 z-30 bg-overlay/25 animate-overlay-in"
      />
      <aside
        aria-label={t('drive.dialog.preview-title')}
        className={cn(
          'fixed end-0 top-0 z-30 flex h-dvh w-full max-w-sm flex-col border-s border-border bg-surface shadow-xl animate-content-in',
        )}
      >
        <header className="flex shrink-0 items-center justify-between gap-3 border-b border-border px-4 py-3">
          <h2 className="truncate text-sm font-semibold text-foreground">
            {t('drive.dialog.preview-title')}
          </h2>
          <Button
            type="button"
            variant="ghost"
            size="icon"
            className="size-8 shrink-0 text-muted"
            onClick={onClose}
            aria-label={t('common.action.close')}
          >
            <X className="size-4" aria-hidden />
          </Button>
        </header>

        <div className="flex min-h-0 flex-1 flex-col gap-4 overflow-y-auto p-4">
          <div className="flex h-32 items-center justify-center rounded-xl border border-border bg-surface-muted/50 text-accent">
            <Icon className={isFolder ? 'size-12' : 'size-14'} aria-hidden />
          </div>

          <div className="min-w-0">
            <h3 className="break-words text-sm font-semibold text-foreground">{item.name}</h3>
            <p className="mt-1 text-xs text-muted">
              {t(isFolder ? 'drive.item-kind.folder' : 'drive.item-kind.file')}
              {!isFolder && (
                <>
                  {' · '}
                  <span dir="ltr">{formatFileSize(item.sizeBytes)}</span>
                </>
              )}
            </p>
          </div>

          <div className="flex flex-wrap gap-2">
            {onOpen && (
              <Button type="button" className="h-9" onClick={onOpen}>
                {isFolder ? (
                  <FolderOpen className="size-4" aria-hidden />
                ) : (
                  <ExternalLink className="size-4" aria-hidden />
                )}
                <Label k="drive.action.open" />
              </Button>
            )}
            {!isFolder && onDownload && (
              <Button type="button" variant="outline" className="h-9" onClick={onDownload}>
                <Download className="size-4" aria-hidden />
                <Label k="drive.action.download" />
              </Button>
            )}
          </div>

          <section className="border-t border-border pt-3">
            <h4 className="mb-2 text-xs font-semibold text-foreground">
              <Label k="drive.preview.details" />
            </h4>
            <dl className="flex flex-col gap-2 text-xs">
              <div className="flex items-start justify-between gap-3">
                <dt className="text-muted">
                  <Label k="drive.column.name" />
                </dt>
                <dd className="max-w-52 break-all text-end text-foreground">{item.name}</dd>
              </div>
              <div className="flex items-start justify-between gap-3">
                <dt className="text-muted">
                  <Label k="drive.column.modified" />
                </dt>
                <dd dir="ltr" className="text-end text-foreground">
                  {formatDateTime(item.updatedAt ?? item.createdAt)}
                </dd>
              </div>
              <div className="flex items-start justify-between gap-3">
                <dt className="text-muted">
                  <Label k="drive.column.owner" />
                </dt>
                <dd className="max-w-52 truncate text-end text-foreground">{ownerName}</dd>
              </div>
              {!isFolder && (
                <div className="flex items-start justify-between gap-3">
                  <dt className="text-muted">
                    <Label k="drive.column.size" />
                  </dt>
                  <dd dir="ltr" className="text-foreground">
                    {formatFileSize(item.sizeBytes)}
                  </dd>
                </div>
              )}
            </dl>
          </section>
        </div>
      </aside>
    </>
  )
}
