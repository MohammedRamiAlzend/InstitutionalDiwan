import { useMemo, useState } from 'react'
import { Copy, Pencil, Plus, Power, PowerOff, RefreshCw } from 'lucide-react'
import { toast } from 'sonner'
import { LOOKUP_FALLBACKS, useLookupEntries, type LookupEntryResponse } from '@/entities/lookup'
import {
  LookupEntryFormDialog,
  useSetLookupEntryActiveMutation,
} from '@/features/lookup-manage'
import { toArabicErrorMessage } from '@/shared/api'
import { lookupCatalogMeta } from '@/shared/config'
import { formatDate, useTableState } from '@/shared/lib'
import {
  Badge,
  Button,
  ConfirmDialog,
  DataTable,
  GuideChip,
  Select,
  type DataTableColumn,
  type MenuEntry,
} from '@/shared/ui'

interface LookupEntriesTableProps {
  lookup: string
}

const STATUS_OPTIONS = [
  { value: 'all', label: 'كل القيم' },
  { value: 'active', label: 'النشطة فقط' },
  { value: 'inactive', label: 'الموقوفة فقط' },
]

/** Entries of one reference catalog (client-mode grid) with add, relabel and status actions. */
export function LookupEntriesTable({ lookup }: LookupEntriesTableProps) {
  const meta = lookupCatalogMeta(lookup)
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
      toast.success('تم نسخ المعرّف.')
    } catch {
      toast.error('تعذّر النسخ إلى الحافظة.')
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
      toast.success(statusEntry.isActive ? 'تم إيقاف القيمة.' : 'تم تفعيل القيمة.')
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
        header: 'المعرّف',
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
        header: 'التسمية العربية',
        hideable: false,
        sortable: true,
        sortValue: (entry) => entry.descAr,
        cell: (entry) => (
          <span className="flex items-center gap-2 text-sm text-foreground">
            {entry.descAr}
            {systemIds.has(entry.id) && <Badge variant="outline">نظامية</Badge>}
          </span>
        ),
      },
      {
        id: 'desc',
        header: 'التسمية الإنجليزية',
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
        header: 'الحالة',
        sortable: true,
        sortValue: (entry) => (entry.isActive ? 1 : 0),
        cell: (entry) => (
          <Badge variant={entry.isActive ? 'success' : 'danger'}>
            {entry.isActive ? 'نشطة' : 'موقوفة'}
          </Badge>
        ),
      },
      {
        id: 'createdAt',
        header: 'تاريخ الإنشاء',
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
      label: 'تعديل التسمية',
      icon: <Pencil className="size-4" aria-hidden />,
      onSelect: () => openEdit(entry),
    },
    {
      id: 'toggle-active',
      label: entry.isActive ? 'إيقاف القيمة' : 'تفعيل القيمة',
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
      label: 'نسخ المعرّف',
      icon: <Copy className="size-4" aria-hidden />,
      onSelect: () => void copyId(entry),
    },
  ]

  const createEntry: MenuEntry = {
    id: 'create',
    label: 'قيمة جديدة',
    icon: <Plus className="size-4" aria-hidden />,
    disabled: !meta.canCreate,
    onSelect: openCreate,
  }
  const refreshEntry: MenuEntry = {
    id: 'refresh',
    label: 'تحديث البيانات',
    icon: <RefreshCw className="size-4" aria-hidden />,
    onSelect: () => void entriesQuery.refetch(),
  }

  const hasQuery = Boolean(table.search) || table.hasActiveFilters

  return (
    <>
      <DataTable
        tableId="lookup-entries"
        mode="client"
        caption={meta.labelAr}
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
        searchPlaceholder="ابحث بالمعرّف أو التسمية…"
        sortKey={table.sortKey}
        sortDirection={table.sortDirection}
        onSortChange={table.setSort}
        filters={
          <div className="flex flex-wrap items-center gap-2">
            <GuideChip id="lookups.status" />
            <Select
              aria-label="تصفية حسب الحالة"
              value={status || 'all'}
              onValueChange={(value) => table.setFilter('status', value === 'all' ? null : value)}
              options={STATUS_OPTIONS}
              className="w-44"
            />
            {table.hasActiveFilters && (
              <Button type="button" variant="ghost" size="sm" onClick={table.clearFilters}>
                مسح المرشحات
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
              title={meta.canCreate ? undefined : 'هذا الجدول لا يقبل إضافة قيم من هذه الشاشة.'}
            >
              <Plus className="size-4" aria-hidden />
              قيمة جديدة
            </Button>
            <GuideChip id="lookups.add" />
          </>
        }
        emptyTitle={hasQuery ? 'لا توجد قيم مطابقة.' : 'لا توجد قيم في هذا الجدول.'}
        emptyDescription={
          hasQuery ? 'جرّب تعديل البحث أو المرشحات.' : 'أضف أول قيمة مرجعية لهذا الجدول.'
        }
        emptyAction={
          hasQuery ? (
            <Button type="button" variant="outline" onClick={table.clearQuery}>
              مسح البحث والمرشحات
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
        title={statusEntry?.isActive ? 'إيقاف القيمة' : 'تفعيل القيمة'}
        description={
          statusEntry
            ? statusEntry.isActive
              ? `ستُوقف القيمة «${statusEntry.descAr}» ولن تظهر في القوائم المنسدلة. يُرفض الإيقاف إذا كانت سجلات أخرى تشير إليها.${systemIds.has(statusEntry.id) ? ' تنبيه: هذه قيمة نظامية يعتمد عليها سلوك النظام.' : ''}`
              : `ستعود القيمة «${statusEntry.descAr}» متاحة للاختيار في القوائم المنسدلة.`
            : undefined
        }
        confirmLabel={statusEntry?.isActive ? 'إيقاف القيمة' : 'تفعيل القيمة'}
        destructive={statusEntry?.isActive}
        loading={statusMutation.isPending}
        onConfirm={() => void handleToggleStatus()}
      />
    </>
  )
}
