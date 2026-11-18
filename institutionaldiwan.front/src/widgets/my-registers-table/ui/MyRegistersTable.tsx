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
  FIELD_DATA_TYPE,
  FieldFilterBar,
  formatFieldValue,
  SCHEMA_STATUS,
  useDepartmentRegisterForms,
  type SchemaFieldResponse,
} from '@/entities/document-schema'
import { LOOKUP_SLUGS, useLookupEntries } from '@/entities/lookup'
import {
  useDepartmentRegisterEntries,
  useNumberingRegistries,
  type DepartmentRegisterEntriesParams,
  type DepartmentRegisterEntryResponse,
} from '@/entities/numbering-registry'
import { useMyPrimaryDepartmentId } from '@/entities/user'
import { DOCUMENT_NUMBER_STATUS, ROUTES } from '@/shared/config'
import { formatDate, formatNumber, t, useLabel, useTableState } from '@/shared/lib'
import {
  Badge,
  Button,
  DataTable,
  GuideRegion,
  Input,
  Label,
  Select,
  type DataTableColumn,
  type MenuEntry,
} from '@/shared/ui'

const NO_FORM = 'none'

/** URL parameter holding the repeated `key:operator:value` filters on the chosen form's fields. */
const FIELD_FILTER_PARAM = 'f'

/** The server orders a form field by its stored column, which these types order meaningfully. */
const SORTABLE_FIELD_TYPES: ReadonlySet<number> = new Set([
  FIELD_DATA_TYPE.Text,
  FIELD_DATA_TYPE.LongText,
  FIELD_DATA_TYPE.Number,
  FIELD_DATA_TYPE.Date,
  FIELD_DATA_TYPE.Boolean,
  FIELD_DATA_TYPE.Select,
])

function fieldColumn(field: SchemaFieldResponse): DataTableColumn<DepartmentRegisterEntryResponse> {
  const latin = field.dataType === FIELD_DATA_TYPE.Number || field.dataType === FIELD_DATA_TYPE.Date
  return {
    id: `field.${field.key}`,
    header: field.labelAr,
    sortable: SORTABLE_FIELD_TYPES.has(field.dataType),
    sortKey: `field:${field.key}`,
    cell: (entry) => (
      <span dir={latin ? 'ltr' : undefined} className="text-xs text-foreground">
        {formatFieldValue(field, entry.values[field.key])}
      </span>
    ),
  }
}

/**
 * `/my-registers` — pick one register of the caller's own department and browse its lines. Search,
 * filters, sorting and paging all run on the server and live in the URL. When the department has a
 * record form for a document type, the lines carry that form's values as extra columns, for the
 * chosen version (the latest by default).
 */
