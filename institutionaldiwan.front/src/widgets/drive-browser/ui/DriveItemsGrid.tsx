import type { ReactNode } from 'react'
import { ChevronLeft, ChevronRight, Inbox, MoreHorizontal } from 'lucide-react'
import { DRIVE_ITEM_KIND, DRIVE_ROLE } from '@/shared/config'
import { cn, formatFileSize, formatDateTime, formatNumber, t } from '@/shared/lib'
import {
  Button,
  ContextMenu,
  DropdownMenu,
  Label,
  Skeleton,
  dispatchContextMenu,
  isContextMenuShortcut,
  type MenuEntry,
} from '@/shared/ui'
import { ItemKindIcon, StarIndicator, type DriveItemResponse } from '@/entities/drive-item'
import { DriveDropTarget } from './DriveDropTarget'
import { startItemDrag } from '../model/dnd'

interface DriveItemsGridProps {
  items: readonly DriveItemResponse[]
  isLoading: boolean
  roles: Record<string, number | undefined>
  rowMenu: (item: DriveItemResponse) => readonly MenuEntry[]
  onOpenFolder: (itemId: string) => void
  onOpenFile?: (item: DriveItemResponse) => void
  onDropItem: (itemId: string, targetFolderId: string | null) => void
  page: number
  pageSize: number
  totalCount: number
  onPageChange: (page: number) => void
  emptyTitle: string
  emptyDescription?: string
  emptyAction?: ReactNode
}

/** The thumbnail-grid presentation of the browse page; shares menus, DnD and URL paging with list. */
export function DriveItemsGrid({
  items,
  isLoading,
  roles,
  rowMenu,
  onOpenFolder,
  onOpenFile,
  onDropItem,
  page,
  pageSize,
  totalCount,
  onPageChange,
  emptyTitle,
  emptyDescription,
  emptyAction,
}: DriveItemsGridProps) {
  const totalPages = Math.max(1, Math.ceil(totalCount / pageSize))
  const rangeStart = totalCount === 0 ? 0 : (page - 1) * pageSize + 1
  const rangeEnd = Math.min(page * pageSize, totalCount)

  if (isLoading) {
    return (
      <div className="grid grid-cols-2 gap-3 sm:grid-cols-3 lg:grid-cols-4 xl:grid-cols-5">
        {Array.from({ length: 10 }, (_, index) => (
          <Skeleton key={index} className="h-32 w-full rounded-xl" />
        ))}
      </div>
    )
  }

  if (items.length === 0) {
    return (
      <div className="flex min-h-64 flex-col items-center justify-center gap-3 rounded-xl border border-border bg-surface p-8 text-center">
        <span className="flex size-12 items-center justify-center rounded-full bg-surface-muted text-muted">
          <Inbox className="size-6" aria-hidden />
        </span>
        <div className="flex flex-col gap-1">
          <p className="text-sm font-medium text-foreground">{emptyTitle}</p>
          {emptyDescription && <p className="text-xs text-muted">{emptyDescription}</p>}
        </div>
        {emptyAction}
      </div>
    )
  }

  return (
    <div className="flex flex-col gap-3">
      <div className="grid grid-cols-2 gap-3 sm:grid-cols-3 lg:grid-cols-4 xl:grid-cols-5">
        {items.map((item) => {
          const isFolder = item.itemKindId === DRIVE_ITEM_KIND.Folder
          const role = roles[item.itemId]
          const canDrag = role !== undefined && role >= DRIVE_ROLE.Editor
          const entries = rowMenu(item)
          const open = () => (isFolder ? onOpenFolder(item.itemId) : onOpenFile?.(item))

          const card = (
            <div
              tabIndex={0}
              draggable={canDrag}
              onDragStart={(event) => startItemDrag(event, item.itemId)}
              onClick={open}
              onKeyDown={(event) => {
                if (event.key === 'Enter' || event.key === ' ') {
                  event.preventDefault()
                  open()
                } else if (entries.length > 0 && isContextMenuShortcut(event)) {
                  event.preventDefault()
                  dispatchContextMenu(event.currentTarget)
                }
              }}
              className={cn(
                'group flex cursor-pointer flex-col gap-2 rounded-xl border border-border bg-surface p-3 transition-colors',
                'hover:border-accent/50 hover:bg-surface-muted/50 focus-visible:border-focus-ring focus-visible:outline-none',
              )}
            >
              <div className="flex items-start justify-between gap-2">
                <ItemKindIcon kindId={item.itemKindId} className="size-8" />
                <div className="flex items-center gap-1" onClick={(event) => event.stopPropagation()}>
                  <StarIndicator starred={item.starred} />
                  {entries.length > 0 && (
                    <DropdownMenu
                      trigger={
                        <Button
                          type="button"
                          variant="ghost"
                          size="icon"
                          className="size-7"
                          aria-label={t('table.row-actions-named', { name: item.name })}
                        >
                          <MoreHorizontal className="size-4" aria-hidden />
                        </Button>
                      }
                      entries={entries}
                    />
                  )}
                </div>
              </div>
              <span className="truncate text-sm font-medium text-foreground" title={item.name}>
                {item.name}
              </span>
              <span className="mt-auto flex items-center justify-between gap-2 text-xs text-muted">
                <span dir="ltr">{isFolder ? '—' : formatFileSize(item.sizeBytes)}</span>
                <span dir="ltr">{formatDateTime(item.updatedAt ?? item.createdAt)}</span>
              </span>
            </div>
          )

          return (
            <ContextMenu key={item.itemId} entries={entries}>
              {isFolder ? (
                <DriveDropTarget targetFolderId={item.itemId} onDropItem={onDropItem}>
                  {card}
                </DriveDropTarget>
              ) : (
                card
              )}
            </ContextMenu>
          )
        })}
      </div>

      <div className="flex flex-wrap items-center justify-between gap-3">
        <span className="text-xs text-muted">
          <Label k="table.showing" /> <span dir="ltr">{formatNumber(rangeStart)}–{formatNumber(rangeEnd)}</span>{' '}
          <Label k="table.of" /> <span dir="ltr">{formatNumber(totalCount)}</span>
        </span>
        <div className="flex items-center gap-2">
          <span className="text-xs text-muted">
            <Label k="table.page-word" /> <span dir="ltr">{formatNumber(page)}</span> <Label k="table.of" />{' '}
            <span dir="ltr">{formatNumber(totalPages)}</span>
          </span>
          <Button
            type="button"
            variant="outline"
            size="icon"
            className="size-8"
            onClick={() => onPageChange(page - 1)}
            disabled={page <= 1}
            aria-label={t('table.prev-page')}
          >
            <ChevronLeft className="size-4 rtl:rotate-180" aria-hidden />
          </Button>
          <Button
            type="button"
            variant="outline"
            size="icon"
            className="size-8"
            onClick={() => onPageChange(page + 1)}
            disabled={page >= totalPages}
            aria-label={t('table.next-page')}
          >
            <ChevronRight className="size-4 rtl:rotate-180" aria-hidden />
          </Button>
        </div>
      </div>
    </div>
  )
}