import { useMemo } from 'react'
import { ExternalLink, Hash, Plus, RefreshCw, Trash2 } from 'lucide-react'
import { useNavigate } from 'react-router-dom'
import { toast } from 'sonner'
import { DocumentStatusBadge, OfficialNumberTag } from '@/entities/document'
import {
  RELATION_DIRECTION_LABELS,
  RelationDirectionLabel,
  RelationTypeBadge,
  relationPhrase,
  type DocumentRelationResponse,
  type RelationDirection,
} from '@/entities/document-relation'
import { LOOKUP_SLUGS, findLookupFallback, useLookupEntries } from '@/entities/lookup'
import { ROUTES } from '@/shared/config'
import { formatDateTime, resolveLabel, t, useLabelsStore, type TableState } from '@/shared/lib'
import {
  Button,
  DataTable,
  GuideChip,
  Label,
  Select,
  type DataTableColumn,
  type MenuEntry,
  type SelectOption,
} from '@/shared/ui'

interface RelationsGridProps {
  relations: DocumentRelationResponse[] | undefined
  isLoading: boolean
  error: unknown
  isFetching: boolean
  onRefresh: () => void
  /** URL-backed grid state, owned by the panel because it also holds the view switch. */
  table: TableState
  canManage: boolean
  onAdd: () => void
  onRemove: (relation: DocumentRelationResponse) => void
}

