import { useMemo } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { ArrowUpRight, Copy, FileText, Hash, RefreshCw } from 'lucide-react'
import { toast } from 'sonner'
import {
  documentRowLabel,
  isDocumentRestricted,
  NumberStatusBadge,
  RestrictedDocumentLabel,
} from '@/entities/document'
import {
  useDepartmentRegister,
  useNumberingRegistries,
  type DepartmentRegisterLineResponse,
} from '@/entities/numbering-registry'
import { useMyPrimaryDepartmentId } from '@/entities/user'
import { DOCUMENT_NUMBER_STATUS, ROUTES } from '@/shared/config'
import { formatDate, formatNumber, useTableState } from '@/shared/lib'
import {
  Badge,
  Button,
  DataTable,
  GuideRegion,
  Input,
  Select,
  type DataTableColumn,
  type MenuEntry,
} from '@/shared/ui'

const STATUS_OPTIONS = [
  { value: 'all', label: 'كل الحالات' },
  { value: String(DOCUMENT_NUMBER_STATUS.Active), label: 'نشط' },
  { value: String(DOCUMENT_NUMBER_STATUS.Replaced), label: 'مُستبدل' },
  { value: String(DOCUMENT_NUMBER_STATUS.Cancelled), label: 'ملغى' },
] as const

/** A date input's `yyyy-mm-dd` against a line's ISO date; null when the line has no date. */
function dayOf(line: DepartmentRegisterLineResponse): string | null {
  return line.registryDate ? line.registryDate.slice(0, 10) : null
}

/**
 * `/my-registers` — pick one of the registers of the caller's own department and browse its lines.
 * The department book is read once; the register, year, status and date filters narrow it in the
 * browser and live in the URL, like the search, sort and page.
 */
