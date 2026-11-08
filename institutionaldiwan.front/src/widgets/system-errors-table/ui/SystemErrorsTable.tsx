import { useMemo, useState } from 'react'
import { Copy, Eye, FileSpreadsheet, RefreshCw, Trash2 } from 'lucide-react'
import { toast } from 'sonner'
import {
  SYSTEM_ERROR_KIND_LABELS,
  SYSTEM_ERROR_SOURCE_LABELS,
  SYSTEM_ERROR_STATUS_LABELS,
  SystemErrorStatusBadge,
  systemErrorApi,
  useSystemErrors,
  type SystemErrorFilters,
  type SystemErrorSortKey,
  type SystemErrorSummaryResponse,
} from '@/entities/system-error'
import { PurgeSystemErrorsDialog } from '@/features/system-error-manage'
import { toArabicErrorMessage } from '@/shared/api'
import { formatDateTime, formatNumber, parseContentDisposition, saveBlobAsFile, useTableState } from '@/shared/lib'
import {
  Button,
  DataTable,
  GuideChip,
  Input,
  Select,
  type DataTableColumn,
  type MenuEntry,
} from '@/shared/ui'
import { SystemErrorDetailsDialog } from './SystemErrorDetailsDialog'

const ALL = 'all'
const SORT_KEYS: readonly SystemErrorSortKey[] = ['lastSeenAt', 'firstSeenAt', 'occurrenceCount', 'title', 'status']

function resolveSortKey(raw: string | null): SystemErrorSortKey {
  return SORT_KEYS.find((key) => key === raw) ?? 'lastSeenAt'
}

function optionsOf(labels: Record<number, string>, allLabel: string) {
  return [{ value: ALL, label: allLabel }, ...Object.entries(labels).map(([value, label]) => ({ value, label }))]
}

const STATUS_OPTIONS = optionsOf(SYSTEM_ERROR_STATUS_LABELS, 'كل الحالات')
const SOURCE_OPTIONS = optionsOf(SYSTEM_ERROR_SOURCE_LABELS, 'كل المصادر')
const KIND_OPTIONS = optionsOf(SYSTEM_ERROR_KIND_LABELS, 'كل الأنواع')

function optionalNumber(value: string | undefined): number | undefined {
  return value ? Number(value) : undefined
}

/** `yyyy-mm-dd` from the date input → the UTC instant that starts (or ends) that local day. */
function dayBoundaryUtc(value: string | undefined, end: boolean): string | undefined {
  if (!value || !/^\d{4}-\d{2}-\d{2}$/.test(value)) return undefined
  const date = new Date(`${value}T${end ? '23:59:59.999' : '00:00:00'}`)
  return Number.isNaN(date.getTime()) ? undefined : date.toISOString()
}

