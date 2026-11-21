import { useState } from 'react'
import { ChevronDown, ChevronLeft, Folder, Loader2 } from 'lucide-react'
import { DRIVE_ITEM_KIND } from '@/shared/config'
import { cn, t } from '@/shared/lib'
import { useDriveChildren, type DriveItemResponse } from '@/entities/drive-item'
import { Label } from '@/shared/ui'

interface FolderTreePickerProps {
  spaceId: string
  /** The item being moved; its own node is disabled so it cannot become its own destination. */
  movingItemId: string
  /** `''` selects the space root; otherwise a folder id. */
  selectedId: string
  onSelect: (id: string) => void
}

interface FolderNodeProps extends FolderTreePickerProps {
  folder: DriveItemResponse
  depth: number
}

function FolderNode({
  spaceId,
  movingItemId,
  selectedId,
  onSelect,
  folder,
  depth,
}: FolderNodeProps) {
  const [expanded, setExpanded] = useState(false)
  const childrenQuery = useDriveChildren(spaceId, folder.itemId, { enabled: expanded })
  const subFolders = (childrenQuery.data ?? []).filter(
    (child) => child.itemKindId === DRIVE_ITEM_KIND.Folder,
  )
  const isSelf = folder.itemId === movingItemId
  const isSelected = selectedId === folder.itemId

  return (
    <li>
      <div
        className={cn(
          'flex items-center gap-1 rounded-lg py-1 pe-2',
          isSelected && 'bg-accent/10',
          isSelf && 'opacity-50',
        )}
        style={{ paddingInlineStart: depth * 16 }}
      >
        <button
          type="button"
          onClick={() => setExpanded((value) => !value)}
          aria-label={t('drive.dialog.move-expand')}
          aria-expanded={expanded}
          className="flex size-6 shrink-0 items-center justify-center rounded text-muted transition-colors hover:bg-surface-muted hover:text-foreground"
        >
          {expanded ? (
            <ChevronDown className="size-4 rtl:rotate-180" aria-hidden />
          ) : (
            <ChevronLeft className="size-4 rtl:rotate-180" aria-hidden />
          )}
        </button>
        <button
          type="button"
          disabled={isSelf}
          onClick={() => onSelect(folder.itemId)}
          aria-current={isSelected ? 'true' : undefined}
          className={cn(
            'flex min-w-0 flex-1 items-center gap-2 rounded px-1 py-0.5 text-start text-sm transition-colors',
            isSelected ? 'font-semibold text-accent' : 'text-foreground hover:bg-surface-muted',
            isSelf && 'cursor-not-allowed',
          )}
        >
          <Folder className="size-4 shrink-0 text-accent" aria-hidden />
          <span className="truncate">{folder.name}</span>
        </button>
      </div>
      {expanded && (
        <ul className="flex flex-col">
          {childrenQuery.isLoading ? (
            <li
              className="flex items-center gap-2 py-1 text-xs text-muted"
              style={{ paddingInlineStart: (depth + 1) * 16 }}
            >
              <Loader2 className="size-3.5 animate-spin" aria-hidden />
              <Label k="drive.state.loading" />
            </li>
          ) : subFolders.length === 0 ? (
            <li
              className="py-1 text-xs text-muted"
              style={{ paddingInlineStart: (depth + 1) * 16 }}
            >
              <Label k="drive.dialog.move-no-subfolders" />
            </li>
          ) : (
            subFolders.map((child) => (
              <FolderNode
                key={child.itemId}
                spaceId={spaceId}
                movingItemId={movingItemId}
                selectedId={selectedId}
                onSelect={onSelect}
                folder={child}
                depth={depth + 1}
              />
            ))
          )}
        </ul>
      )}
    </li>
  )
}

/**
 * A lazy folder tree of one space for the move dialog: the root plus expandable folder branches
 * (direct children read on demand). The moving item's own node is disabled; deeper cycles are
 * rejected by the server with a clear conflict error.
 */
export function FolderTreePicker({
  spaceId,
  movingItemId,
  selectedId,
  onSelect,
}: FolderTreePickerProps) {
  const rootQuery = useDriveChildren(spaceId, null)
  const rootFolders = (rootQuery.data ?? []).filter(
    (child) => child.itemKindId === DRIVE_ITEM_KIND.Folder,
  )
  const rootSelected = selectedId === ''

  return (
    <div className="max-h-72 overflow-y-auto rounded-lg border border-border bg-surface p-2">
      <ul className="flex flex-col">
        <li>
          <button
            type="button"
            onClick={() => onSelect('')}
            aria-current={rootSelected ? 'true' : undefined}
            className={cn(
              'flex w-full items-center gap-2 rounded-lg px-2 py-1 text-start text-sm transition-colors',
              rootSelected ? 'bg-accent/10 font-semibold text-accent' : 'text-foreground hover:bg-surface-muted',
            )}
          >
            <Folder className="size-4 shrink-0 text-accent" aria-hidden />
            <Label k="drive.browser.root" />
          </button>
        </li>
        {rootQuery.isLoading ? (
          <li className="flex items-center gap-2 px-2 py-1 text-xs text-muted">
            <Loader2 className="size-3.5 animate-spin" aria-hidden />
            <Label k="drive.state.loading" />
          </li>
        ) : (
          rootFolders.map((folder) => (
            <FolderNode
              key={folder.itemId}
              spaceId={spaceId}
              movingItemId={movingItemId}
              selectedId={selectedId}
              onSelect={onSelect}
              folder={folder}
              depth={1}
            />
          ))
        )}
      </ul>
    </div>
  )
}