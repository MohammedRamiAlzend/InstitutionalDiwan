import { useMemo } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { ArrowUpRight, Copy, FileText, Hash, RefreshCw } from 'lucide-react'
import { toast } from 'sonner'
import { useDepartmentOptions } from '@/entities/department'
import {
  documentRowLabel,
  isDocumentRestricted,
  NumberStatusBadge,
  RestrictedDocumentLabel,
} from '@/entities/document'
import {
  useDepartmentRegister,
  type DepartmentRegisterLineResponse,
} from '@/entities/numbering-registry'
import { ROUTES } from '@/shared/config'
import { formatDate, formatNumber, t, useLabel, useTableState } from '@/shared/lib'
import {
  Badge,
  Button,
  DataTable,
  Input,
  Label,
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
  const suspendedLabel = useLabel('department.status.suspended')

  const departmentOptions = useMemo(
    () =>
      (departmentsQuery.data ?? []).map((department) => ({
        value: department.departmentId,
        label: `${department.code} — ${department.nameAr}${department.isActive ? '' : ` (${suspendedLabel})`}`,
      })),
    [departmentsQuery.data, suspendedLabel],
  )

  const copyValue = async (value: string, label: string) => {
    try {
      await navigator.clipboard.writeText(value)
      toast.success(t('department.toast.copied', { label }))
    } catch {
      toast.error(t('department.toast.copy-failed'))
    }
  }

  const openDocument = (line: DepartmentRegisterLineResponse) =>
    navigate(`${ROUTES.DOCUMENT_DETAIL(line.documentId)}?tab=numbers`)

  const columns = useMemo<DataTableColumn<DepartmentRegisterLineResponse>[]>(
    () => [
      {
        id: 'document',
        headerKey: 'department.register.column.document',
        hideable: false,
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
        headerKey: 'department.register.column.central-number',
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
        headerKey: 'department.register.column.registry-number',
        sortable: true,
        sortValue: (line) => line.registryNumberText,
        cell: (line) => (
          <code dir="ltr" className="whitespace-nowrap text-xs text-foreground">
            {line.registryNumberText}
          </code>
        ),
      },
      {
        id: 'linkedNumber',
        headerKey: 'department.register.column.linked-number',
        sortable: true,
        sortValue: (line) => line.replacedByNumberText ?? line.replacesNumberText,
        cell: (line) => {
          const linked = line.replacedByNumberText ?? line.replacesNumberText
          if (!linked) return <span className="text-xs text-muted">—</span>
          return (
            <span className="inline-flex items-center gap-1.5 whitespace-nowrap text-xs text-muted">
              {line.replacedByNumberText ? t('department.register.replaced-by') : t('department.register.replaces')}
              <code dir="ltr" className="text-foreground">
                {linked}
              </code>
            </span>
          )
        },
      },
      {
        id: 'numberingYear',
        headerKey: 'department.register.column.year',
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
        headerKey: 'department.register.column.registry-date',
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
        headerKey: 'department.register.column.status',
        cell: (line) => <NumberStatusBadge statusId={line.statusId} />,
      },
      {
        id: 'registrationCount',
        headerKey: 'department.register.column.registration-count',
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
    ...(isDocumentRestricted(line.documentTitle)
      ? []
      : ([
          {
            id: 'open-document',
            label: t('department.register.menu.open'),
            icon: <ArrowUpRight className="size-4" aria-hidden />,
            onSelect: () => openDocument(line),
          },
          { type: 'separator' },
        ] as MenuEntry[])),
    {
      id: 'copy-central-number',
      label: t('department.register.menu.copy-central'),
      icon: <Copy className="size-4" aria-hidden />,
      disabled: !line.centralNumberText,
      onSelect: () =>
        line.centralNumberText &&
        void copyValue(line.centralNumberText, t('department.register.copy.central')),
    },
    {
      id: 'copy-registry-number',
      label: t('department.register.menu.copy-registry'),
      icon: <Copy className="size-4" aria-hidden />,
      onSelect: () => void copyValue(line.registryNumberText, t('department.register.copy.registry')),
    },
    {
      id: 'copy-document-id',
      label: t('department.register.menu.copy-document-id'),
      icon: <Hash className="size-4" aria-hidden />,
      onSelect: () => void copyValue(line.documentId, t('department.register.copy.document-id')),
    },
  ]

  const headerMenu: MenuEntry[] = [
    {
      id: 'refresh',
      label: t('table.refresh'),
      icon: <RefreshCw className="size-4" aria-hidden />,
      onSelect: () => void registerQuery.refetch(),
    },
  ]

  const yearScopeLabel = yearText ? t('department.register.empty.year-scope', { year: yearText }) : ''

  return (
    <DataTable
      tableId="department-register"
      mode="client"
      caption={t('department.register.caption')}
      columns={columns}
      rows={lines}
      rowKey={(line) => line.documentNumberId}
      rowSearchText={(line) =>
        [
          documentRowLabel(line.documentTitle),
          line.centralNumberText,
          line.registryNumberText,
          line.replacedByNumberText,
          line.replacesNumberText,
          String(line.numberingYear),
        ]
          .filter(Boolean)
          .join(' ')
          .toLocaleLowerCase('ar')
      }
      rowLabel={(line) => documentRowLabel(line.documentTitle)}
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
      searchPlaceholder={t('department.register.search-placeholder')}
      sortKey={table.sortKey}
      sortDirection={table.sortDirection}
      onSortChange={table.setSort}
      rowMenu={buildRowMenu}
      headerMenu={headerMenu}
      emptyMenu={headerMenu}
      filters={
        <div className="flex flex-wrap items-end gap-2">
          <Select
            aria-label={t('department.register.filter.department-aria')}
            value={departmentId || undefined}
            onValueChange={(value) => table.setFilter('departmentId', value)}
            options={departmentOptions}
            disabled={departmentsQuery.isLoading}
            placeholder={
              departmentsQuery.isLoading ? t('department.register.filter.loading') : t('department.register.filter.department-placeholder')
            }
            className="w-64"
          />
          <Input
            aria-label={t('department.register.filter.year-aria')}
            type="number"
            inputMode="numeric"
            dir="ltr"
            min={1900}
            max={2200}
            value={yearText}
            onChange={(event) => table.setFilter('year', event.target.value)}
            placeholder={t('department.register.filter.year-placeholder')}
            className="w-32"
          />
          {table.hasActiveFilters && (
            <Button type="button" variant="ghost" size="sm" onClick={table.clearFilters}>
              <Label k="department.register.filter.clear" />
            </Button>
          )}
        </div>
      }
      emptyTitle={
        !departmentId
          ? t('department.register.empty.choose-title')
          : table.search
            ? t('department.empty.search-title')
            : t('department.register.empty.title')
      }
      emptyDescription={
        !departmentId
          ? t('department.register.empty.choose-description')
          : table.search
            ? t('department.register.empty.search-description')
            : t('department.register.empty.description', { yearScope: yearScopeLabel })
      }
      emptyAction={
        !departmentId ? undefined : table.search ? (
          <Button type="button" variant="outline" onClick={() => table.setSearch('')}>
            <Label k="department.empty.clear-search" />
          </Button>
        ) : undefined
      }
    />
  )
}
