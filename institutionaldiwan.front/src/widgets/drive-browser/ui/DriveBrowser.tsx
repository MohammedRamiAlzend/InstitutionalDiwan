import { useMemo, useState } from 'react'
import {
  Copy,
  Download,
  Eye,
  FolderInput,
  FolderOpen,
  FolderPlus,
  Hash,
  History,
  Loader2,
  Pencil,
  RefreshCw,
  Shield,
  Star,
  Trash2,
  Upload,
} from 'lucide-react'
import { toast } from 'sonner'
import {
  ItemKindIcon,
  StarIndicator,
  useDriveEffectiveRole,
  useDriveItems,
  type DriveBrowseParams,
  type DriveItemResponse,
  type DriveView,
} from '@/entities/drive-item'
import type { DriveSpaceResponse } from '@/entities/drive-space'
import { useSessionStore, useUserOptions } from '@/entities/user'
import {
  CreateFolderDialog,
  MoveItemDialog,
  RenameItemDialog,
  useMoveItemMutation,
  useSetItemStarMutation,
} from '@/features/drive-item-manage'
import { toArabicErrorMessage } from '@/shared/api'
import { DRIVE_ITEM_KIND, DRIVE_ROLE, Permissions } from '@/shared/config'
import {
  formatDateTime,
  formatFileSize,
  formatShortId,
  t,
  useTableState,
} from '@/shared/lib'
import {
  Button,
  DataTable,
  GuideRegion,
  Label,
  type DataTableColumn,
  type MenuEntry,
} from '@/shared/ui'
import { DriveBreadcrumb } from './DriveBreadcrumb'
import { DriveBrowserControls, type DriveLayout } from './DriveBrowserControls'
import { DriveDropTarget } from './DriveDropTarget'
import { DriveItemsGrid } from './DriveItemsGrid'
import { DrivePreviewPanel } from './DrivePreviewPanel'
import { useItemRoles } from '../model/useItemRoles'
import { startItemDrag } from '../model/dnd'

/** Later-phase item actions; an entry appears only when its callback is wired and the role allows. */
export interface DriveItemActions {
  onOpenFile?: (item: DriveItemResponse) => void
  onDownload?: (item: DriveItemResponse) => void
  onUploadVersion?: (item: DriveItemResponse) => void
  onVersions?: (item: DriveItemResponse) => void
  onPermissions?: (item: DriveItemResponse) => void
  onRequestDeletion?: (item: DriveItemResponse) => void
  onDirectDelete?: (item: DriveItemResponse) => void
}

interface DriveBrowserProps {
  view: DriveView
  spaceId: string | null
  folderId: string | null
  personal: DriveSpaceResponse | null
  shared: DriveSpaceResponse[]
  onNavigateFolder: (folderId: string | null) => void
  onUpload?: () => void
  onDropFiles?: (files: File[]) => void
  actions?: DriveItemActions
}

/**
 * The Drive content region for the browse modes (my-files / shared / starred): a server-paged grid
 * or list over `useDriveItems`, breadcrumb, toolbar, effective-role-gated context menus, and
 * drag-and-drop moves. Trash and deletion-requests are separate views owned by the page.
 */