export function MyRegistersTable() {
  const table = useTableState({
    defaultSortKey: 'registryDate',
    defaultSortDirection: 'desc',
    filterKeys: ['registry', 'year', 'status', 'from', 'to'],
  })
  const navigate = useNavigate()

  const { departmentId, isLoading: isLoadingDepartment } = useMyPrimaryDepartmentId()
  const registriesQuery = useNumberingRegistries({ enabled: Boolean(departmentId) })
  const bookQuery = useDepartmentRegister(departmentId, null)
  const book = useMemo(() => bookQuery.data ?? [], [bookQuery.data])

  const registries = useMemo(
    () =>
      (registriesQuery.data ?? []).filter((registry) => registry.departmentId === departmentId),
    [registriesQuery.data, departmentId],
  )

  const lineCount = useMemo(() => {
    const counts = new Map<string, number>()
    for (const line of book) {
      if (line.registryId) counts.set(line.registryId, (counts.get(line.registryId) ?? 0) + 1)
    }
    return counts
  }, [book])

  const registryOptions = useMemo(
    () =>
      registries.map((registry) => ({
        value: registry.registryId,
        label: `${registry.code} — ${registry.nameAr} (${formatNumber(
          lineCount.get(registry.registryId) ?? 0,
        )})${registry.isActive ? '' : ' (موقوف)'}`,
      })),
    [registries, lineCount],
  )

  // A department with a single register needs no choosing.
  const registryId =
    table.filters.registry || (registries.length === 1 ? registries[0].registryId : '')
  const yearText = table.filters.year ?? ''
  const year = /^\d{4}$/.test(yearText) ? Number(yearText) : null
  const statusId = table.filters.status ?? ''
  const from = table.filters.from ?? ''
  const to = table.filters.to ?? ''

  const lines = useMemo(
    () =>
      book.filter((line) => {
        if (line.registryId !== registryId) return false
        if (year !== null && line.numberingYear !== year) return false
        if (statusId && String(line.statusId) !== statusId) return false
        const day = dayOf(line)
        if (from && (!day || day < from)) return false
        if (to && (!day || day > to)) return false
        return true
      }),
    [book, registryId, year, statusId, from, to],
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
        id: 'registryNumber',
        header: 'رقم السجل',
        hideable: false,
        sortable: true,
        sortValue: (line) => line.registryNumberText,
        cell: (line) => (
          <code dir="ltr" className="whitespace-nowrap text-xs text-foreground">
            {line.registryNumberText}
          </code>
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
        id: 'document',
        header: 'الوثيقة',
        sortable: true,
        sortValue: (line) => line.documentTitle,
        cell: (line) =>
          isDocumentRestricted(line.documentTitle) ? (
            <RestrictedDocumentLabel />
          ) : (
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
        id: 'linkedNumber',
        header: 'الرقم المرتبط',
        sortable: true,
        sortValue: (line) => line.replacedByNumberText ?? line.replacesNumberText,
        cell: (line) => {
          const linked = line.replacedByNumberText ?? line.replacesNumberText
          if (!linked) return <span className="text-xs text-muted">—</span>
          return (
            <span className="inline-flex items-center gap-1.5 whitespace-nowrap text-xs text-muted">
              {line.replacedByNumberText ? 'استُبدل بـ' : 'بدل'}
              <code dir="ltr" className="text-foreground">
                {linked}
              </code>
            </span>
          )
        },
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
        id: 'status',
        header: 'الحالة',
        cell: (line) => <NumberStatusBadge statusId={line.statusId} />,
      },
      {
        id: 'registrationCount',
        header: 'مرات التسجيل',
        sortable: true,
        sortValue: (line) => line.registrationCount,
        cell: (line) => <Badge variant="outline">{formatNumber(line.registrationCount)}</Badge>,
      },
    ],
    [],
  )

  const buildRowMenu = (line: DepartmentRegisterLineResponse): MenuEntry[] => [
    ...(isDocumentRestricted(line.documentTitle)
      ? []
      : ([
          {
            id: 'open-document',
            label: 'فتح الوثيقة',
            icon: <ArrowUpRight className="size-4" aria-hidden />,
            onSelect: () => openDocument(line),
          },
          { type: 'separator' },
        ] as MenuEntry[])),
    {
      id: 'copy-registry-number',
      label: 'نسخ رقم السجل',
      icon: <Copy className="size-4" aria-hidden />,
      onSelect: () => void copyValue(line.registryNumberText, 'رقم السجل'),
    },
    {
      id: 'copy-central-number',
      label: 'نسخ رقم الديوان',
      icon: <Copy className="size-4" aria-hidden />,
      disabled: !line.centralNumberText,
      onSelect: () =>
        line.centralNumberText && void copyValue(line.centralNumberText, 'رقم الديوان'),
    },
    {
      id: 'copy-document-id',
      label: 'نسخ معرّف الوثيقة',
      icon: <Hash className="size-4" aria-hidden />,
      onSelect: () => void copyValue(line.documentId, 'معرّف الوثيقة'),
    },
  ]

  const refetch = () => {
    void registriesQuery.refetch()
    void bookQuery.refetch()
  }

  const headerMenu: MenuEntry[] = [
    {
      id: 'refresh',
      label: 'تحديث البيانات',
      icon: <RefreshCw className="size-4" aria-hidden />,
      onSelect: refetch,
    },
  ]

  const isLoading = isLoadingDepartment || registriesQuery.isLoading || bookQuery.isLoading
  const noRegisters = !isLoading && Boolean(departmentId) && registries.length === 0
  const hasNarrowing = Boolean(year !== null || statusId || from || to || table.search)

  const emptyTitle = noRegisters
    ? 'لا سجلات لقسمك'
    : !registryId
      ? 'اختر سجلًا لعرض أسطره'
      : hasNarrowing
        ? 'لا توجد نتائج مطابقة.'
        : 'لا أسطر في هذا السجل بعد.'
  const emptyDescription = noRegisters
    ? 'لم يُنشأ لقسمك سجل ترقيم. يُنشئه مسؤول الترقيم من صفحة «الترقيم».'
    : !registryId
      ? 'تظهر هنا الأسطر المسجّلة في السجل الذي تختاره من سجلات قسمك.'
      : hasNarrowing
        ? 'جرّب تعديل البحث أو المرشحات، أو امسحها لعرض كل أسطر السجل.'
        : 'تُضاف الأسطر عند تسجيل وثيقة في هذا السجل أو استلامها.'

  return (
    <DataTable
      tableId="my-registers"
      mode="client"
      caption="سجلاتي"
      columns={columns}
      rows={lines}
      rowKey={(line) => line.documentNumberId}
      rowSearchText={(line) =>
        [
          documentRowLabel(line.documentTitle),
          line.registryNumberText,
          line.centralNumberText,
          line.replacedByNumberText,
          line.replacesNumberText,
          String(line.numberingYear),
        ]
          .filter(Boolean)
          .join(' ')
          .toLocaleLowerCase('ar')
      }
      rowLabel={(line) => documentRowLabel(line.documentTitle)}
      isLoading={isLoading}
      error={registriesQuery.error ?? bookQuery.error}
      onRetry={refetch}
      onRefresh={refetch}
      isFetching={registriesQuery.isFetching || bookQuery.isFetching}
      page={table.page}
      pageSize={table.pageSize}
      totalCount={lines.length}
      onPageChange={table.setPage}
      onPageSizeChange={table.setPageSize}
      search={table.search}
      onSearchChange={table.setSearch}
      searchPlaceholder="ابحث برقم السجل أو الوثيقة أو رقم الديوان…"
      sortKey={table.sortKey}
      sortDirection={table.sortDirection}
      onSortChange={table.setSort}
      rowMenu={buildRowMenu}
      headerMenu={headerMenu}
      emptyMenu={headerMenu}
      filters={
        <GuideRegion id="my-registers.filters">
          <div className="flex flex-wrap items-end gap-2">
            <Select
              aria-label="اختيار السجل"
              value={registryId || undefined}
              onValueChange={(value) => table.setFilter('registry', value)}
              options={registryOptions}
              disabled={isLoading || registries.length === 0}
              placeholder={isLoading ? 'جارٍ التحميل…' : 'اختر السجل…'}
              className="w-72"
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
            <Select
              aria-label="تصفية حسب حالة الرقم"
              value={statusId || 'all'}
              onValueChange={(value) => table.setFilter('status', value === 'all' ? null : value)}
              options={[...STATUS_OPTIONS]}
              className="w-36"
            />
            <Input
              aria-label="من تاريخ"
              type="date"
              dir="ltr"
              value={from}
              max={to || undefined}
              onChange={(event) => table.setFilter('from', event.target.value)}
              className="w-40"
            />
            <Input
              aria-label="إلى تاريخ"
              type="date"
              dir="ltr"
              value={to}
              min={from || undefined}
              onChange={(event) => table.setFilter('to', event.target.value)}
              className="w-40"
            />
            {(year !== null || statusId || from || to) && (
              <Button
                type="button"
                variant="ghost"
                size="sm"
                onClick={() => {
                  table.setFilter('year', null)
                  table.setFilter('status', null)
                  table.setFilter('from', null)
                  table.setFilter('to', null)
                }}
              >
                مسح المرشحات
              </Button>
            )}
          </div>
        </GuideRegion>
      }
      emptyTitle={emptyTitle}
      emptyDescription={emptyDescription}
      emptyAction={
        registryId && table.search ? (
          <Button type="button" variant="outline" onClick={() => table.setSearch('')}>
            مسح البحث
          </Button>
        ) : undefined
      }
    />
  )
}
