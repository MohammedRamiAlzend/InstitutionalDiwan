import { useMemo, useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { Copy, Eye, Hash, Plus, RefreshCw } from 'lucide-react'
import { toast } from 'sonner'
import { DepartmentBadge, useDepartmentOptions, DepartmentLabel } from '@/entities/department'
import {
  DocumentStatusBadge,
  OfficialNumberTag,
  useDocuments,
  type DocumentResponse,
} from '@/entities/document'
import {
  FIELD_DATA_TYPE,
  FieldFilterBar,
  formatFieldValue,
  useDocumentTypeSchema,
  type SchemaFieldResponse,
} from '@/entities/document-schema'
import { LOOKUP_SLUGS, LookupLabel, useLookupEntries } from '@/entities/lookup'
import { useCanCreateDocuments, useSessionStore } from '@/entities/user'
import { CreateDocumentDialog } from '@/features/document-create'
import { RegisterNumberFilter } from './RegisterNumberFilter'
import { toArabicErrorMessage } from '@/shared/api'
import { FEATURES, Permissions, ROUTES } from '@/shared/config'
import { formatDate, formatDateTime, t, useLabel, useTableState } from '@/shared/lib'
import {
  Button,
  DataTable,
  GuideChip,
  Label,
  Select,
  type DataTableColumn,
  type MenuEntry,
} from '@/shared/ui'

/** URL parameter holding the repeated `key:operator:value` dynamic-field filters. */
const FIELD_FILTER_PARAM = 'f'

/** Sorting on the server works on the stored column, which only these types order meaningfully. */
const SORTABLE_FIELD_TYPES: ReadonlySet<number> = new Set([
  FIELD_DATA_TYPE.Text,
  FIELD_DATA_TYPE.Number,
  FIELD_DATA_TYPE.Date,
  FIELD_DATA_TYPE.Boolean,
])

function fieldColumn(field: SchemaFieldResponse): DataTableColumn<DocumentResponse> {
  const latin = field.dataType === FIELD_DATA_TYPE.Number || field.dataType === FIELD_DATA_TYPE.Date
  return {
    id: `field.${field.key}`,
    header: field.labelAr,
    sortable: SORTABLE_FIELD_TYPES.has(field.dataType),
    sortKey: `field:${field.key}`,
    cell: (document) => (
      <span dir={latin ? 'ltr' : undefined} className="whitespace-nowrap text-xs text-foreground">
        {formatFieldValue(field, document.fieldValues?.[field.key])}
      </span>
    ),
  }
}

function optionalId(value: string | undefined): number | undefined {
  if (!value) return undefined
  const parsed = Number(value)
  return Number.isInteger(parsed) && parsed > 0 ? parsed : undefined
}

/** Module 03 correspondence registry: server search/paging, filters, row menus. */
export function DocumentRegistryTable() {
  const table = useTableState({
    defaultSortKey: 'createdAt',
    defaultSortDirection: 'desc',
    filterKeys: ['statusId', 'documentTypeId', 'owningDepartmentId', 'registerNumber'],
    // Field filters belong to one document type, so changing the type drops them.
    repeatedKeys: [FIELD_FILTER_PARAM],
    resetOnChange: { documentTypeId: [FIELD_FILTER_PARAM] },
  })
  const documentTypeId = optionalId(table.filters.documentTypeId)

  const documentsQuery = useDocuments({
    page: table.page,
    pageSize: table.pageSize,
    search: table.search || undefined,
    sortBy: table.sortKey ?? 'createdAt',
    sortDescending: table.sortDirection === 'desc',
    statusId: optionalId(table.filters.statusId),
    documentTypeId,
    fieldFilters: documentTypeId ? table.repeated[FIELD_FILTER_PARAM] : undefined,
    owningDepartmentId: table.filters.owningDepartmentId || undefined,
    registerNumber: table.filters.registerNumber || undefined,
  })

  const hasPermission = useSessionStore((state) => state.hasPermission)
  // The settings can reserve creation to the Central Diwan, so the button is hidden for the others.
  const departmentMayCreate = useCanCreateDocuments()
  const canCreate = hasPermission(Permissions.DocumentCreate) && departmentMayCreate
  const navigate = useNavigate()

  const departmentsQuery = useDepartmentOptions()
  const statusQuery = useLookupEntries(LOOKUP_SLUGS.DocumentStatusTypes)
  const typesQuery = useLookupEntries(LOOKUP_SLUGS.DocumentTypes)

  const departmentById = useMemo(
    () =>
      new Map(
        (departmentsQuery.data ?? []).map((department) => [
          department.departmentId,
          department,
        ]),
      ),
    [departmentsQuery.data],
  )

  const allStatusesLabel = useLabel('documents.filter.all-statuses')
  const allTypesLabel = useLabel('documents.filter.all-types')
  const allDepartmentsLabel = useLabel('documents.filter.all-departments')

  const statusOptions = useMemo(
    () => [
      { value: 'all', label: allStatusesLabel },
      ...(statusQuery.data ?? []).map((entry) => ({
        value: String(entry.id),
        label: entry.descAr,
      })),
    ],
    [statusQuery.data, allStatusesLabel],
  )

  const typeOptions = useMemo(
    () => [
      { value: 'all', label: allTypesLabel },
      ...(typesQuery.data ?? []).map((entry) => ({
        value: String(entry.id),
        label: entry.descAr,
      })),
    ],
    [typesQuery.data, allTypesLabel],
  )

  const departmentOptions = useMemo(
    () => [
      { value: 'all', label: allDepartmentsLabel },
      ...(departmentsQuery.data ?? []).map((department) => ({
        value: department.departmentId,
        label: department.nameAr,
      })),
    ],
    [departmentsQuery.data, allDepartmentsLabel],
  )

  const [createOpen, setCreateOpen] = useState(false)

  const copyValue = async (value: string, label: string) => {
    try {
      await navigator.clipboard.writeText(value)
      toast.success(t('documents.toast.copied', { label }))
    } catch {
      toast.error(t('documents.toast.copy-failed'))
    }
  }

  // Columns follow the chosen type's published schema, like the field filters.
  const schemaQuery = useDocumentTypeSchema(documentTypeId)
  const fieldColumns = useMemo(
    () => (schemaQuery.data?.schema?.fields ?? []).filter((field) => field.behavior.showInTable).map(fieldColumn),
    [schemaQuery.data],
  )

  const columns = useMemo<DataTableColumn<DocumentResponse>[]>(
    () => [
      {
        id: 'title',
        headerKey: 'documents.column.title',
        sortable: true,
        sortKey: 'title',
        hideable: false,
        cell: (document) => (
          <div className="flex min-w-52 flex-col gap-0.5">
            <Link
              to={ROUTES.DOCUMENT_DETAIL(document.documentId)}
              className="text-sm font-medium text-foreground hover:text-accent hover:underline"
            >
              {document.title}
            </Link>
          </div>
        ),
      },
      {
        id: 'officialNumber',
        headerKey: 'documents.column.official-number',
        sortable: true,
        sortKey: 'officialNumber',
        cell: (document) => (
          <OfficialNumberTag officialNumberText={document.officialNumberText} />
        ),
      },
      {
        id: 'documentType',
        headerKey: 'documents.column.type',
        cell: (document) => (
          <LookupLabel lookup={LOOKUP_SLUGS.DocumentTypes} id={document.documentTypeId} />
        ),
      },
      {
        id: 'status',
        headerKey: 'documents.column.status',
        cell: (document) => <DocumentStatusBadge statusId={document.statusId} />,
      },
      {
        id: 'department',
        headerKey: 'documents.column.department',
        cell: (document) => {
          const department = departmentById.get(document.owningDepartmentId)
          return department ? (
            <DepartmentBadge
              nameAr={department.nameAr}
              code={department.code}
              isActive={department.isActive}
            />
          ) : (
            <DepartmentLabel departmentId={document.owningDepartmentId} />
          )
        },
      },
      ...fieldColumns,
      {
        id: 'registrationDate',
        headerKey: 'documents.column.registration-date',
        sortable: true,
        sortKey: 'registrationDate',
        cell: (document) => (
          <span dir="ltr" className="whitespace-nowrap text-xs text-muted">
            {formatDate(document.registrationDate)}
          </span>
        ),
      },
      {
        id: 'createdAt',
        headerKey: 'documents.column.created-at',
        sortable: true,
        sortKey: 'createdAt',
        defaultHidden: true,
        cell: (document) => (
          <span dir="ltr" className="whitespace-nowrap text-xs text-muted">
            {formatDateTime(document.createdAt)}
          </span>
        ),
      },
    ],
    [departmentById, fieldColumns],
  )

  const buildRowMenu = (document: DocumentResponse): MenuEntry[] => [
    {
      id: 'open',
      label: t('documents.row.open'),
      icon: <Eye className="size-4" aria-hidden />,
      onSelect: () => navigate(ROUTES.DOCUMENT_DETAIL(document.documentId)),
    },
    {
      id: 'copy-number',
      label: t('documents.row.copy-number'),
      icon: <Hash className="size-4" aria-hidden />,
      disabled: !document.officialNumberText,
      onSelect: () =>
        void copyValue(document.officialNumberText ?? '', t('documents.column.official-number')),
    },
    {
      id: 'copy-id',
      label: t('documents.row.copy-id'),
      icon: <Copy className="size-4" aria-hidden />,
      onSelect: () => void copyValue(document.documentId, t('documents.copy.id-label')),
    },
  ]

  const refreshEntry: MenuEntry = {
    id: 'refresh',
    label: t('table.refresh'),
    icon: <RefreshCw className="size-4" aria-hidden />,
    onSelect: () => void documentsQuery.refetch(),
  }

  const createEntry: MenuEntry = {
    id: 'create',
    label: t('documents.action.new'),
    icon: <Plus className="size-4" aria-hidden />,
    onSelect: () => setCreateOpen(true),
  }

  const headerMenu: MenuEntry[] = canCreate ? [createEntry, refreshEntry] : [refreshEntry]

  const hasQuery = Boolean(table.search) || table.hasActiveFilters

  return (
    <>
      <DataTable
        tableId="documents"
        caption={t('documents.registry-title')}
        columns={columns}
        rows={documentsQuery.data?.items}
        rowKey={(document) => document.documentId}
        isLoading={documentsQuery.isLoading}
        error={documentsQuery.error}
        onRetry={() => void documentsQuery.refetch()}
        onRefresh={() => void documentsQuery.refetch()}
        isFetching={documentsQuery.isFetching}
        page={table.page}
        pageSize={table.pageSize}
        totalCount={documentsQuery.data?.totalCount ?? 0}
        onPageChange={table.setPage}
        onPageSizeChange={table.setPageSize}
        search={table.search}
        onSearchChange={table.setSearch}
        sortKey={table.sortKey}
        sortDirection={table.sortDirection}
        onSortChange={table.setSort}
        searchPlaceholder={t('documents.search-placeholder')}
        filters={
          <div className="flex flex-wrap items-center gap-2">
            <GuideChip id="documents.filters" />
            <Select
              aria-label={t('documents.filter.status-aria')}
              value={table.filters.statusId || 'all'}
              onValueChange={(value) =>
                table.setFilter('statusId', value === 'all' ? null : value)
              }
              options={statusOptions}
              className="w-40"
            />
            <Select
              aria-label={t('documents.filter.type-aria')}
              value={table.filters.documentTypeId || 'all'}
              onValueChange={(value) =>
                table.setFilter('documentTypeId', value === 'all' ? null : value)
              }
              options={typeOptions}
              className="w-40"
            />
            <RegisterNumberFilter
              value={table.filters.registerNumber ?? ''}
              onChange={(value) => table.setFilter('registerNumber', value)}
            />
            <GuideChip id="documents.field-filters" />
            <FieldFilterBar
              documentTypeId={documentTypeId}
              raw={table.repeated[FIELD_FILTER_PARAM] ?? []}
              onChange={(raw) => table.setRepeated(FIELD_FILTER_PARAM, raw)}
            />
            <Select
              aria-label={t('documents.filter.department-aria')}
              value={table.filters.owningDepartmentId || 'all'}
              onValueChange={(value) =>
                table.setFilter('owningDepartmentId', value === 'all' ? null : value)
              }
              options={departmentOptions}
              disabled={departmentsQuery.isLoading}
              className="w-52"
            />
            {departmentsQuery.isError && (
              <span className="flex items-center gap-2 text-xs text-danger">
                {toArabicErrorMessage(departmentsQuery.error)}
                <Button
                  type="button"
                  variant="outline"
                  size="sm"
                  onClick={() => void departmentsQuery.refetch()}
                >
                  <Label k="documents.action.reload" />
                </Button>
              </span>
            )}
            {table.hasActiveFilters && (
              <Button type="button" variant="ghost" size="sm" onClick={table.clearFilters}>
                <Label k="documents.filter.clear" />
              </Button>
            )}
          </div>
        }
        rowMenu={buildRowMenu}
        headerMenu={headerMenu}
        emptyMenu={headerMenu}
        rowLabel={(document) => document.title}
        toolbar={
          canCreate ? (
            <>
              <Button type="button" onClick={() => setCreateOpen(true)}>
                <Plus className="size-4" aria-hidden />
                <Label k="documents.action.new" />
              </Button>
              <GuideChip id="documents.create" />
            </>
          ) : undefined
        }
        emptyTitle={hasQuery ? t('documents.empty.no-match-title') : t('documents.empty.none-title')}
        emptyDescription={
          hasQuery
            ? t('documents.empty.no-match-description')
            : canCreate
              ? FEATURES.drafts
                ? t('documents.empty.start-drafts')
                : t('documents.empty.start')
              : t('documents.empty.readonly')
        }
        emptyAction={
          hasQuery ? (
            <Button type="button" variant="outline" onClick={table.clearQuery}>
              <Label k="documents.empty.clear-query" />
            </Button>
          ) : canCreate ? (
            <Button type="button" onClick={() => setCreateOpen(true)}>
              <Plus className="size-4" aria-hidden />
              <Label k="documents.action.create" />
            </Button>
          ) : undefined
        }
      />

      {createOpen && (
        <CreateDocumentDialog open onOpenChange={setCreateOpen} />
      )}
    </>
  )
}
