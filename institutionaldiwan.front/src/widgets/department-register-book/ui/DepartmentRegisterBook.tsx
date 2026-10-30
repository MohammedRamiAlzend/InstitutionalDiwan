import { useMemo } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { ArrowUpRight, Copy, FileText, Hash, RefreshCw } from 'lucide-react'
import { toast } from 'sonner'
import { useDepartmentOptions } from '@/entities/department'
import { NumberStatusBadge } from '@/entities/document'
import {
  useDepartmentRegister,
  type DepartmentRegisterLineResponse,
} from '@/entities/numbering-registry'
import { ROUTES } from '@/shared/config'
import { formatDate, formatNumber, useTableState } from '@/shared/lib'
import {
  Badge,
  Button,
  DataTable,
  Input,
  Select,
  type DataTableColumn,
  type MenuEntry,
} from '@/shared/ui'

/** `/numbering?tab=register` — the numbers departments wrote in their own registers. */
export function DepartmentRegisterBook() {
  const table = useTableState({
    defaultSortKey: 'registryDate',
    defaultSortDirection: 'desc',
    filterKeys: ['departmentId', 'year'],
  })
  const navigate = useNavigate()

  const departmentId = table.filters.departmentId ?? ''
  const yearText = table.filters.year ?? ''
  const numberingYear = /^\d{4}$/.test(yearText) ? Number(yearText) : null

  const departmentsQuery = useDepartmentOptions()
  const registerQuery = useDepartmentRegister(departmentId || null, numberingYear)
  const lines = useMemo(() => registerQuery.data ?? [], [registerQuery.data])

  const departmentOptions = useMemo(
    () =>
      (departmentsQuery.data ?? []).map((department) => ({
        value: department.departmentId,
        label: `${department.code} — ${department.nameAr}${department.isActive ? '' : ' (موقوف)'}`,
      })),
    [departmentsQuery.data],
  )

  const copyValue = async (value: string, label: string) => {
    try {
      await navigator.clipboard.writeText(value)
      toast.success(`تم نسخ ${label}.`)
    } catch {
      toast.error('تعذّر النسخ إلى الحافظة.')
    }
  }

  const openDocument = (line: DepartmentRegisterLineResponse) =>
    navigate(`${ROUTES.DOCUMENT_DETAIL(line.documentId)}?tab=numbers`)

  const columns = useMemo<DataTableColumn<DepartmentRegisterLineResponse>[]>(
    () => [
      {
        id: 'document',
        header: 'الوثيقة',
        hideable: false,
        sortable: true,
        sortValue: (line) => line.documentTitle,
        cell: (line) => (
          <Link
            to={`${ROUTES.DOCUMENT_DETAIL(line.documentId)}?tab=numbers`}
            className="inline-flex items-center gap-1.5 text-sm text-accent transition-colors hover:underline"
          >
            <FileText className="size-3.5 shrink-0" aria-hidden />
            <span className="line-clamp-1 max-w-72">{line.documentTitle}</span>
          </Link>
        ),
      },
      {
        id: 'centralNumber',
        header: 'رقم الديوان',
        sortable: true,
        sortValue: (line) => line.centralNumberText,
        cell: (line) => (
          <span dir="ltr" className="whitespace-nowrap text-xs text-muted">
            {line.centralNumberText ?? '—'}
          </span>
        ),
      },
      {
        id: 'registryNumber',
        header: 'رقم الدائرة',
        sortable: true,
        sortValue: (line) => line.registryNumberText,
        cell: (line) => (
          <code dir="ltr" className="whitespace-nowrap text-xs text-foreground">
            {line.registryNumberText}
          </code>
        ),
      },
      {
        id: 'numberingYear',
        header: 'السنة',
        sortable: true,
        sortValue: (line) => line.numberingYear,
        cell: (line) => (
          <span dir="ltr" className="whitespace-nowrap text-xs text-muted">
            {line.numberingYear}
          </span>
        ),
      },
      {
        id: 'registryDate',
        header: 'تاريخ التسجيل',
        sortable: true,
        sortValue: (line) => line.registryDate,
        cell: (line) => (
          <span dir="ltr" className="whitespace-nowrap text-xs text-muted">
            {formatDate(line.registryDate)}
          </span>
        ),
      },
      {
        id: 'status',
        header: 'الحالة',
        cell: (line) => <NumberStatusBadge statusId={line.statusId} />,
      },
      {
        id: 'registrationCount',
        header: 'مرات التسجيل',
        sortable: true,
        sortValue: (line) => line.registrationCount,
        cell: (line) => (
          <Badge variant="outline">{formatNumber(line.registrationCount)}</Badge>
        ),
      },
    ],
    [],
  )

  const buildRowMenu = (line: DepartmentRegisterLineResponse): MenuEntry[] => [
    {
      id: 'open-document',
      label: 'فتح الوثيقة',
      icon: <ArrowUpRight className="size-4" aria-hidden />,
      onSelect: () => openDocument(line),
    },
    { type: 'separator' },
    {
      id: 'copy-central-number',
      label: 'نسخ رقم الديوان',
      icon: <Copy className="size-4" aria-hidden />,
      disabled: !line.centralNumberText,
      onSelect: () =>
        line.centralNumberText &&
        void copyValue(line.centralNumberText, 'رقم الديوان'),
    },
    {
      id: 'copy-registry-number',
      label: 'نسخ رقم الدائرة',
      icon: <Copy className="size-4" aria-hidden />,
      onSelect: () => void copyValue(line.registryNumberText, 'رقم الدائرة'),
    },
    {
      id: 'copy-document-id',
      label: 'نسخ معرّف الوثيقة',
      icon: <Hash className="size-4" aria-hidden />,
      onSelect: () => void copyValue(line.documentId, 'معرّف الوثيقة'),
    },
  ]

  const headerMenu: MenuEntry[] = [
    {
      id: 'refresh',
      label: 'تحديث البيانات',
      icon: <RefreshCw className="size-4" aria-hidden />,
      onSelect: () => void registerQuery.refetch(),
    },
  ]

  const yearScopeLabel = yearText ? ` في سنة ${yearText}` : ''

  return (
    <DataTable
      tableId="department-register"
      mode="client"
      caption="سجل أرقام الدوائر"
      columns={columns}
      rows={lines}
      rowKey={(line) => line.documentNumberId}
      rowSearchText={(line) =>
        [
          line.documentTitle,
          line.centralNumberText,
          line.registryNumberText,
          String(line.numberingYear),
        ]
          .filter(Boolean)
          .join(' ')
          .toLocaleLowerCase('ar')
      }
      rowLabel={(line) => line.documentTitle}
      isLoading={Boolean(departmentId) && registerQuery.isLoading}
      error={departmentId ? registerQuery.error : null}
      onRetry={() => void registerQuery.refetch()}
      onRefresh={() => void registerQuery.refetch()}
      isFetching={registerQuery.isFetching}
      page={table.page}
      pageSize={table.pageSize}
      totalCount={lines.length}
      onPageChange={table.setPage}
      onPageSizeChange={table.setPageSize}
      search={table.search}
      onSearchChange={table.setSearch}
      searchPlaceholder="ابحث بالوثيقة أو رقم الديوان أو رقم الدائرة…"
      sortKey={table.sortKey}
      sortDirection={table.sortDirection}
      onSortChange={table.setSort}
      rowMenu={buildRowMenu}
      headerMenu={headerMenu}
      emptyMenu={headerMenu}
      filters={
        <div className="flex flex-wrap items-end gap-2">
          <Select
            aria-label="تصفية حسب الدائرة"
            value={departmentId || undefined}
            onValueChange={(value) => table.setFilter('departmentId', value)}
            options={departmentOptions}
            disabled={departmentsQuery.isLoading}
            placeholder={
              departmentsQuery.isLoading ? 'جارٍ التحميل…' : 'اختر الدائرة…'
            }
            className="w-64"
          />
          <Input
            aria-label="تصفية حسب سنة الترقيم"
            type="number"
            inputMode="numeric"
            dir="ltr"
            min={1900}
            max={2200}
            value={yearText}
            onChange={(event) => table.setFilter('year', event.target.value)}
            placeholder="كل السنوات"
            className="w-32"
          />
          {table.hasActiveFilters && (
            <Button type="button" variant="ghost" size="sm" onClick={table.clearFilters}>
              مسح المرشحات
            </Button>
          )}
        </div>
      }
      emptyTitle={
        !departmentId
          ? 'اختر دائرة لعرض سجلها'
          : table.search
            ? 'لا توجد نتائج مطابقة.'
            : 'لا توجد أرقام مسجّلة لهذه الدائرة.'
      }
      emptyDescription={
        !departmentId
          ? 'يعرض السجل الأرقام التي كتبتها الدائرة في سجلها الخاص مقابل رقم الديوان المركزي.'
          : table.search
            ? 'جرّب تعديل البحث أو السنة، أو امسح البحث لعرض كل الأرقام.'
            : `لم تُسجَّل أرقام من هذه الدائرة${yearScopeLabel} بعد.`
      }
      emptyAction={
        !departmentId ? undefined : table.search ? (
          <Button type="button" variant="outline" onClick={() => table.setSearch('')}>
            مسح البحث
          </Button>
        ) : undefined
      }
    />
  )
}
