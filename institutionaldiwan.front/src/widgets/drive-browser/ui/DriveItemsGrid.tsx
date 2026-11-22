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

/** Icon-grid, tile, and compact-list presentations share Drive menus, paging and drag/drop behavior. */
export function DriveItemsGrid({
  items,
  isLoading,
  roles,
  rowMenu,
  onOpenFolder,
  onOpenFile,
  onSelectItem,
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
    return <div className={layout === 'list' ? 'flex flex-col gap-2' : 'grid grid-cols-1 gap-3 sm:grid-cols-2 xl:grid-cols-3'}>{Array.from({ length: 8 }, (_, index) => <Skeleton key={index} className={layout === 'list' ? 'h-12 w-full rounded-lg' : 'h-28 w-full rounded-xl'} />)}</div>
  }

  if (items.length === 0) {
    return (
      <div className="flex min-h-64 flex-col items-center justify-center gap-3 rounded-xl border border-border bg-surface p-8 text-center">
        <span className="flex size-12 items-center justify-center rounded-full bg-surface-muted text-muted"><Inbox className="size-6" aria-hidden /></span>
        <div className="flex flex-col gap-1"><p className="text-sm font-medium text-foreground">{emptyTitle}</p>{emptyDescription && <p className="text-xs text-muted">{emptyDescription}</p>}</div>
        {emptyAction}
      </div>
    )
  }

  return (
    <div className="flex flex-col gap-3">
      <div className={cn(
        layout === 'list' ? 'flex flex-col divide-y divide-border overflow-hidden rounded-xl border border-border bg-surface' :
        layout === 'tiles' ? 'grid grid-cols-1 gap-2 lg:grid-cols-2' :
        'grid grid-cols-2 gap-3 sm:grid-cols-3 lg:grid-cols-4 xl:grid-cols-5',
      )}>
        {items.map((item) => {
          const isFolder = item.itemKindId === DRIVE_ITEM_KIND.Folder
          const role = roles[item.itemId]
          const canDrag = role !== undefined && role >= DRIVE_ROLE.Editor
          const entries = rowMenu(item)
          const openFolder = () => onOpenFolder(item.itemId)
          const selectItem = () => onSelectItem(item)
          const open = () => isFolder ? openFolder() : onOpenFile?.(item)
          const details = <span className="flex shrink-0 items-center gap-3 text-xs text-muted"><span dir="ltr">{isFolder ? '—' : formatFileSize(item.sizeBytes)}</span><span dir="ltr">{formatDateTime(item.updatedAt ?? item.createdAt)}</span></span>
          const itemName = <button type="button" onClick={selectItem} onDoubleClick={open} className="min-w-0 flex-1 truncate text-start text-sm font-medium text-foreground hover:text-accent focus-visible:outline-none focus-visible:underline" title={item.name} aria-current={selectedItemId === item.itemId ? 'true' : undefined}>{item.name}</button>
          const menuButton = entries.length > 0 && <DropdownMenu trigger={<Button type="button" variant="ghost" size="icon" className="size-7 shrink-0" aria-label={t('table.row-actions-named', { name: item.name })}><MoreHorizontal className="size-4" aria-hidden /></Button>} entries={entries} />
          const content = layout === 'list' ? (
            <div className="flex min-h-11 items-center gap-3 px-3 py-1.5">
              <ItemKindIcon kindId={item.itemKindId} className="size-5 shrink-0" />{itemName}{details}<StarIndicator starred={item.starred} />{menuButton}
            </div>
          ) : layout === 'tiles' ? (
            <div className="flex min-h-20 items-center gap-3 p-3">
              <span className="flex size-11 shrink-0 items-center justify-center rounded-lg bg-surface-muted"><ItemKindIcon kindId={item.itemKindId} className="size-7" /></span>
              <div className="flex min-w-0 flex-1 flex-col gap-1">{itemName}<span className="text-xs text-muted">{t(isFolder ? 'drive.item-kind.folder' : 'drive.item-kind.file')}</span></div>
              {details}<StarIndicator starred={item.starred} />{menuButton}
            </div>
          ) : (
            <div className="flex min-h-36 flex-col gap-3 p-3">
              <div className="flex items-start justify-between gap-2"><ItemKindIcon kindId={item.itemKindId} className="size-9" /><div className="flex items-center gap-1"><StarIndicator starred={item.starred} />{menuButton}</div></div>
              {itemName}{details}
            </div>
          )
          const itemCard = (
            <div
              draggable={canDrag}
              onDragStart={(event) => startItemDrag(event, item.itemId)}
              className={cn(
                'group cursor-pointer transition-colors',
                layout === 'list' ? 'hover:bg-surface-muted/60' : 'rounded-xl border border-border bg-surface hover:border-accent/50 hover:bg-surface-muted/50',
                selectedItemId === item.itemId && 'border-accent bg-accent/5 ring-1 ring-accent/30',
              )}
            >{content}</div>
          )

          return (
            <ContextMenu key={item.itemId} entries={entries}>
              {isFolder ? <DriveDropTarget targetFolderId={item.itemId} onDropItem={onDropItem}>{itemCard}</DriveDropTarget> : itemCard}
            </ContextMenu>
          )
        })}
      </div>

      <div className="flex flex-wrap items-center justify-between gap-3">
        <span className="text-xs text-muted"><Label k="table.showing" /> <span dir="ltr">{formatNumber(rangeStart)}–{formatNumber(rangeEnd)}</span>{' '}<Label k="table.of" /> <span dir="ltr">{formatNumber(totalCount)}</span></span>
        <div className="flex items-center gap-2"><span className="text-xs text-muted"><Label k="table.page-word" /> <span dir="ltr">{formatNumber(page)}</span> <Label k="table.of" />{' '}<span dir="ltr">{formatNumber(totalPages)}</span></span>
          <Button type="button" variant="outline" size="icon" className="size-8" onClick={() => onPageChange(page - 1)} disabled={page <= 1} aria-label={t('table.prev-page')}><ChevronLeft className="size-4 rtl:rotate-180" aria-hidden /></Button>
          <Button type="button" variant="outline" size="icon" className="size-8" onClick={() => onPageChange(page + 1)} disabled={page >= totalPages} aria-label={t('table.next-page')}><ChevronRight className="size-4 rtl:rotate-180" aria-hidden /></Button>
        </div>
      </div>
    </div>
  )
}
