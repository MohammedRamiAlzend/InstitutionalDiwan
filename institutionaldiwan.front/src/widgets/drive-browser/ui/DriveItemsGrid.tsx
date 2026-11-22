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
  type MenuEntry,
} from '@/shared/ui'
import { ItemKindIcon, StarIndicator, type DriveItemResponse } from '@/entities/drive-item'
import { DriveDropTarget } from './DriveDropTarget'
import { startItemDrag } from '../model/dnd'
import type { DriveLayout } from './DriveBrowserControls'

interface DriveItemsGridProps {
  items: readonly DriveItemResponse[]
  isLoading: boolean
  roles: Record<string, number | undefined>
  rowMenu: (item: DriveItemResponse) => readonly MenuEntry[]
  onOpenFolder: (itemId: string) => void
  onOpenFile?: (item: DriveItemResponse) => void
  onSelectItem: (item: DriveItemResponse) => void
  onToggleStar: (item: DriveItemResponse) => void
  selectedItemId: string | null
  layout: Exclude<DriveLayout, 'details'>
  onDropItem: (itemId: string, targetFolderId: string | null) => void
  page: number
  pageSize: number
  totalCount: number
  onPageChange: (page: number) => void
  emptyTitle: string
  emptyDescription?: string
  emptyAction?: ReactNode
}

/**
 * Icon-grid, tile, and compact-list presentations share Drive menus, paging and drag/drop behavior.
 * The icon grid keeps the glyph on its own plate with the name on up to two lines and one quiet
 * metadata line; star and overflow actions stay hidden until the card is hovered or focused so the
 * file bed reads as content, not as a wall of buttons.
 */
