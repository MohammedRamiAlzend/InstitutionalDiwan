import { useMemo, useState } from 'react'
import { Copy, Pencil, Plus, Power, PowerOff, RefreshCw, Star, StarOff } from 'lucide-react'
import { toast } from 'sonner'
import { LOOKUP_FALLBACKS, useLookupEntries, type LookupEntryResponse } from '@/entities/lookup'
import {
  LookupEntryFormDialog,
  useSetLookupEntryActiveMutation,
  useSetLookupEntryDefaultMutation,
} from '@/features/lookup-manage'
import { toArabicErrorMessage } from '@/shared/api'
import { lookupCatalogMeta } from '@/shared/config'
import { formatDate, resolveLabel, t, useLabelsStore, useTableState } from '@/shared/lib'
import {
  Badge,
  Button,
  ConfirmDialog,
  DataTable,
  GuideChip,
  Label,
  Select,
  type DataTableColumn,
  type MenuEntry,
} from '@/shared/ui'

interface LookupEntriesTableProps {
  lookup: string
}

/** Entries of one reference catalog (client-mode grid) with add, relabel and status actions. */
export function LookupEntriesTable({ lookup }: LookupEntriesTableProps) {
  const meta = lookupCatalogMeta(lookup)
  const overrides = useLabelsStore((state) => state.overrides)
  const caption = meta.labelKey ? resolveLabel(meta.labelKey, overrides) : lookup
  const statusOptions = [
    { value: 'all', label: resolveLabel('lookup.filter.all', overrides) },
    { value: 'active', label: resolveLabel('lookup.filter.active', overrides) },
    { value: 'inactive', label: resolveLabel('lookup.filter.inactive', overrides) },
  ]
  const table = useTableState({
    defaultSortKey: 'id',
    defaultSortDirection: 'asc',
    filterKeys: ['status'],
  })
  const entriesQuery = useLookupEntries(lookup, { includeInactive: true })
  const allEntries = useMemo(() => entriesQuery.data ?? [], [entriesQuery.data])
  const status = table.filters.status
  const entries = useMemo(
    () =>
      allEntries.filter((entry) =>
        status === 'active' ? entry.isActive : status === 'inactive' ? !entry.isActive : true,
      ),
    [allEntries, status],
  )

  // Ids that front-end constants rely on; the server only blocks entries already referenced.
  const systemIds = useMemo(
    () => new Set((LOOKUP_FALLBACKS[lookup] ?? []).map((entry) => entry.id)),
    [lookup],
  )
  const suggestedId = allEntries.reduce((max, entry) => Math.max(max, entry.id), 0) + 1

  const [formOpen, setFormOpen] = useState(false)
  const [formEntry, setFormEntry] = useState<LookupEntryResponse | null>(null)
  const [statusEntryId, setStatusEntryId] = useState<number | null>(null)
  const statusEntry = allEntries.find((entry) => entry.id === statusEntryId) ?? null
  const statusMutation = useSetLookupEntryActiveMutation()
  const defaultMutation = useSetLookupEntryDefaultMutation()

  const openCreate = () => {
    setFormEntry(null)
    setFormOpen(true)
  }
  const openEdit = (entry: LookupEntryResponse) => {
    setFormEntry(entry)
    setFormOpen(true)
  }

  const copyId = async (entry: LookupEntryResponse) => {
    try {
      await navigator.clipboard.writeText(String(entry.id))
      toast.success(t('lookup.toast.copied'))
    } catch {
      toast.error(t('lookup.toast.copy-failed'))
    }
  }

  const handleToggleDefault = async (entry: LookupEntryResponse) => {
    try {
      await defaultMutation.mutateAsync({ lookup, id: entry.id, isDefault: !entry.isDefault })
      toast.success(entry.isDefault ? t('lookup.toast.default-removed') : t('lookup.toast.default-set'))
    } catch (error) {
      toast.error(toArabicErrorMessage(error))
    }
  }

  const handleToggleStatus = async () => {
    if (!statusEntry) return
    try {
      await statusMutation.mutateAsync({
        lookup,
        id: statusEntry.id,
        active: !statusEntry.isActive,
      })
      toast.success(statusEntry.isActive ? t('lookup.toast.suspended') : t('lookup.toast.activated'))
      setStatusEntryId(null)
    } catch (error) {
      toast.error(toArabicErrorMessage(error))
      setStatusEntryId(null)
    }
  }

  const columns = useMemo<DataTableColumn<LookupEntryResponse>[]>(
    () => [
      {
        id: 'id',
        headerKey: 'lookup.column.id',
        hideable: false,
        sortable: true,
        sortValue: (entry) => entry.id,
        cell: (entry) => (
          <span dir="ltr" className="font-mono text-xs text-foreground">
            {entry.id}
          </span>
        ),
      },
      {
        id: 'descAr',
        headerKey: 'lookup.column.desc-ar',
        hideable: false,
        sortable: true,
        sortValue: (entry) => entry.descAr,
        cell: (entry) => (
          <span className="flex items-center gap-2 text-sm text-foreground">
            {entry.descAr}
            {systemIds.has(entry.id) && <Badge variant="outline"><Label k="lookup.badge.system" /></Badge>}
            {entry.isDefault && <Badge variant="accent"><Label k="lookup.badge.default" /></Badge>}
          </span>
        ),
      },
      {
        id: 'desc',
        headerKey: 'lookup.column.desc',
        sortable: true,
        sortValue: (entry) => entry.desc,
        cell: (entry) => (
          <span dir="ltr" className="text-xs text-muted">
            {entry.desc}
          </span>
        ),
      },
      {
        id: 'status',
        headerKey: 'lookup.column.status',
        sortable: true,
        sortValue: (entry) => (entry.isActive ? 1 : 0),
        cell: (entry) => (
          <Badge variant={entry.isActive ? 'success' : 'danger'}>
            {entry.isActive ? t('lookup.status.active') : t('lookup.status.inactive')}
          </Badge>
        ),
      },
      {
        id: 'createdAt',
        headerKey: 'lookup.column.created-at',
        defaultHidden: true,
        sortable: true,
        sortValue: (entry) => entry.createdAt,
        cell: (entry) => (
          <span dir="ltr" className="whitespace-nowrap text-xs text-muted">
            {entry.createdAt ? formatDate(entry.createdAt) : '—'}
          </span>
        ),
      },
    ],
    [systemIds],
  )

  const buildRowMenu = (entry: LookupEntryResponse): MenuEntry[] => [
    {
      id: 'edit',
      label: t('lookup.menu.edit'),
      icon: <Pencil className="size-4" aria-hidden />,
      onSelect: () => openEdit(entry),
    },
    {
      id: 'toggle-default',
      label: entry.isDefault ? t('lookup.menu.remove-default') : t('lookup.menu.set-default'),
      icon: entry.isDefault ? (
        <StarOff className="size-4" aria-hidden />
      ) : (
        <Star className="size-4" aria-hidden />
      ),
      disabled: !entry.isDefault && !entry.isActive,
      onSelect: () => void handleToggleDefault(entry),
    },
    {
      id: 'toggle-active',
      label: entry.isActive ? t('lookup.menu.suspend') : t('lookup.menu.activate'),
      icon: entry.isActive ? (
        <PowerOff className="size-4" aria-hidden />
      ) : (
        <Power className="size-4" aria-hidden />
      ),
      danger: entry.isActive,
      onSelect: () => setStatusEntryId(entry.id),
    },
    { type: 'separator' },
    {
      id: 'copy-id',
      label: t('lookup.menu.copy-id'),
      icon: <Copy className="size-4" aria-hidden />,
      onSelect: () => void copyId(entry),
    },
  ]

  const createEntry: MenuEntry = {
    id: 'create',
    label: t('lookup.create'),
    icon: <Plus className="size-4" aria-hidden />,
    disabled: !meta.canCreate,
    onSelect: openCreate,
  }
  const refreshEntry: MenuEntry = {
    id: 'refresh',
    label: t('table.refresh'),
    icon: <RefreshCw className="size-4" aria-hidden />,
    onSelect: () => void entriesQuery.refetch(),
  }

  const hasQuery = Boolean(table.search) || table.hasActiveFilters

  return (
    <>
      <DataTable
        tableId="lookup-entries"
        mode="client"
        caption={caption}
        columns={columns}
        rows={entries}
        rowKey={(entry) => String(entry.id)}
        rowSearchText={(entry) =>
          `${entry.id} ${entry.descAr} ${entry.desc}`.toLocaleLowerCase('ar')
        }
        isLoading={entriesQuery.isLoading}
        error={entriesQuery.error}
        onRetry={() => void entriesQuery.refetch()}
        onRefresh={() => void entriesQuery.refetch()}
        isFetching={entriesQuery.isFetching}
        page={table.page}
        pageSize={table.pageSize}
        totalCount={entries.length}
        onPageChange={table.setPage}
        onPageSizeChange={table.setPageSize}
        search={table.search}
        onSearchChange={table.setSearch}
        searchPlaceholder={t('lookup.search-placeholder')}
        sortKey={table.sortKey}
        sortDirection={table.sortDirection}
        onSortChange={table.setSort}
        filters={
          <div className="flex flex-wrap items-center gap-2">
            <GuideChip id="lookups.status" />
            <Select
              aria-label={t('lookup.filter.status-aria')}
              value={status || 'all'}
              onValueChange={(value) => table.setFilter('status', value === 'all' ? null : value)}
              options={statusOptions}
              className="w-44"
            />
            {table.hasActiveFilters && (
              <Button type="button" variant="ghost" size="sm" onClick={table.clearFilters}>
                <Label k="lookup.filter.clear" />
              </Button>
            )}
          </div>
        }
        rowMenu={buildRowMenu}
        headerMenu={[createEntry, refreshEntry]}
        emptyMenu={[createEntry, refreshEntry]}
        rowLabel={(entry) => entry.descAr}
        toolbar={
          <>
            <Button
              type="button"
              onClick={openCreate}
              disabled={!meta.canCreate}
              title={meta.canCreate ? undefined : t('lookup.toolbar.create-denied')}
            >
              <Plus className="size-4" aria-hidden />
              <Label k="lookup.create" />
            </Button>
            <GuideChip id="lookups.add" />
          </>
        }
        emptyTitle={hasQuery ? t('lookup.empty.search-title') : t('lookup.empty.title')}
        emptyDescription={
          hasQuery ? t('lookup.empty.search-description') : t('lookup.empty.description')
        }
        emptyAction={
          hasQuery ? (
            <Button type="button" variant="outline" onClick={table.clearQuery}>
              <Label k="lookup.empty.clear" />
            </Button>
          ) : undefined
        }
      />

      {formOpen && (
        <LookupEntryFormDialog
          key={`${lookup}-${formEntry?.id ?? 'create'}`}
          open
          onOpenChange={setFormOpen}
          lookup={lookup}
          entry={formEntry}
          suggestedId={suggestedId}
        />
      )}

      <ConfirmDialog
        open={statusEntry !== null}
        onOpenChange={(open) => {
          if (!open) setStatusEntryId(null)
        }}
        title={statusEntry?.isActive ? t('lookup.menu.suspend') : t('lookup.menu.activate')}
        description={
          statusEntry
            ? statusEntry.isActive
              ? t('lookup.confirm.suspend-description', { name: statusEntry.descAr }) +
                (systemIds.has(statusEntry.id) ? t('lookup.confirm.suspend-system-note') : '')
              : t('lookup.confirm.activate-description', { name: statusEntry.descAr })
            : undefined
        }
        confirmLabel={statusEntry?.isActive ? t('lookup.menu.suspend') : t('lookup.menu.activate')}
        destructive={statusEntry?.isActive}
        loading={statusMutation.isPending}
        onConfirm={() => void handleToggleStatus()}
      />
    </>
  )
}
