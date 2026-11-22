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

const crumbClass =
  'rounded-md px-1.5 py-1 text-sm transition-colors hover:bg-surface-muted focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-focus-ring'

/**
 * The folder path of the open space: a root crumb (the space) plus the ancestors of the open folder
 * (root-first, the folder itself last). Every crumb navigates and is a drag-and-drop move target.
 * Rendered as a slim inline trail so it shares the page surface instead of adding another card.
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
    <nav
      aria-label={t('drive.browser.root-aria')}
      className="flex min-h-8 w-full min-w-0 flex-wrap items-center gap-0.5 text-sm"
    >
      <DriveDropTarget targetFolderId={null} onDropItem={onDropItem}>
        <button
          type="button"
          onClick={() => onNavigateFolder(null)}
          className={`${crumbClass} font-medium text-foreground`}
        >
          {spaceName}
        </button>
      </DriveDropTarget>

      {nodes.map((node, index) => {
        const isLast = index === nodes.length - 1
        return (
          <span key={node.itemId} className="flex items-center gap-0.5">
            <ChevronLeft className="size-3.5 text-muted rtl:rotate-180" aria-hidden />
            <DriveDropTarget targetFolderId={node.itemId} onDropItem={onDropItem}>
              {isLast ? (
                <span aria-current="page" className="px-1.5 py-1 font-semibold text-foreground">
                  {node.name}
                </span>
              ) : (
                <button
                  type="button"
                  onClick={() => onNavigateFolder(node.itemId)}
                  className={`${crumbClass} text-muted hover:text-foreground`}
                >
                  {node.name}
                </button>
              )}
            </DriveDropTarget>
          </span>
        )
      })}

      {folderId === null && (
        <span className="px-1.5 py-1 text-xs text-muted">
          <Label k="drive.browser.root" />
        </span>
      )}
    </nav>
  )
}