export function DriveItemsGrid({
  items,
  isLoading,
  roles,
  rowMenu,
  onOpenFolder,
  onOpenFile,
  onSelectItem,
  onToggleStar,
  selectedItemId,
  layout,
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
      <div
        className={cn(
          'grid gap-2',
          layout === 'list'
            ? 'grid-cols-1'
            : layout === 'tiles'
              ? 'grid-cols-1 lg:grid-cols-2'
              : 'grid-cols-2 sm:grid-cols-3 xl:grid-cols-4',
        )}
      >
        {Array.from({ length: 8 }, (_, index) => (
          <Skeleton
            key={index}
            className={layout === 'list' ? 'h-11 w-full rounded-lg' : 'h-32 w-full rounded-xl'}
          />
        ))}
      </div>
    )
  }

  if (items.length === 0) {
    return (
      <div className="flex min-h-64 flex-col items-center justify-center gap-3 rounded-xl border border-dashed border-border bg-surface p-8 text-center">
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
      <div
        className={cn(
          layout === 'list'
            ? 'flex flex-col divide-y divide-border overflow-hidden rounded-xl border border-border bg-surface'
            : layout === 'tiles'
              ? 'grid grid-cols-1 gap-2 lg:grid-cols-2'
              : 'grid grid-cols-2 gap-3 sm:grid-cols-3 xl:grid-cols-4',
        )}
      >
        {items.map((item) => {
          const isFolder = item.itemKindId === DRIVE_ITEM_KIND.Folder
          const role = roles[item.itemId]
          const canDrag = role !== undefined && role >= DRIVE_ROLE.Editor
          const entries = rowMenu(item)
          const openFolder = () => onOpenFolder(item.itemId)
          const selectItem = () => onSelectItem(item)
          const open = () => (isFolder ? openFolder() : onOpenFile?.(item))
          const selected = selectedItemId === item.itemId

          const sizeText = isFolder ? '—' : formatFileSize(item.sizeBytes)
          const dateText = formatDateTime(item.updatedAt ?? item.createdAt)
          const meta = (
            <span className="flex min-w-0 items-center gap-1.5 truncate text-[11px] text-muted">
              <span dir="ltr" className="shrink-0">{sizeText}</span>
              <span aria-hidden>·</span>
              <span dir="ltr" className="truncate">{dateText}</span>
            </span>
          )
          const itemName = (
            <button
              type="button"
              onClick={selectItem}
              onDoubleClick={open}
              className={cn(
                'min-w-0 text-start font-medium text-foreground transition-colors hover:text-accent focus-visible:outline-none focus-visible:underline',
                layout === 'grid' ? 'line-clamp-2 min-h-10 text-sm' : 'flex-1 truncate text-sm',
              )}
              title={item.name}
              aria-current={selected ? 'true' : undefined}
            >
              {item.name}
            </button>
          )
          const starButton = (
            <button
              type="button"
              onClick={() => onToggleStar(item)}
              aria-label={t(item.starred ? 'drive.action.unstar' : 'drive.action.star')}
              className="flex size-7 items-center justify-center rounded-md text-muted transition-colors hover:bg-surface-muted hover:text-foreground"
            >
              <StarIndicator starred={item.starred} />
            </button>
          )
          const menuButton = entries.length > 0 && (
            <DropdownMenu
              trigger={
                <Button
                  type="button"
                  variant="ghost"
                  size="icon"
                  className="size-7 shrink-0 text-muted hover:text-foreground"
                  aria-label={t('table.row-actions-named', { name: item.name })}
                >
                  <MoreHorizontal className="size-4" aria-hidden />
                </Button>
              }
              entries={entries}
            />
          )

          const content =
            layout === 'list' ? (
              <div className="flex min-h-11 items-center gap-3 px-3 py-1.5">
                <ItemKindIcon kindId={item.itemKindId} className="size-4 shrink-0 text-muted" />
                {itemName}
                {meta}
                <span className="flex shrink-0 items-center gap-0.5">
                  {starButton}
                  {menuButton}
                </span>
              </div>
            ) : layout === 'tiles' ? (
              <div className="flex min-h-16 items-center gap-3 p-2.5">
                <span className="flex size-10 shrink-0 items-center justify-center rounded-lg bg-surface-muted text-muted">
                  <ItemKindIcon kindId={item.itemKindId} className="size-5" />
                </span>
                <div className="flex min-w-0 flex-1 flex-col gap-0.5">
                  {itemName}
                  {meta}
                </div>
                <span className="flex shrink-0 items-center gap-0.5">
                  {starButton}
                  {menuButton}
                </span>
              </div>
            ) : (
              <div className="flex flex-col">
                <div className="relative flex h-24 items-center justify-center border-b border-border/60 bg-surface-muted/40 text-muted">
                  <ItemKindIcon kindId={item.itemKindId} className="size-8" />
                  <span className="absolute end-1 top-1 flex items-center gap-0.5 rounded-md bg-surface/80 opacity-0 shadow-sm transition-opacity group-hover:opacity-100 group-focus-within:opacity-100">
                    {starButton}
                    {menuButton}
                  </span>
                </div>
                <div className="flex min-w-0 flex-col gap-1 p-2.5">
                  {itemName}
                  {meta}
                </div>
              </div>
            )

          const itemCard = (
            <div
              draggable={canDrag}
              onDragStart={(event) => startItemDrag(event, item.itemId)}
              className={cn(
                'group min-w-0 transition-colors',
                layout === 'list'
                  ? 'hover:bg-surface-muted/60'
                  : layout === 'tiles'
                    ? 'rounded-xl border border-border bg-surface hover:border-accent/50 hover:bg-surface-muted/40'
                    : 'flex flex-col overflow-hidden rounded-xl border border-border bg-surface hover:border-accent/50',
                selected &&
                  (layout === 'list'
                    ? 'bg-accent/5'
                    : 'border-accent bg-accent/5 ring-1 ring-accent/30'),
              )}
            >
              {content}
            </div>
          )

          return (
            <ContextMenu key={item.itemId} entries={entries}>
              {isFolder ? (
                <DriveDropTarget targetFolderId={item.itemId} onDropItem={onDropItem}>
                  {itemCard}
                </DriveDropTarget>
              ) : (
                itemCard
              )}
            </ContextMenu>
          )
        })}
      </div>

      <div className="flex flex-wrap items-center justify-between gap-3 border-t border-border pt-2.5">
        <span className="text-xs text-muted">
          <Label k="table.showing" />{' '}
          <span dir="ltr">{formatNumber(rangeStart)}–{formatNumber(rangeEnd)}</span>{' '}
          <Label k="table.of" /> <span dir="ltr">{formatNumber(totalCount)}</span>
        </span>
        <div className="flex items-center gap-2">
          <span className="text-xs text-muted">
            <Label k="table.page-word" /> <span dir="ltr">{formatNumber(page)}</span>{' '}
            <Label k="table.of" /> <span dir="ltr">{formatNumber(totalPages)}</span>
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