/** The document's links as a full client-mode grid: type/direction filters, search, sort, paging, row menu. */
export function RelationsGrid({
  relations,
  isLoading,
  error,
  isFetching,
  onRefresh,
  table,
  canManage,
  onAdd,
  onRemove,
}: RelationsGridProps) {
  const navigate = useNavigate()
  const typesQuery = useLookupEntries(LOOKUP_SLUGS.DocumentRelationTypes)
  const overrides = useLabelsStore((state) => state.overrides)

  const typeFilter = table.filters.type
  const directionFilter = table.filters.direction

  const typeLabel = (relationTypeId: number) =>
    typesQuery.data?.find((entry) => entry.id === relationTypeId)?.descAr ??
    findLookupFallback(LOOKUP_SLUGS.DocumentRelationTypes, relationTypeId)?.descAr ??
    `#${relationTypeId}`

  const rows = useMemo(
    () =>
      (relations ?? []).filter(
        (relation) =>
          (!typeFilter || String(relation.type.relationTypeId) === typeFilter) &&
          (!directionFilter || relation.direction === directionFilter),
      ),
    [relations, typeFilter, directionFilter],
  )

  const typeOptions = useMemo<SelectOption[]>(
    () => [
      { value: 'all', label: resolveLabel('relation.grid.filter.type-all', overrides) },
      ...(typesQuery.data ?? []).map((entry) => ({ value: String(entry.id), label: entry.descAr })),
    ],
    [typesQuery.data, overrides],
  )

  const directionOptions = useMemo<SelectOption[]>(
    () => [
      { value: 'all', label: resolveLabel('relation.grid.filter.direction-all', overrides) },
      ...(Object.keys(RELATION_DIRECTION_LABELS) as RelationDirection[]).map((direction) => ({
        value: direction,
        label: resolveLabel(RELATION_DIRECTION_LABELS[direction], overrides),
      })),
    ],
    [overrides],
  )

  const columns: DataTableColumn<DocumentRelationResponse>[] = [
    {
      id: 'relation',
      headerKey: 'relation.grid.column.relation',
      hideable: false,
      sortable: true,
      sortValue: (relation) => relationPhrase(relation.type.relationTypeId, relation.direction),
      cell: (relation) => (
        <RelationDirectionLabel
          relationTypeId={relation.type.relationTypeId}
          direction={relation.direction}
        />
      ),
    },
    {
      id: 'type',
      headerKey: 'relation.grid.column.type',
      sortable: true,
      sortValue: (relation) => typeLabel(relation.type.relationTypeId),
      cell: (relation) => <RelationTypeBadge relationTypeId={relation.type.relationTypeId} />,
    },
    {
      id: 'document',
      headerKey: 'relation.grid.column.document',
      hideable: false,
      sortable: true,
      sortValue: (relation) => relation.relatedDocument.title,
      cell: (relation) => (
        <span className="flex max-w-72 flex-col">
          <span className="truncate font-medium text-foreground" title={relation.relatedDocument.title}>
            {relation.relatedDocument.title}
          </span>
          <OfficialNumberTag officialNumberText={relation.relatedDocument.officialNumberText} />
        </span>
      ),
    },
    {
      id: 'status',
      headerKey: 'relation.grid.column.status',
      sortable: true,
      sortValue: (relation) => relation.relatedDocument.statusId,
      cell: (relation) => <DocumentStatusBadge statusId={relation.relatedDocument.statusId} />,
    },
    {
      id: 'notes',
      headerKey: 'relation.grid.column.notes',
      cell: (relation) =>
        relation.notes ? (
          <span className="block max-w-56 truncate text-xs text-muted" title={relation.notes}>
            {relation.notes}
          </span>
        ) : (
          <span className="text-xs text-muted">—</span>
        ),
    },
    {
      id: 'createdAt',
      headerKey: 'relation.grid.column.created-at',
      sortable: true,
      sortValue: (relation) => relation.createdAt,
      cell: (relation) => (
        <span dir="ltr" className="whitespace-nowrap text-xs text-muted">
          {formatDateTime(relation.createdAt)}
        </span>
      ),
    },
  ]

  const copyNumber = async (value: string) => {
    try {
      await navigator.clipboard.writeText(value)
      toast.success(t('documents.toast.copied', { label: t('relation.grid.copy.number-label') }))
    } catch {
      toast.error(t('documents.toast.copy-failed'))
    }
  }

  const buildRowMenu = (relation: DocumentRelationResponse): MenuEntry[] => {
    const number = relation.relatedDocument.officialNumberText
    return [
      {
        id: 'open',
        label: t('relation.grid.row.open'),
        icon: <ExternalLink className="size-4" aria-hidden />,
        onSelect: () => navigate(ROUTES.DOCUMENT_DETAIL(relation.relatedDocument.documentId)),
      },
      {
        id: 'copy-number',
        label: t('relation.grid.row.copy-number'),
        icon: <Hash className="size-4" aria-hidden />,
        disabled: !number,
        onSelect: () => void copyNumber(number ?? ''),
      },
      ...(canManage
        ? ([
            { type: 'separator' },
            {
              id: 'remove',
              label: t('relation.grid.row.remove'),
              icon: <Trash2 className="size-4" aria-hidden />,
              danger: true,
              onSelect: () => onRemove(relation),
            },
          ] satisfies MenuEntry[])
        : []),
    ]
  }

  const refreshEntry: MenuEntry = {
    id: 'refresh',
    label: t('relation.grid.refresh'),
    icon: <RefreshCw className="size-4" aria-hidden />,
    onSelect: onRefresh,
  }

  const addButton = canManage ? (
    <Button type="button" size="sm" onClick={onAdd}>
      <Plus className="size-4" aria-hidden />
      <Label k="relation.grid.action.add" />
    </Button>
  ) : undefined

  const hasQuery = Boolean(table.search) || Boolean(typeFilter) || Boolean(directionFilter)

  return (
    <DataTable
      tableId="document-relations"
      mode="client"
      caption={t('relation.grid.caption')}
      columns={columns}
      rows={rows}
      rowKey={(relation) => relation.relationId}
      rowLabel={(relation) => relation.relatedDocument.title}
      rowSearchText={(relation) =>
        [
          relation.relatedDocument.title,
          relation.relatedDocument.officialNumberText,
          typeLabel(relation.type.relationTypeId),
          relation.notes,
        ]
          .filter(Boolean)
          .join(' ')
      }
      isLoading={isLoading}
      error={error}
      onRetry={onRefresh}
      onRefresh={onRefresh}
      isFetching={isFetching}
      page={table.page}
      pageSize={table.pageSize}
      totalCount={rows.length}
      onPageChange={table.setPage}
      onPageSizeChange={table.setPageSize}
      search={table.search}
      onSearchChange={table.setSearch}
      searchPlaceholder={t('relation.grid.search-placeholder')}
      sortKey={table.sortKey}
      sortDirection={table.sortDirection}
      onSortChange={table.setSort}
      toolbar={addButton}
      filters={
        <div className="flex flex-wrap items-center gap-2">
          <Select
            aria-label={t('relation.grid.filter.type-aria')}
            value={typeFilter || 'all'}
            onValueChange={(value) => table.setFilter('type', value === 'all' ? null : value)}
            options={typeOptions}
            className="w-44"
          />
          <GuideChip id="document-details.relations-types" />
          <Select
            aria-label={t('relation.grid.filter.direction-aria')}
            value={directionFilter || 'all'}
            onValueChange={(value) => table.setFilter('direction', value === 'all' ? null : value)}
            options={directionOptions}
            className="w-44"
          />
          <GuideChip id="document-details.relations-direction" />
          {hasQuery && (
            <Button
              type="button"
              variant="ghost"
              size="sm"
              onClick={() => {
                table.setFilter('type', null)
                table.setFilter('direction', null)
                table.clearQuery()
              }}
            >
              <Label k="relation.grid.clear" />
            </Button>
          )}
        </div>
      }
      rowMenu={buildRowMenu}
      headerMenu={[refreshEntry]}
      emptyMenu={[refreshEntry]}
      emptyAction={(relations?.length ?? 0) === 0 ? addButton : undefined}
      emptyTitle={t(hasQuery ? 'relation.grid.empty.matching-title' : 'relation.grid.empty.title')}
      emptyDescription={t(
        hasQuery
          ? 'relation.grid.empty.matching-description'
          : 'relation.grid.empty.description',
      )}
    />
  )
}