export function MyRegistersTable() {
  const table = useTableState({
    defaultSortKey: 'registryDate',
    defaultSortDirection: 'desc',
    filterKeys: ['registry', 'type', 'version', 'year', 'status', 'from', 'to'],
    repeatedKeys: [FIELD_FILTER_PARAM],
    // A version belongs to one document type's form, and field filters to one version.
    resetOnChange: { type: ['version', FIELD_FILTER_PARAM], version: [FIELD_FILTER_PARAM] },
  })
  const navigate = useNavigate()

  const { departmentId, isLoading: isLoadingDepartment } = useMyPrimaryDepartmentId()
  const registriesQuery = useNumberingRegistries({ enabled: Boolean(departmentId) })
  const formsQuery = useDepartmentRegisterForms(departmentId)
  const typesQuery = useLookupEntries(LOOKUP_SLUGS.DocumentTypes)

  const suspendedLabel = useLabel('my-registers.registry.suspended')
  const noFormLabel = useLabel('my-registers.type.no-form')
  const statusAllLabel = useLabel('my-registers.status.all')
  const statusActiveLabel = useLabel('my-registers.status.active')
  const statusReplacedLabel = useLabel('my-registers.status.replaced')
  const statusCancelledLabel = useLabel('my-registers.status.cancelled')
  const statusOptions = useMemo(
    () => [
      { value: 'all', label: statusAllLabel },
      { value: String(DOCUMENT_NUMBER_STATUS.Active), label: statusActiveLabel },
      { value: String(DOCUMENT_NUMBER_STATUS.Replaced), label: statusReplacedLabel },
      { value: String(DOCUMENT_NUMBER_STATUS.Cancelled), label: statusCancelledLabel },
    ],
    [statusAllLabel, statusActiveLabel, statusReplacedLabel, statusCancelledLabel],
  )

  const registries = useMemo(
    () =>
      (registriesQuery.data ?? []).filter((registry) => registry.departmentId === departmentId),
    [registriesQuery.data, departmentId],
  )
  const registryOptions = useMemo(
    () =>
      registries.map((registry) => ({
        value: registry.registryId,
        label: `${registry.code} — ${registry.nameAr}${registry.isActive ? '' : ` ${suspendedLabel}`}`,
      })),
    [registries, suspendedLabel],
  )

  // A department with a single register needs no choosing.
  const registryId =
    table.filters.registry || (registries.length === 1 ? registries[0].registryId : '')
  const registry = registries.find((candidate) => candidate.registryId === registryId)

  // The form: the one the user chose, else the register's own document type if it has a form, else
  // the first form. "No form" lists every line without record data.
  const forms = useMemo(() => formsQuery.data?.forms ?? [], [formsQuery.data])
  const typeParam = table.filters.type ?? ''
  const defaultTypeId =
    registry?.documentTypeId && forms.some((entry) => entry.documentTypeId === registry.documentTypeId)
      ? registry.documentTypeId
      : forms[0]?.documentTypeId
  const typeId =
    typeParam === NO_FORM ? null : typeParam ? Number(typeParam) : (defaultTypeId ?? null)
  const form = forms.find((entry) => entry.documentTypeId === typeId) ?? null
  // The newest version comes first, so it is the default.
  const version =
    form?.versions.find((candidate) => candidate.schemaVersionId === table.filters.version) ??
    form?.versions[0] ??
    null

  const typeName = useMemo(() => {
    const names = new Map((typesQuery.data ?? []).map((entry) => [entry.id, entry.descAr]))
    return (id: number) => names.get(id) ?? t('my-registers.type.fallback', { id })
  }, [typesQuery.data])
  const typeOptions = useMemo(
    () => [
      { value: NO_FORM, label: noFormLabel },
      ...forms.map((entry) => ({
        value: String(entry.documentTypeId),
        label: typeName(entry.documentTypeId),
      })),
    ],
    [forms, typeName, noFormLabel],
  )
  const versionOptions = (form?.versions ?? []).map((candidate, index) => ({
    value: candidate.schemaVersionId,
    label: `${t('my-registers.version.label', { number: candidate.versionNumber })}${
      index === 0 ? ` ${t('my-registers.version.newest')}` : ''
    }${candidate.status === SCHEMA_STATUS.Retired ? ` — ${t('my-registers.version.retired')}` : ''}`,
  }))

  const year = /^\d{4}$/.test(table.filters.year ?? '') ? Number(table.filters.year) : undefined
  const statusId = Number.parseInt(table.filters.status ?? '', 10)
  const from = table.filters.from ?? ''
  const to = table.filters.to ?? ''

  const fieldFilters = table.repeated[FIELD_FILTER_PARAM] ?? []
  const formsReady = !formsQuery.isLoading
  const params: DepartmentRegisterEntriesParams | null =
    departmentId && registryId && formsReady
      ? {
          departmentId,
          registryId,
          numberingYear: year,
          statusId: Number.isFinite(statusId) ? statusId : undefined,
          dateFrom: from || undefined,
          dateTo: to || undefined,
          schemaVersionId: version?.schemaVersionId,
          fieldFilters: version ? fieldFilters : undefined,
          page: table.page,
          pageSize: table.pageSize,
          search: table.search || undefined,
          sortBy: table.sortKey ?? undefined,
          sortDescending: table.sortDirection === 'desc',
        }
      : null
  const entriesQuery = useDepartmentRegisterEntries(params)

  const copyValue = async (value: string, label: string) => {
    try {
      await navigator.clipboard.writeText(value)
      toast.success(t('my-registers.toast.copied', { label }))
    } catch {
      toast.error(t('my-registers.toast.copy-failed'))
    }
  }

  const openDocument = (entry: DepartmentRegisterEntryResponse) =>
    navigate(`${ROUTES.DOCUMENT_DETAIL(entry.documentId)}?tab=numbers`)

  const fieldColumns = (version?.fields ?? []).map(fieldColumn)

  const columns: DataTableColumn<DepartmentRegisterEntryResponse>[] = [
      {
        id: 'registryNumber',
        headerKey: 'my-registers.column.registry-number',
        hideable: false,
        sortable: true,
        sortKey: 'registryNumber',
        cell: (entry) => (
          <code dir="ltr" className="whitespace-nowrap text-xs text-foreground">
            {entry.registryNumberText}
          </code>
        ),
      },
      {
        id: 'registryDate',
        headerKey: 'my-registers.column.registry-date',
        sortable: true,
        sortKey: 'registryDate',
        cell: (entry) => (
          <span dir="ltr" className="whitespace-nowrap text-xs text-muted">
            {formatDate(entry.registryDate)}
          </span>
        ),
      },
      {
        id: 'document',
        headerKey: 'my-registers.column.document',
        sortable: true,
        sortKey: 'document',
        cell: (entry) =>
          isDocumentRestricted(entry.documentTitle) ? (
            <RestrictedDocumentLabel />
          ) : (
            <Link
              to={`${ROUTES.DOCUMENT_DETAIL(entry.documentId)}?tab=numbers`}
              className="inline-flex items-center gap-1.5 text-sm text-accent transition-colors hover:underline"
            >
              <FileText className="size-3.5 shrink-0" aria-hidden />
              <span className="line-clamp-1 max-w-72">{entry.documentTitle}</span>
            </Link>
          ),
      },
      ...fieldColumns,
      {
        id: 'centralNumber',
        headerKey: 'my-registers.column.central-number',
        sortable: true,
        sortKey: 'centralNumber',
        cell: (entry) => (
          <span dir="ltr" className="whitespace-nowrap text-xs text-muted">
            {entry.centralNumberText ?? '—'}
          </span>
        ),
      },
      {
        id: 'linkedNumber',
        headerKey: 'my-registers.column.linked-number',
        cell: (entry) => {
          const linked = entry.replacedByNumberText ?? entry.replacesNumberText
          if (!linked) return <span className="text-xs text-muted">—</span>
          return (
            <span className="inline-flex items-center gap-1.5 whitespace-nowrap text-xs text-muted">
              <Label
                k={
                  entry.replacedByNumberText
                    ? 'my-registers.linked.replaced-by'
                    : 'my-registers.linked.replaces'
                }
              />
              <code dir="ltr" className="text-foreground">
                {linked}
              </code>
            </span>
          )
        },
      },
      {
        id: 'numberingYear',
        headerKey: 'my-registers.column.year',
        sortable: true,
        sortKey: 'numberingYear',
        defaultHidden: true,
        cell: (entry) => (
          <span dir="ltr" className="whitespace-nowrap text-xs text-muted">
            {entry.numberingYear}
          </span>
        ),
      },
      {
        id: 'status',
        headerKey: 'my-registers.column.status',
        sortable: true,
        sortKey: 'status',
        cell: (entry) => <NumberStatusBadge statusId={entry.statusId} />,
      },
      {
        id: 'registrationCount',
        headerKey: 'my-registers.column.registration-count',
        sortable: true,
        sortKey: 'registrationCount',
        defaultHidden: true,
        cell: (entry) => <Badge variant="outline">{formatNumber(entry.registrationCount)}</Badge>,
      },
  ]

  const buildRowMenu = (entry: DepartmentRegisterEntryResponse): MenuEntry[] => [
    ...(isDocumentRestricted(entry.documentTitle)
      ? []
      : ([
          {
            id: 'open-document',
            label: t('my-registers.row.open'),
            icon: <ArrowUpRight className="size-4" aria-hidden />,
            onSelect: () => openDocument(entry),
          },
          { type: 'separator' },
        ] as MenuEntry[])),
    {
      id: 'copy-registry-number',
      label: t('my-registers.row.copy-registry'),
      icon: <Copy className="size-4" aria-hidden />,
      onSelect: () =>
        void copyValue(entry.registryNumberText, t('my-registers.column.registry-number')),
    },
    {
      id: 'copy-central-number',
      label: t('my-registers.row.copy-central'),
      icon: <Copy className="size-4" aria-hidden />,
      disabled: !entry.centralNumberText,
      onSelect: () =>
        entry.centralNumberText &&
        void copyValue(entry.centralNumberText, t('my-registers.column.central-number')),
    },
    {
      id: 'copy-document-id',
      label: t('my-registers.row.copy-id'),
      icon: <Hash className="size-4" aria-hidden />,
      onSelect: () => void copyValue(entry.documentId, t('my-registers.copy.document-id')),
    },
  ]

  const refetch = () => {
    void registriesQuery.refetch()
    void formsQuery.refetch()
    void entriesQuery.refetch()
  }

  const headerMenu: MenuEntry[] = [
    {
      id: 'refresh',
      label: t('table.refresh'),
      icon: <RefreshCw className="size-4" aria-hidden />,
      onSelect: refetch,
    },
  ]

  const isLoading =
    isLoadingDepartment ||
    registriesQuery.isLoading ||
    formsQuery.isLoading ||
    (params !== null && entriesQuery.isLoading)
  const noRegisters = !isLoading && Boolean(departmentId) && registries.length === 0
  const hasNarrowing = Boolean(
    year !== undefined ||
      Number.isFinite(statusId) ||
      from ||
      to ||
      table.search ||
      fieldFilters.length > 0,
  )

  const emptyTitle = noRegisters
    ? t('my-registers.empty.no-registers-title')
    : !registryId
      ? t('my-registers.empty.choose-title')
      : hasNarrowing
        ? t('my-registers.empty.no-match-title')
        : version
          ? t('my-registers.empty.no-version-lines-title')
          : t('my-registers.empty.no-lines-title')
  const emptyDescription = noRegisters
    ? t('my-registers.empty.no-registers-description')
    : !registryId
      ? t('my-registers.empty.choose-description')
      : hasNarrowing
        ? t('my-registers.empty.no-match-description')
        : version
          ? t('my-registers.empty.no-version-lines-description')
          : t('my-registers.empty.no-lines-description')

  return (
    <DataTable
      tableId="my-registers"
      caption={t('my-registers.caption')}
      columns={columns}
      rows={params ? entriesQuery.data?.items : []}
      rowKey={(entry) => entry.documentNumberId}
      rowLabel={(entry) => documentRowLabel(entry.documentTitle)}
      isLoading={isLoading}
      error={registriesQuery.error ?? formsQuery.error ?? (params ? entriesQuery.error : null)}
      onRetry={refetch}
      onRefresh={refetch}
      isFetching={entriesQuery.isFetching || registriesQuery.isFetching}
      page={table.page}
      pageSize={table.pageSize}
      totalCount={params ? (entriesQuery.data?.totalCount ?? 0) : 0}
      onPageChange={table.setPage}
      onPageSizeChange={table.setPageSize}
      search={table.search}
      onSearchChange={table.setSearch}
      searchPlaceholder={t('my-registers.search-placeholder')}
      sortKey={table.sortKey}
      sortDirection={table.sortDirection}
      onSortChange={table.setSort}
      rowMenu={buildRowMenu}
      headerMenu={headerMenu}
      emptyMenu={headerMenu}
      filters={
        <GuideRegion id="my-registers.filters">
          <div className="flex flex-col gap-2">
            <div className="flex flex-wrap items-end gap-2">
              <Select
                aria-label={t('my-registers.filter.registry-aria')}
                value={registryId || undefined}
                onValueChange={(value) => table.setFilter('registry', value)}
                options={registryOptions}
                disabled={isLoading || registries.length === 0}
                placeholder={
                  isLoading
                    ? t('my-registers.filter.registry-loading')
                    : t('my-registers.filter.registry-placeholder')
                }
                className="w-64"
              />
              <Select
                aria-label={t('my-registers.filter.form-aria')}
                value={form ? String(form.documentTypeId) : NO_FORM}
                onValueChange={(value) => table.setFilter('type', value)}
                options={typeOptions}
                disabled={!registryId || formsQuery.isLoading}
                className="w-48"
              />
              {form && (
                <Select
                  aria-label={t('my-registers.filter.version-aria')}
                  value={version?.schemaVersionId}
                  onValueChange={(value) => table.setFilter('version', value)}
                  options={versionOptions}
                  className="w-48"
                />
              )}
            </div>
            <div className="flex flex-wrap items-end gap-2">
              <Input
                aria-label={t('my-registers.filter.year-aria')}
                type="number"
                inputMode="numeric"
                dir="ltr"
                min={1900}
                max={2200}
                value={table.filters.year ?? ''}
                onChange={(event) => table.setFilter('year', event.target.value)}
                placeholder={t('my-registers.filter.year-placeholder')}
                className="w-32"
              />
              <Select
                aria-label={t('my-registers.filter.status-aria')}
                value={Number.isFinite(statusId) ? String(statusId) : 'all'}
                onValueChange={(value) => table.setFilter('status', value === 'all' ? null : value)}
                options={statusOptions}
                className="w-36"
              />
              <Input
                aria-label={t('my-registers.filter.from-aria')}
                type="date"
                dir="ltr"
                value={from}
                max={to || undefined}
                onChange={(event) => table.setFilter('from', event.target.value)}
                className="w-40"
              />
              <Input
                aria-label={t('my-registers.filter.to-aria')}
                type="date"
                dir="ltr"
                value={to}
                min={from || undefined}
                onChange={(event) => table.setFilter('to', event.target.value)}
                className="w-40"
              />
              {form && version && (
                <FieldFilterBar
                  documentTypeId={form.documentTypeId}
                  fields={version.fields}
                  raw={fieldFilters}
                  onChange={(raw) => table.setRepeated(FIELD_FILTER_PARAM, raw)}
                />
              )}
              {(year !== undefined ||
                Number.isFinite(statusId) ||
                from ||
                to ||
                fieldFilters.length > 0) && (
                <Button
                  type="button"
                  variant="ghost"
                  size="sm"
                  onClick={() =>
                    table.removeParams(['year', 'status', 'from', 'to', FIELD_FILTER_PARAM])
                  }
                >
                  <Label k="my-registers.filter.clear" />
                </Button>
              )}
            </div>
          </div>
        </GuideRegion>
      }
      emptyTitle={emptyTitle}
      emptyDescription={emptyDescription}
      emptyAction={
        registryId && table.search ? (
          <Button type="button" variant="outline" onClick={() => table.setSearch('')}>
            <Label k="my-registers.empty.clear-search" />
          </Button>
        ) : undefined
      }
    />
  )
}