export function DriveBrowser({
  view,
  spaceId,
  folderId,
  personal,
  shared,
  onNavigateFolder,
  onUpload,
  onDropFiles,
  actions,
}: DriveBrowserProps) {
  const userId = useSessionStore((state) => state.user?.userId ?? null)
  const canManage = useSessionStore(
    (state) => state.user?.permissions.includes(Permissions.DriveManage) ?? false,
  )

  const table = useTableState({
    filterKeys: ['kind', 'owner', 'layout'],
    defaultSortKey: 'name',
    defaultSortDirection: 'asc',
  })
  const layout: DriveLayout = ['details', 'list', 'tiles', 'grid'].includes(table.filters.layout)
    ? (table.filters.layout as DriveLayout)
    : 'details'

  const activeSpace =
    view === 'my-files' ? personal : view === 'shared' ? (shared.find((s) => s.spaceId === spaceId) ?? null) : null
  const browseSpaceId = view === 'starred' ? null : (activeSpace?.spaceId ?? null)

  const params = useMemo<DriveBrowseParams>(
    () => ({
      page: table.page,
      pageSize: table.pageSize,
      spaceId: browseSpaceId ?? undefined,
      parentId: view === 'starred' ? undefined : (folderId ?? undefined),
      search: table.search || undefined,
      kind:
        table.filters.kind === 'folder'
          ? DRIVE_ITEM_KIND.Folder
          : table.filters.kind === 'file'
            ? DRIVE_ITEM_KIND.File
            : undefined,
      ownerUserId: table.filters.owner || undefined,
      starred: view === 'starred' ? true : undefined,
      sortBy: table.sortKey ?? undefined,
      sortDescending: table.sortDirection === 'desc',
    }),
    [table.page, table.pageSize, table.search, table.filters.kind, table.filters.owner, table.sortKey, table.sortDirection, browseSpaceId, folderId, view],
  )

  const queryEnabled = view === 'starred' ? true : Boolean(browseSpaceId)
  const query = useDriveItems(params, { enabled: queryEnabled })
  const items = query.data?.items ?? []
  const roles = useItemRoles(items)

  const folderRoleQuery = useDriveEffectiveRole(folderId, {
    enabled: Boolean(folderId) && view !== 'starred',
  })
  const canCreateFolder =
    view === 'starred'
      ? false
      : folderId
        ? (folderRoleQuery.data?.roleId ?? DRIVE_ROLE.None) >= DRIVE_ROLE.Editor
        : view === 'my-files'
          ? Boolean(personal)
          : canManage || activeSpace?.ownerUserId === userId

  const userOptionsQuery = useUserOptions({ page: 1, pageSize: 100 })
  const ownerOptions = useMemo(
    () => (userOptionsQuery.data?.items ?? []).map((u) => ({ value: u.userId, label: u.username })),
    [userOptionsQuery.data],
  )
  const ownerName = useMemo(() => {
    const map: Record<string, string> = {}
    for (const option of ownerOptions) map[option.value] = option.label
    return map
  }, [ownerOptions])

  const starMutation = useSetItemStarMutation()
  const moveMutation = useMoveItemMutation()
  const [createFolderOpen, setCreateFolderOpen] = useState(false)
  const [renameTarget, setRenameTarget] = useState<DriveItemResponse | null>(null)
  const [moveTarget, setMoveTarget] = useState<DriveItemResponse | null>(null)
  const [selectedItem, setSelectedItem] = useState<DriveItemResponse | null>(null)
  const [fileOver, setFileOver] = useState(false)

  const copyValue = async (value: string, label: string) => {
    try {
      await navigator.clipboard.writeText(value)
      toast.success(t('drive.toast.copied', { label }))
    } catch {
      toast.error(t('drive.toast.copy-failed'))
    }
  }

  const handleStar = (item: DriveItemResponse) => {
    const starred = !item.starred
    void starMutation
      .mutateAsync({ itemId: item.itemId, starred })
      .then(() => toast.success(t(starred ? 'drive.toast.starred' : 'drive.toast.unstarred')))
      .catch((error) => toast.error(toArabicErrorMessage(error)))
  }

  const handleDropItem = (itemId: string, targetFolderId: string | null) => {
    if (itemId === targetFolderId) return
    void moveMutation
      .mutateAsync({ itemId, newParentId: targetFolderId })
      .then(() => toast.success(t('drive.toast.moved')))
      .catch((error) => toast.error(toArabicErrorMessage(error)))
  }

  const openFolder = (itemId: string) => {
    setSelectedItem(null)
    onNavigateFolder(itemId)
  }
  const openFile = (item: DriveItemResponse) => actions?.onOpenFile?.(item)

  const buildRowMenu = (item: DriveItemResponse): MenuEntry[] => {
    const role = roles[item.itemId]
    const canEdit = role !== undefined && role >= DRIVE_ROLE.Editor
    const canManageItem = role !== undefined && role >= DRIVE_ROLE.Manager
    const isFolder = item.itemKindId === DRIVE_ITEM_KIND.Folder
    const menu: MenuEntry[] = []

    if (isFolder) {
      menu.push({
        id: 'open',
        label: t('drive.action.open'),
        icon: <FolderOpen className="size-4" aria-hidden />,
        onSelect: () => openFolder(item.itemId),
      })
    } else if (actions?.onOpenFile) {
      menu.push({
        id: 'open',
        label: t('drive.action.open'),
        icon: <Eye className="size-4" aria-hidden />,
        onSelect: () => actions.onOpenFile?.(item),
      })
    }
    if (!isFolder && actions?.onDownload) {
      menu.push({
        id: 'download',
        label: t('drive.action.download'),
        icon: <Download className="size-4" aria-hidden />,
        onSelect: () => actions.onDownload?.(item),
      })
    }
    menu.push({
      id: 'star',
      label: t(item.starred ? 'drive.action.unstar' : 'drive.action.star'),
      icon: <Star className="size-4" aria-hidden />,
      onSelect: () => handleStar(item),
    })

    if (canEdit) {
      menu.push(
        { type: 'separator' },
        {
          id: 'rename',
          label: t('drive.action.rename'),
          icon: <Pencil className="size-4" aria-hidden />,
          onSelect: () => setRenameTarget(item),
        },
        {
          id: 'move',
          label: t('drive.action.move'),
          icon: <FolderInput className="size-4" aria-hidden />,
          onSelect: () => setMoveTarget(item),
        },
      )
      if (!isFolder && actions?.onUploadVersion) {
        menu.push({
          id: 'upload-version',
          label: t('drive.action.upload-version'),
          icon: <Upload className="size-4" aria-hidden />,
          onSelect: () => actions.onUploadVersion?.(item),
        })
      }
    }
    if (!isFolder && actions?.onVersions) {
      menu.push({
        id: 'versions',
        label: t('drive.action.versions'),
        icon: <History className="size-4" aria-hidden />,
        onSelect: () => actions.onVersions?.(item),
      })
    }
    if (canManageItem && actions?.onPermissions) {
      menu.push({
        id: 'permissions',
        label: t('drive.action.permissions'),
        icon: <Shield className="size-4" aria-hidden />,
        onSelect: () => actions.onPermissions?.(item),
      })
    }
    if (canEdit && actions?.onRequestDeletion) {
      menu.push({
        id: 'request-deletion',
        label: t('drive.action.request-deletion'),
        icon: <Trash2 className="size-4" aria-hidden />,
        danger: true,
        onSelect: () => actions.onRequestDeletion?.(item),
      })
    }
    if (canManageItem && actions?.onDirectDelete) {
      menu.push({
        id: 'direct-delete',
        label: t('drive.action.move-to-trash'),
        icon: <Trash2 className="size-4" aria-hidden />,
        danger: true,
        onSelect: () => actions.onDirectDelete?.(item),
      })
    }

    menu.push(
      { type: 'separator' },
      {
        id: 'copy-name',
        label: t('drive.copy.name'),
        icon: <Copy className="size-4" aria-hidden />,
        onSelect: () => void copyValue(item.name, t('drive.copy.name')),
      },
      {
        id: 'copy-id',
        label: t('drive.copy.id'),
        icon: <Hash className="size-4" aria-hidden />,
        onSelect: () => void copyValue(item.itemId, t('drive.copy.id')),
      },
    )
    return menu
  }

  const headerMenu: MenuEntry[] = [
    ...(canCreateFolder
      ? [
          {
            id: 'new-folder',
            label: t('drive.action.new-folder'),
            icon: <FolderPlus className="size-4" aria-hidden />,
            onSelect: () => setCreateFolderOpen(true),
          } satisfies MenuEntry,
          { type: 'separator' } satisfies MenuEntry,
        ]
      : []),
    ...(onUpload
      ? [
          {
            id: 'upload',
            label: t('drive.action.upload'),
            icon: <Upload className="size-4" aria-hidden />,
            onSelect: onUpload,
          } satisfies MenuEntry,
          { type: 'separator' } satisfies MenuEntry,
        ]
      : []),
    {
      id: 'refresh',
      label: t('table.refresh'),
      icon: <RefreshCw className="size-4" aria-hidden />,
      onSelect: () => void query.refetch(),
    },
  ]

  const columns: DataTableColumn<DriveItemResponse>[] = [
      {
        id: 'name',
        headerKey: 'drive.column.name',
        hideable: false,
        sortable: true,
        sortKey: 'name',
        width: '40%',
        cell: (item) => {
          const isFolder = item.itemKindId === DRIVE_ITEM_KIND.Folder
          const role = roles[item.itemId]
          const canDrag = role !== undefined && role >= DRIVE_ROLE.Editor
          return (
            <DriveDropTarget
              targetFolderId={isFolder ? item.itemId : null}
              onDropItem={handleDropItem}
              disabled={!isFolder}
            >
              <div
                draggable={canDrag}
                onDragStart={(event) => startItemDrag(event, item.itemId)}
                className="flex items-center gap-2"
              >
                <ItemKindIcon kindId={item.itemKindId} />
                <button
                  type="button"
                  onClick={() => (isFolder ? openFolder(item.itemId) : setSelectedItem(item))}
                  onDoubleClick={() => { if (!isFolder) openFile(item) }}
                  className="truncate text-start text-sm font-medium text-foreground transition-colors hover:text-accent"
                  title={item.name}
                >
                  {item.name}
                </button>
              </div>
            </DriveDropTarget>
          )
        },
      },
      {
        id: 'star',
        headerKey: 'drive.starred',
        sortable: false,
        width: '3rem',
        cell: (item) => (
          <button
            type="button"
            onClick={() => handleStar(item)}
            aria-label={t(item.starred ? 'drive.action.unstar' : 'drive.action.star')}
            className="flex size-7 items-center justify-center rounded transition-colors hover:bg-surface-muted"
          >
            <StarIndicator starred={item.starred} />
          </button>
        ),
      },
      {
        id: 'size',
        headerKey: 'drive.column.size',
        sortable: true,
        sortKey: 'size',
        cell: (item) => (
          <span dir="ltr" className="whitespace-nowrap text-xs text-muted">
            {item.itemKindId === DRIVE_ITEM_KIND.Folder ? '—' : formatFileSize(item.sizeBytes)}
          </span>
        ),
      },
      {
        id: 'owner',
        headerKey: 'drive.column.owner',
        defaultHidden: true,
        cell: (item) => (
          <span className="text-xs text-foreground">
            {ownerName[item.ownerUserId] ?? formatShortId(item.ownerUserId)}
          </span>
        ),
      },
      {
        id: 'modified',
        headerKey: 'drive.column.modified',
        sortable: true,
        sortKey: 'modifiedAt',
        cell: (item) => (
          <span dir="ltr" className="whitespace-nowrap text-xs text-muted">
            {formatDateTime(item.updatedAt ?? item.createdAt)}
          </span>
        ),
      },
      {
        id: 'kind',
        headerKey: 'drive.column.kind',
        defaultHidden: true,
        cell: (item) => (
          <span className="text-xs text-muted">
            {t(item.itemKindId === DRIVE_ITEM_KIND.Folder ? 'drive.item-kind.folder' : 'drive.item-kind.file')}
          </span>
        ),
      },
  ]

  const hasFilters = Boolean(table.search || table.filters.kind || table.filters.owner)
  const emptyTitle = hasFilters ? t('drive.browser.search-empty-title') : t('drive.browser.empty-title')
  const emptyDescription = hasFilters
    ? t('drive.browser.search-empty-description')
    : t('drive.browser.empty-description')
  const emptyAction = hasFilters ? (
    <Button type="button" variant="outline" onClick={() => table.clearQuery()}>
      <Label k="drive.browser.empty-clear" />
    </Button>
  ) : canCreateFolder ? (
    <Button type="button" onClick={() => setCreateFolderOpen(true)}>
      <FolderPlus className="size-4" aria-hidden />
      <Label k="drive.action.new-folder" />
    </Button>
  ) : onUpload ? (
    <Button type="button" onClick={onUpload}>
      <Upload className="size-4" aria-hidden />
      <Label k="drive.action.upload" />
    </Button>
  ) : undefined

  const controls = (
    <GuideRegion id="drive.upload">
      <DriveBrowserControls
        search={table.search}
        onSearchChange={table.setSearch}
        layout={layout}
        onLayoutChange={(next) => table.setFilter('layout', next === 'details' ? null : next)}
        canCreateFolder={canCreateFolder}
        onNewFolder={() => setCreateFolderOpen(true)}
        onUpload={onUpload}
        kind={table.filters.kind ?? ''}
        onKindChange={(value) => table.setFilter('kind', value === 'all' ? null : value)}
        owner={table.filters.owner ?? ''}
        onOwnerChange={(value) => table.setFilter('owner', value === 'all' ? null : value)}
        ownerOptions={ownerOptions}
        onRefresh={() => void query.refetch()}
        isFetching={query.isFetching}
      />
    </GuideRegion>
  )

  if (view === 'shared' && !activeSpace) {
    return (
      <div className="flex min-h-64 flex-col items-center justify-center gap-3 rounded-xl border border-dashed border-border bg-surface p-8 text-center">
        <FolderOpen className="size-10 text-muted" aria-hidden />
        <p className="max-w-md text-sm text-muted">
          <Label k="drive.browser.pick-space" />
        </p>
      </div>
    )
  }
  if (view === 'my-files' && !personal) {
    return (
      <div className="flex min-h-64 items-center justify-center gap-2 rounded-xl border border-border bg-surface p-8 text-sm text-muted">
        <Loader2 className="size-5 animate-spin" aria-hidden />
        <Label k="drive.state.loading" />
      </div>
    )
  }

  return (
    <div
      className="relative flex flex-col gap-4"
      onDragOver={(event) => {
        if (onDropFiles && Array.from(event.dataTransfer.types).includes('Files')) {
          event.preventDefault()
          setFileOver(true)
        }
      }}
      onDragLeave={(event) => {
        if (event.currentTarget === event.target) setFileOver(false)
      }}
      onDrop={(event) => {
        if (onDropFiles && event.dataTransfer.files.length > 0) {
          event.preventDefault()
          setFileOver(false)
          onDropFiles(Array.from(event.dataTransfer.files))
        }
      }}
    >
      {view !== 'starred' && (
        <GuideRegion id="drive.breadcrumb">
          <DriveBreadcrumb
            folderId={folderId}
            spaceName={activeSpace?.name ?? t('drive.my-files')}
            onNavigateFolder={onNavigateFolder}
            onDropItem={handleDropItem}
          />
        </GuideRegion>
      )}

      {fileOver && (
        <div className="pointer-events-none absolute inset-0 z-10 flex items-center justify-center rounded-xl border-2 border-dashed border-accent bg-accent/10">
          <p className="text-sm font-semibold text-accent">
            <Label k="drive.browser.drop-hint" />
          </p>
        </div>
      )}

      <GuideRegion id="drive.grid">
        <div className="flex min-w-0 flex-col gap-4 xl:flex-row xl:items-start">
          <div className="min-w-0 flex-1">
            {layout === 'details' ? (
              <DataTable
                tableId="drive-items"
                mode="server"
                caption={t('drive.browser.caption')}
                columns={columns}
                rows={items}
                rowKey={(item) => item.itemId}
                isLoading={query.isLoading}
                error={query.error}
                onRetry={() => void query.refetch()}
                isFetching={query.isFetching}
                page={table.page}
                pageSize={table.pageSize}
                totalCount={query.data?.totalCount ?? 0}
                onPageChange={table.setPage}
                onPageSizeChange={table.setPageSize}
                sortKey={table.sortKey}
                sortDirection={table.sortDirection}
                onSortChange={table.setSort}
                rowMenu={buildRowMenu}
                headerMenu={headerMenu}
                emptyMenu={headerMenu}
                rowLabel={(item) => item.name}
                toolbar={controls}
                emptyTitle={emptyTitle}
                emptyDescription={emptyDescription}
                emptyAction={emptyAction}
              />
            ) : (
              <div className="flex flex-col gap-3">
                {controls}
                {query.error && (
                  <div role="alert" className="flex flex-wrap items-center justify-between gap-3 rounded-lg border border-danger/40 bg-danger/10 px-3 py-2 text-sm text-danger">
                    <span>{toArabicErrorMessage(query.error)}</span>
                    <Button type="button" variant="outline" size="sm" onClick={() => void query.refetch()}>
                      <Label k="drive.retry" />
                    </Button>
                  </div>
                )}
                <DriveItemsGrid
                  items={items}
                  isLoading={query.isLoading}
                  roles={roles}
                  rowMenu={buildRowMenu}
                  onOpenFolder={openFolder}
                  onOpenFile={actions?.onOpenFile}
                  onSelectFile={setSelectedItem}
                  selectedItemId={selectedItem?.itemId ?? null}
                  layout={layout}
                  onDropItem={handleDropItem}
                  page={table.page}
                  pageSize={table.pageSize}
                  totalCount={query.data?.totalCount ?? 0}
                  onPageChange={table.setPage}
                  emptyTitle={emptyTitle}
                  emptyDescription={emptyDescription}
                  emptyAction={emptyAction}
                />
              </div>
            )}
          </div>
          {selectedItem && (
            <DrivePreviewPanel
              item={selectedItem}
              ownerName={ownerName[selectedItem.ownerUserId] ?? formatShortId(selectedItem.ownerUserId)}
              onClose={() => setSelectedItem(null)}
              onOpen={actions?.onOpenFile ? () => actions.onOpenFile?.(selectedItem) : undefined}
              onDownload={actions?.onDownload ? () => actions.onDownload?.(selectedItem) : undefined}
            />
          )}
        </div>
      </GuideRegion>

      {canCreateFolder && browseSpaceId && (
        <CreateFolderDialog
          spaceId={browseSpaceId}
          parentId={view === 'starred' ? null : folderId}
          open={createFolderOpen}
          onOpenChange={setCreateFolderOpen}
        />
      )}
      {renameTarget && (
        <RenameItemDialog
          item={renameTarget}
          open
          onOpenChange={(nextOpen) => {
            if (!nextOpen) setRenameTarget(null)
          }}
        />
      )}
      {moveTarget && (
        <MoveItemDialog
          item={moveTarget}
          open
          onOpenChange={(nextOpen) => {
            if (!nextOpen) setMoveTarget(null)
          }}
        />
      )}
    </div>
  )
}
