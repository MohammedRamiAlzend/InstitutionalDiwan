import { useEffect, useMemo, useRef, useState } from 'react'
import { Link, useNavigate, useSearchParams } from 'react-router-dom'
import { BookmarkPlus, Copy, Eye, Hash, History, RefreshCw } from 'lucide-react'
import { toast } from 'sonner'
import {
  archiveCriteriaFromParams,
  archiveCriteriaToParams,
  parseArchiveCriteria,
  serializeArchiveCriteria,
  useArchivedDocuments,
  useSavedSearches,
  type ArchivedDocumentResponse,
  type ArchiveSortKey,
  type SavedSearchResponse,
} from '@/entities/archive'
import { DepartmentBadge, useDepartmentOptions, DepartmentLabel } from '@/entities/department'
import {
  OfficialNumberTag,
  documentRowLabel,
  isDocumentRestricted,
} from '@/entities/document'
import { FieldFilterBar } from '@/entities/document-schema'
import {
  LOOKUP_SLUGS,
  LookupLabel,
  useLookupEntries,
} from '@/entities/lookup'
import { SaveSearchDialog } from '@/features/archive-manage'
import { toArabicErrorMessage } from '@/shared/api'
import { ROUTES } from '@/shared/config'
import { formatDate, formatDateTime, t, useLabel, useTableState } from '@/shared/lib'
import {
  Button,
  DataTable,
  GuideChip,
  Input,
  Label,
  Select,
  type DataTableColumn,
  type MenuEntry,
} from '@/shared/ui'
import { SavedSearchesBar } from './SavedSearchesBar'
import { UserLabel } from '@/entities/user'

/** URL parameter holding the repeated `key:operator:value` dynamic-field filters. */
const FIELD_FILTER_PARAM = 'f'

const SORT_KEYS: readonly ArchiveSortKey[] = ['archivedAt', 'title', 'officialNumber']

function optionalId(value: string | undefined): number | undefined {
  if (!value) return undefined
  const parsed = Number(value)
  return Number.isInteger(parsed) && parsed > 0 ? parsed : undefined
}

/** `yyyy-mm-dd` from the date input → the UTC instant that starts (or ends) that local day. */
function dayBoundaryUtc(value: string | undefined, end: boolean): string | undefined {
  if (!value || !/^\d{4}-\d{2}-\d{2}$/.test(value)) return undefined
  const date = new Date(`${value}T${end ? '23:59:59.999' : '00:00:00'}`)
  return Number.isNaN(date.getTime()) ? undefined : date.toISOString()
}

function resolveSortKey(raw: string | null): ArchiveSortKey {
  return SORT_KEYS.find((key) => key === raw) ?? 'archivedAt'
}

