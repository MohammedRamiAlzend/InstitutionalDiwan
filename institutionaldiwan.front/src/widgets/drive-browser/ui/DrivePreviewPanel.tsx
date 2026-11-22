import { Download, ExternalLink, FileText, Folder, FolderOpen, X } from 'lucide-react'
import { DRIVE_ITEM_KIND } from '@/shared/config'
import { formatDateTime, formatFileSize, t } from '@/shared/lib'
import { Button, Label } from '@/shared/ui'
import type { DriveItemResponse } from '@/entities/drive-item'

interface DrivePreviewPanelProps {
  item: DriveItemResponse
  ownerName: string
  onClose: () => void
  onOpen?: () => void
  onDownload?: () => void
}

/** Contextual metadata/actions for the selected file; full content preview remains in DrivePreviewDialog. */
export function DrivePreviewPanel({ item, ownerName, onClose, onOpen, onDownload }: DrivePreviewPanelProps) {
  const isFolder = item.itemKindId === DRIVE_ITEM_KIND.Folder
  const Icon = isFolder ? Folder : FileText

  return (
    <aside className="flex w-full shrink-0 flex-col overflow-hidden rounded-2xl border border-border bg-surface shadow-sm xl:sticky xl:top-4 xl:w-80" aria-label={t('drive.dialog.preview-title')}>
      <header className="flex items-center justify-between gap-3 border-b border-border bg-surface-muted/30 px-4 py-3">
        <h2 className="truncate text-sm font-semibold text-foreground" title={item.name}>{t('drive.dialog.preview-title')}</h2>
        <Button type="button" variant="ghost" size="icon" className="size-8 shrink-0" onClick={onClose} aria-label={t('common.action.close')}>
          <X className="size-4" aria-hidden />
        </Button>
      </header>
      <div className="flex flex-col gap-4 p-4">
        <div className="flex h-36 items-center justify-center rounded-xl border border-border bg-surface-muted text-accent">
          <span className="flex size-24 items-center justify-center rounded-2xl border border-border bg-surface shadow-sm">
            <Icon className={isFolder ? 'size-14' : 'size-16'} aria-hidden />
          </span>
        </div>
        <div className="min-w-0">
          <h3 className="break-words text-sm font-semibold text-foreground">{item.name}</h3>
          <p className="mt-1 text-xs text-muted">
            {t(isFolder ? 'drive.item-kind.folder' : 'drive.item-kind.file')}
            {!isFolder && <> · <span dir="ltr">{formatFileSize(item.sizeBytes)}</span></>}
          </p>
        </div>
        <div className="flex flex-wrap gap-2">
          {onOpen && (
            <Button type="button" className="h-9" onClick={onOpen}>
              {isFolder ? <FolderOpen className="size-4" aria-hidden /> : <ExternalLink className="size-4" aria-hidden />}
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
        <section className="border-t border-border pt-3" aria-label={t('drive.dialog.preview-title')}>
          <h4 className="mb-2 text-xs font-semibold text-foreground"><Label k="drive.preview.details" /></h4>
          <dl className="flex flex-col gap-2 text-xs">
            <div className="flex items-start justify-between gap-3"><dt className="text-muted"><Label k="drive.column.name" /></dt><dd className="max-w-44 break-all text-end text-foreground">{item.name}</dd></div>
            <div className="flex items-start justify-between gap-3"><dt className="text-muted"><Label k="drive.column.modified" /></dt><dd dir="ltr" className="text-end text-foreground">{formatDateTime(item.updatedAt ?? item.createdAt)}</dd></div>
            <div className="flex items-start justify-between gap-3"><dt className="text-muted"><Label k="drive.column.owner" /></dt><dd className="max-w-44 truncate text-end text-foreground">{ownerName}</dd></div>
            {!isFolder && <div className="flex items-start justify-between gap-3"><dt className="text-muted"><Label k="drive.column.size" /></dt><dd dir="ltr" className="text-foreground">{formatFileSize(item.sizeBytes)}</dd></div>}
          </dl>
        </section>
      </div>
    </aside>
  )
}
