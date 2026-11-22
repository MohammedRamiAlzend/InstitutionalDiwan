import { ChevronLeft } from 'lucide-react'
import { useDriveBreadcrumb } from '@/entities/drive-item'
import { t } from '@/shared/lib'
import { Label } from '@/shared/ui'
import { DriveDropTarget } from './DriveDropTarget'

interface DriveBreadcrumbProps {
  folderId: string | null
  spaceName: string
  onNavigateFolder: (folderId: string | null) => void
  onDropItem: (itemId: string, targetFolderId: string | null) => void
}

/**
 * The folder path of the open space: a root crumb (the space) plus the ancestors of the open folder
 * (root-first, the folder itself last). Every crumb navigates and is a drag-and-drop move target.
 */
export function DriveBreadcrumb({
  folderId,
  spaceName,
  onNavigateFolder,
  onDropItem,
}: DriveBreadcrumbProps) {
  const breadcrumbQuery = useDriveBreadcrumb(folderId)
  const nodes = breadcrumbQuery.data ?? []

  return (
    <nav aria-label={t('drive.browser.root-aria')} className="flex min-h-11 w-full min-w-0 flex-wrap items-center gap-1 rounded-xl border border-border bg-surface px-2 py-1.5 text-sm shadow-sm">
      <DriveDropTarget targetFolderId={null} onDropItem={onDropItem}>
        <button
          type="button"
          onClick={() => onNavigateFolder(null)}
          className="rounded-lg px-2 py-1 font-medium text-foreground transition-colors hover:bg-surface-muted"
        >
          {spaceName}
        </button>
      </DriveDropTarget>

      {nodes.map((node, index) => {
        const isLast = index === nodes.length - 1
        return (
          <span key={node.itemId} className="flex items-center gap-1">
            <ChevronLeft className="size-4 text-muted rtl:rotate-180" aria-hidden />
            <DriveDropTarget targetFolderId={node.itemId} onDropItem={onDropItem}>
              {isLast ? (
                <span aria-current="page" className="px-2 py-1 font-semibold text-accent">
                  {node.name}
                </span>
              ) : (
                <button
                  type="button"
                  onClick={() => onNavigateFolder(node.itemId)}
                  className="rounded-lg px-2 py-1 text-foreground transition-colors hover:bg-surface-muted"
                >
                  {node.name}
                </button>
              )}
            </DriveDropTarget>
          </span>
        )
      })}

      {folderId === null && (
        <span className="px-2 py-1 text-xs text-muted">
          <Label k="drive.browser.root" />
        </span>
      )}
    </nav>
  )
}