/** Module 07 archive grid: access-filtered server search, filters, saved searches, row menus. */
export function ArchiveSearchTable() {
  const table = useTableState({
    defaultSortKey: 'archivedAt',
    defaultSortDirection: 'desc',
    filterKeys: [
      'owningDepartmentId',
      'documentTypeId',
      'archiveLocationId',
      'archivedFrom',
      'archivedTo',
    ],
    // Field filters belong to one document type, so changing the type drops them.
    repeatedKeys: [FIELD_FILTER_PARAM],
    resetOnChange: { documentTypeId: [FIELD_FILTER_PARAM] },
  })
  const documentTypeId = optionalId(table.filters.documentTypeId)
  const [searchParams, setSearchParams] = useSearchParams()
  const navigate = useNavigate()

  const archiveQuery = useArchivedDocuments({
    page: table.page,
    pageSize: table.pageSize,
    search: table.search || undefined,
    sortBy: resolveSortKey(table.sortKey),
    sortDescending: table.sortDirection === 'desc',
    owningDepartmentId: table.filters.owningDepartmentId || undefined,
    documentTypeId,
    fieldFilters: documentTypeId ? table.repeated[FIELD_FILTER_PARAM] : undefined,
    archiveLocationId: optionalId(table.filters.archiveLocationId),
    archivedFromUtc: dayBoundaryUtc(table.filters.archivedFrom, false),
    archivedToUtc: dayBoundaryUtc(table.filters.archivedTo, true),
  })

  const departmentsQuery = useDepartmentOptions()
  const typesQuery = useLookupEntries(LOOKUP_SLUGS.DocumentTypes)
  const locationsQuery = useLookupEntries(LOOKUP_SLUGS.ArchiveLocations)
  const savedQuery = useSavedSearches()

  const departmentById = useMemo(
    () => new Map((departmentsQuery.data ?? []).map((department) => [department.departmentId, department])),
    [departmentsQuery.data],
  )

  const allOption = (label: string) => ({ value: 'all', label })
  const allTypesLabel = useLabel('archive.filter.all-types')
  const allLocationsLabel = useLabel('archive.filter.all-locations')
  const allDepartmentsLabel = useLabel('archive.filter.all-departments')
  const typeOptions = useMemo(
    () => [
      allOption(allTypesLabel),
      ...(typesQuery.data ?? []).map((entry) => ({ value: String(entry.id), label: entry.descAr })),
    ],
    [typesQuery.data, allTypesLabel],
  )
  const locationOptions = useMemo(
    () => [
      allOption(allLocationsLabel),
      ...(locationsQuery.data ?? []).map((entry) => ({ value: String(entry.id), label: entry.descAr })),
    ],
    [locationsQuery.data, allLocationsLabel],
  )
  const departmentOptions = useMemo(
    () => [
      allOption(allDepartmentsLabel),
      ...(departmentsQuery.data ?? []).map((department) => ({
        value: department.departmentId,
        label: department.nameAr,
      })),
    ],
    [departmentsQuery.data, allDepartmentsLabel],
  )

  // The grid's criteria are exactly its URL params, so saving and restoring is a copy in/out.
  const currentCriteriaJson = serializeArchiveCriteria(archiveCriteriaFromParams(searchParams))
  const hasCriteria = currentCriteriaJson !== '{}'

  const applySavedSearch = (search: SavedSearchResponse) => {
    setSearchParams(archiveCriteriaToParams(parseArchiveCriteria(search.criteriaJson)), { replace: true })
  }

  // A default saved search opens the page for a caller who arrived without any criteria — once.
  const defaultApplied = useRef(false)
  useEffect(() => {
    if (defaultApplied.current || !savedQuery.data) return
    defaultApplied.current = true

    const defaultSearch = savedQuery.data.find((search) => search.isDefault)
    if (defaultSearch && ![...searchParams.keys()].length) {
      setSearchParams(archiveCriteriaToParams(parseArchiveCriteria(defaultSearch.criteriaJson)), {
        replace: true,
      })
    }
  }, [savedQuery.data, searchParams, setSearchParams])

  const activeSearchId =
    savedQuery.data?.find((search) => search.criteriaJson === currentCriteriaJson)?.savedSearchId ?? null

  const [saveOpen, setSaveOpen] = useState(false)

  const copyValue = async (value: string, label: string) => {
    try {
      await navigator.clipboard.writeText(value)
      toast.success(t('archive.toast.copied', { label }))
    } catch {
      toast.error(t('archive.toast.copy-failed'))
    }
  }

  const columns = useMemo<DataTableColumn<ArchivedDocumentResponse>[]>(
    () => [
      {
        id: 'title',
        headerKey: 'archive.column.title',
        sortable: true,
        sortKey: 'title',
        hideable: false,
        cell: ({ document }) => (
          <div className="flex min-w-52 flex-col gap-0.5">
            {isDocumentRestricted(document.title) ? (
              <span className="text-sm text-muted">{documentRowLabel(document.title)}</span>
            ) : (
              <Link
                to={ROUTES.DOCUMENT_DETAIL(document.documentId)}
                className="text-sm font-medium text-foreground hover:text-accent hover:underline"
              >
                {document.title}
              </Link>
            )}
          </div>
        ),
      },
      {
        id: 'officialNumber',
        headerKey: 'archive.column.official-number',
        sortable: true,
        sortKey: 'officialNumber',
        cell: ({ document }) => <OfficialNumberTag officialNumberText={document.officialNumberText} />,
      },
      {
        id: 'location',
        headerKey: 'archive.column.location',
        cell: ({ archiveLocationId }) =>
          archiveLocationId === null ? (
            <span className="text-xs text-muted">—</span>
          ) : (
            <LookupLabel lookup={LOOKUP_SLUGS.ArchiveLocations} id={archiveLocationId} />
          ),
      },
      {
        id: 'archivedAt',
        headerKey: 'archive.column.archived-at',
        sortable: true,
        sortKey: 'archivedAt',
        cell: ({ archivedAt }) => (
          <span dir="ltr" className="whitespace-nowrap text-xs text-foreground">
            {formatDateTime(archivedAt)}
          </span>
        ),
      },
      {
        id: 'documentType',
        headerKey: 'archive.column.type',
        cell: ({ document }) => (
          <LookupLabel lookup={LOOKUP_SLUGS.DocumentTypes} id={document.documentTypeId} />
        ),
      },
      {
        id: 'department',
        headerKey: 'archive.column.department',
        cell: ({ document }) => {
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
      {
        id: 'registrationDate',
        headerKey: 'archive.column.registration-date',
        defaultHidden: true,
        cell: ({ document }) => (
          <span dir="ltr" className="whitespace-nowrap text-xs text-muted">
            {formatDate(document.registrationDate)}
          </span>
        ),
      },
      {
        id: 'archivedBy',
        headerKey: 'archive.column.archived-by',
        defaultHidden: true,
        cell: ({ archivedByUserId }) => <UserLabel userId={archivedByUserId} />,
      },
    ],
    [departmentById],
  )

  const buildRowMenu = ({ document }: ArchivedDocumentResponse): MenuEntry[] => [
    {
      id: 'open',
      label: t('archive.row.open'),
      icon: <Eye className="size-4" aria-hidden />,
      disabled: isDocumentRestricted(document.title),
      onSelect: () => navigate(ROUTES.DOCUMENT_DETAIL(document.documentId)),
    },
    {
      id: 'history',
      label: t('archive.row.history'),
      icon: <History className="size-4" aria-hidden />,
      disabled: isDocumentRestricted(document.title),
      onSelect: () => navigate(`${ROUTES.DOCUMENT_DETAIL(document.documentId)}?tab=archive`),
    },
    {
      id: 'copy-number',
      label: t('archive.row.copy-number'),
      icon: <Hash className="size-4" aria-hidden />,
      disabled: !document.officialNumberText,
      onSelect: () => void copyValue(document.officialNumberText ?? '', t('archive.copy.official-number')),
    },
    {
      id: 'copy-id',
      label: t('archive.row.copy-id'),
      icon: <Copy className="size-4" aria-hidden />,
      onSelect: () => void copyValue(document.documentId, t('archive.copy.document-id')),
    },
  ]

  const refreshEntry: MenuEntry = {
    id: 'refresh',
    label: t('table.refresh'),
    icon: <RefreshCw className="size-4" aria-hidden />,
    onSelect: () => void archiveQuery.refetch(),
  }
  const saveEntry: MenuEntry = {
    id: 'save-search',
    label: t('archive.menu.save-search'),
    icon: <BookmarkPlus className="size-4" aria-hidden />,
    disabled: !hasCriteria,
    onSelect: () => setSaveOpen(true),
  }
  const headerMenu: MenuEntry[] = [saveEntry, refreshEntry]

  const hasQuery = Boolean(table.search) || table.hasActiveFilters

  return (
    <div className="flex flex-col gap-3">
      <SavedSearchesBar
        currentCriteriaJson={currentCriteriaJson}
        activeSearchId={activeSearchId}
        onApply={applySavedSearch}
      />

      <DataTable
        tableId="archive-documents"
        caption={t('archive.caption')}
        columns={columns}
        rows={archiveQuery.data?.items}
        rowKey={({ document }) => document.documentId}
        isLoading={archiveQuery.isLoading}
        error={archiveQuery.error}
        onRetry={() => void archiveQuery.refetch()}
        onRefresh={() => void archiveQuery.refetch()}
        isFetching={archiveQuery.isFetching}
        page={table.page}
        pageSize={table.pageSize}
        totalCount={archiveQuery.data?.totalCount ?? 0}
        onPageChange={table.setPage}
        onPageSizeChange={table.setPageSize}
        search={table.search}
        onSearchChange={table.setSearch}
        sortKey={table.sortKey}
        sortDirection={table.sortDirection}
        onSortChange={table.setSort}
        searchPlaceholder={t('archive.search-placeholder')}
        filters={
          <div className="flex flex-wrap items-center gap-2">
            <GuideChip id="archive.filters" />
            <Select
              aria-label={t('archive.filter.location-aria')}
              value={table.filters.archiveLocationId || 'all'}
              onValueChange={(value) =>
                table.setFilter('archiveLocationId', value === 'all' ? null : value)
              }
              options={locationOptions}
              className="w-40"
            />
            <Select
              aria-label={t('archive.filter.type-aria')}
              value={table.filters.documentTypeId || 'all'}
              onValueChange={(value) => table.setFilter('documentTypeId', value === 'all' ? null : value)}
              options={typeOptions}
              className="w-40"
            />
            <GuideChip id="archive.field-filters" />
            <FieldFilterBar
              documentTypeId={documentTypeId}
              raw={table.repeated[FIELD_FILTER_PARAM] ?? []}
              onChange={(raw) => table.setRepeated(FIELD_FILTER_PARAM, raw)}
            />
            <Select
              aria-label={t('archive.filter.department-aria')}
              value={table.filters.owningDepartmentId || 'all'}
              onValueChange={(value) =>
                table.setFilter('owningDepartmentId', value === 'all' ? null : value)
              }
              options={departmentOptions}
              disabled={departmentsQuery.isLoading}
              className="w-52"
            />
            <label className="flex items-center gap-1.5 text-xs text-muted">
              <Label k="archive.filter.from-label" />
              <Input
                type="date"
                dir="ltr"
                aria-label={t('archive.filter.from-aria')}
                value={table.filters.archivedFrom ?? ''}
                max={table.filters.archivedTo || undefined}
                onChange={(event) => table.setFilter('archivedFrom', event.target.value || null)}
                className="h-10 w-40"
              />
            </label>
            <label className="flex items-center gap-1.5 text-xs text-muted">
              <Label k="archive.filter.to-label" />
              <Input
                type="date"
                dir="ltr"
                aria-label={t('archive.filter.to-aria')}
                value={table.filters.archivedTo ?? ''}
                min={table.filters.archivedFrom || undefined}
                onChange={(event) => table.setFilter('archivedTo', event.target.value || null)}
                className="h-10 w-40"
              />
            </label>
            {departmentsQuery.isError && (
              <span className="flex items-center gap-2 text-xs text-danger">
                {toArabicErrorMessage(departmentsQuery.error)}
                <Button
                  type="button"
                  variant="outline"
                  size="sm"
                  onClick={() => void departmentsQuery.refetch()}
                >
                  <Label k="archive.reload" />
                </Button>
              </span>
            )}
            {table.hasActiveFilters && (
              <Button type="button" variant="ghost" size="sm" onClick={table.clearFilters}>
                <Label k="archive.filter.clear" />
              </Button>
            )}
          </div>
        }
        rowMenu={buildRowMenu}
        headerMenu={headerMenu}
        emptyMenu={headerMenu}
        rowLabel={({ document }) => documentRowLabel(document.title)}
        toolbar={
          <>
            <Button type="button" variant="outline" disabled={!hasCriteria} onClick={() => setSaveOpen(true)}>
              <BookmarkPlus className="size-4" aria-hidden />
              <Label k="archive.toolbar.save-search" />
            </Button>
            <GuideChip id="archive.save-search" />
          </>
        }
        emptyTitle={hasQuery ? t('archive.empty.no-match-title') : t('archive.empty.no-documents-title')}
        emptyDescription={
          hasQuery
            ? t('archive.empty.no-match-description')
            : t('archive.empty.no-documents-description')
        }
        emptyAction={
          hasQuery ? (
            <Button type="button" variant="outline" onClick={table.clearQuery}>
              <Label k="archive.empty.clear" />
            </Button>
          ) : undefined
        }
      />

      {saveOpen && (
        <SaveSearchDialog criteriaJson={currentCriteriaJson} open onOpenChange={setSaveOpen} />
      )}
    </div>
  )
}