/** The developer's grid of captured errors: filters in the URL, details, Excel export and manual cleanup. */
export function SystemErrorsTable() {
  const table = useTableState({
    defaultSortKey: 'lastSeenAt',
    defaultSortDirection: 'desc',
    filterKeys: ['status', 'source', 'kind', 'from', 'to'],
  })

  const filters: SystemErrorFilters = {
    search: table.search || undefined,
    sortBy: resolveSortKey(table.sortKey),
    sortDescending: table.sortDirection === 'desc',
    status: optionalNumber(table.filters.status),
    source: optionalNumber(table.filters.source),
    kind: optionalNumber(table.filters.kind),
    lastSeenFromUtc: dayBoundaryUtc(table.filters.from, false),
    lastSeenToUtc: dayBoundaryUtc(table.filters.to, true),
  }

  const errorsQuery = useSystemErrors({ page: table.page, pageSize: table.pageSize, ...filters })
  const [selectedId, setSelectedId] = useState<string | null>(null)
  const [purgeOpen, setPurgeOpen] = useState(false)
  const [exporting, setExporting] = useState(false)

  const exportToExcel = async () => {
    setExporting(true)
    try {
      const { blob, contentDisposition } = await systemErrorApi.exportFile(filters)
      saveBlobAsFile(blob, parseContentDisposition(contentDisposition) ?? 'system-errors.xlsx')
    } catch (error) {
      toast.error(toArabicErrorMessage(error))
    } finally {
      setExporting(false)
    }
  }

  const copyDetails = async (error: SystemErrorSummaryResponse) => {
    try {
      await navigator.clipboard.writeText(`${error.title}\n${error.message}`)
      toast.success('نُسخ الخطأ.')
    } catch {
      toast.error('تعذّر النسخ إلى الحافظة.')
    }
  }

  const columns = useMemo<DataTableColumn<SystemErrorSummaryResponse>[]>(
    () => [
      {
        id: 'title',
        header: 'الخطأ',
        sortable: true,
        sortKey: 'title',
        hideable: false,
        cell: (error) => (
          <button
            type="button"
            className="flex max-w-md cursor-pointer flex-col gap-0.5 text-start"
            onClick={() => setSelectedId(error.systemErrorId)}
          >
            <span dir="ltr" className="truncate text-start text-sm font-medium text-foreground hover:text-accent">
              {error.title}
            </span>
            <span dir="auto" className="line-clamp-1 text-start text-xs text-muted">
              {error.message}
            </span>
          </button>
        ),
      },
      {
        id: 'status',
        header: 'الحالة',
        sortable: true,
        sortKey: 'status',
        cell: (error) => <SystemErrorStatusBadge status={error.status} />,
      },
      { id: 'source', header: 'المصدر', cell: (error) => SYSTEM_ERROR_SOURCE_LABELS[error.source] },
      { id: 'kind', header: 'النوع', cell: (error) => SYSTEM_ERROR_KIND_LABELS[error.kind] },
      {
        id: 'path',
        header: 'المسار',
        cell: (error) => (
          <span dir="ltr" className="whitespace-nowrap text-xs text-muted">
            {error.httpMethod} {error.path ?? '—'}
          </span>
        ),
      },
      {
        id: 'occurrenceCount',
        header: 'المرات',
        sortable: true,
        sortKey: 'occurrenceCount',
        cell: (error) => (
          <span dir="ltr" className="text-sm text-foreground">
            {formatNumber(error.occurrenceCount)}
          </span>
        ),
      },
      {
        id: 'lastSeenAt',
        header: 'آخر ظهور',
        sortable: true,
        sortKey: 'lastSeenAt',
        cell: (error) => (
          <span dir="ltr" className="whitespace-nowrap text-xs text-foreground">
            {formatDateTime(error.lastSeenAt)}
          </span>
        ),
      },
      {
        id: 'firstSeenAt',
        header: 'أول ظهور',
        sortable: true,
        sortKey: 'firstSeenAt',
        defaultHidden: true,
        cell: (error) => (
          <span dir="ltr" className="whitespace-nowrap text-xs text-muted">
            {formatDateTime(error.firstSeenAt)}
          </span>
        ),
      },
      {
        id: 'lastUsername',
        header: 'آخر مستخدم',
        defaultHidden: true,
        cell: (error) => (
          <span dir="ltr" className="text-xs text-muted">
            {error.lastUsername ?? '—'}
          </span>
        ),
      },
    ],
    [],
  )

  const buildRowMenu = (error: SystemErrorSummaryResponse): MenuEntry[] => [
    {
      id: 'open',
      label: 'فتح التفاصيل',
      icon: <Eye className="size-4" aria-hidden />,
      onSelect: () => setSelectedId(error.systemErrorId),
    },
    {
      id: 'copy',
      label: 'نسخ الخطأ',
      icon: <Copy className="size-4" aria-hidden />,
      onSelect: () => void copyDetails(error),
    },
  ]

  const headerMenu: MenuEntry[] = [
    {
      id: 'export',
      label: 'تصدير إلى Excel',
      icon: <FileSpreadsheet className="size-4" aria-hidden />,
      disabled: exporting,
      onSelect: () => void exportToExcel(),
    },
    {
      id: 'purge',
      label: 'حذف الأخطاء القديمة',
      icon: <Trash2 className="size-4" aria-hidden />,
      onSelect: () => setPurgeOpen(true),
    },
    {
      id: 'refresh',
      label: 'تحديث البيانات',
      icon: <RefreshCw className="size-4" aria-hidden />,
      onSelect: () => void errorsQuery.refetch(),
    },
  ]

  const selectFilter = (key: string, label: string, options: { value: string; label: string }[]) => (
    <Select
      aria-label={label}
      value={table.filters[key] || ALL}
      onValueChange={(value) => table.setFilter(key, value === ALL ? null : value)}
      options={options}
      className="w-44"
    />
  )

  const hasQuery = Boolean(table.search) || table.hasActiveFilters

  return (
    <>
      <DataTable
        tableId="system-errors"
        caption="الأخطاء البرمجية"
        columns={columns}
        rows={errorsQuery.data?.items}
        rowKey={(error) => error.systemErrorId}
        isLoading={errorsQuery.isLoading}
        error={errorsQuery.error}
        onRetry={() => void errorsQuery.refetch()}
        onRefresh={() => void errorsQuery.refetch()}
        isFetching={errorsQuery.isFetching}
        page={table.page}
        pageSize={table.pageSize}
        totalCount={errorsQuery.data?.totalCount ?? 0}
        onPageChange={table.setPage}
        onPageSizeChange={table.setPageSize}
        search={table.search}
        onSearchChange={table.setSearch}
        sortKey={table.sortKey}
        sortDirection={table.sortDirection}
        onSortChange={table.setSort}
        searchPlaceholder="ابحث بالعنوان أو الرسالة أو المسار…"
        filters={
          <div className="flex flex-wrap items-center gap-2">
            <GuideChip id="system-errors.filters" />
            {selectFilter('status', 'تصفية حسب الحالة', STATUS_OPTIONS)}
            {selectFilter('source', 'تصفية حسب المصدر', SOURCE_OPTIONS)}
            {selectFilter('kind', 'تصفية حسب النوع', KIND_OPTIONS)}
            <label className="flex items-center gap-1.5 text-xs text-muted">
              ظهر من
              <Input
                type="date"
                dir="ltr"
                aria-label="آخر ظهور من"
                value={table.filters.from ?? ''}
                max={table.filters.to || undefined}
                onChange={(event) => table.setFilter('from', event.target.value || null)}
                className="h-10 w-40"
              />
            </label>
            <label className="flex items-center gap-1.5 text-xs text-muted">
              إلى
              <Input
                type="date"
                dir="ltr"
                aria-label="آخر ظهور إلى"
                value={table.filters.to ?? ''}
                min={table.filters.from || undefined}
                onChange={(event) => table.setFilter('to', event.target.value || null)}
                className="h-10 w-40"
              />
            </label>
            {table.hasActiveFilters && (
              <Button type="button" variant="ghost" size="sm" onClick={table.clearFilters}>
                مسح المرشحات
              </Button>
            )}
          </div>
        }
        rowMenu={buildRowMenu}
        headerMenu={headerMenu}
        emptyMenu={headerMenu}
        rowLabel={(error) => error.title}
        toolbar={
          <>
            <Button type="button" variant="outline" loading={exporting} onClick={() => void exportToExcel()}>
              <FileSpreadsheet className="size-4" aria-hidden />
              تصدير إلى Excel
            </Button>
            <GuideChip id="system-errors.export" />
            <Button type="button" variant="outline" onClick={() => setPurgeOpen(true)}>
              <Trash2 className="size-4" aria-hidden />
              حذف الأخطاء القديمة
            </Button>
            <GuideChip id="system-errors.purge" />
          </>
        }
        emptyTitle={hasQuery ? 'لا توجد أخطاء مطابقة.' : 'لا توجد أخطاء مسجلة.'}
        emptyDescription={
          hasQuery ? 'جرّب تعديل البحث أو المرشحات.' : 'ستظهر هنا الأخطاء التي تقع في الخادم أو في المتصفح.'
        }
        emptyAction={
          hasQuery ? (
            <Button type="button" variant="outline" onClick={table.clearQuery}>
              مسح البحث والمرشحات
            </Button>
          ) : undefined
        }
      />

      <SystemErrorDetailsDialog systemErrorId={selectedId} onClose={() => setSelectedId(null)} />
      {purgeOpen && <PurgeSystemErrorsDialog open onOpenChange={setPurgeOpen} />}
    </>
  )
}
