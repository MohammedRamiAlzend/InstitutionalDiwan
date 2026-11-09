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
import { formatDateTime, type TableState } from '@/shared/lib'
import {
  Button,
  DataTable,
  GuideChip,
  Select,
  type DataTableColumn,
  type MenuEntry,
  type SelectOption,
} from '@/shared/ui'

const DIRECTION_OPTIONS: SelectOption[] = [
  { value: 'all', label: 'كل الاتجاهات' },
  ...(Object.keys(RELATION_DIRECTION_LABELS) as RelationDirection[]).map((direction) => ({
    value: direction,
    label: RELATION_DIRECTION_LABELS[direction],
  })),
]

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
      { value: 'all', label: 'كل الأنواع' },
      ...(typesQuery.data ?? []).map((entry) => ({ value: String(entry.id), label: entry.descAr })),
    ],
    [typesQuery.data],
  )

  const columns: DataTableColumn<DocumentRelationResponse>[] = [
    {
      id: 'relation',
      header: 'الرابط',
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
      header: 'النوع',
      sortable: true,
      sortValue: (relation) => typeLabel(relation.type.relationTypeId),
      cell: (relation) => <RelationTypeBadge relationTypeId={relation.type.relationTypeId} />,
    },
    {
      id: 'document',
      header: 'الوثيقة المقابلة',
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
      header: 'حالتها',
      sortable: true,
      sortValue: (relation) => relation.relatedDocument.statusId,
      cell: (relation) => <DocumentStatusBadge statusId={relation.relatedDocument.statusId} />,
    },
    {
      id: 'notes',
      header: 'ملاحظات',
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
      header: 'تاريخ الإضافة',
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
      toast.success('تم نسخ رقم النظام.')
    } catch {
      toast.error('تعذّر النسخ إلى الحافظة.')
    }
  }

  const buildRowMenu = (relation: DocumentRelationResponse): MenuEntry[] => {
    const number = relation.relatedDocument.officialNumberText
    return [
      {
        id: 'open',
        label: 'فتح الوثيقة المقابلة',
        icon: <ExternalLink className="size-4" aria-hidden />,
        onSelect: () => navigate(ROUTES.DOCUMENT_DETAIL(relation.relatedDocument.documentId)),
      },
      {
        id: 'copy-number',
        label: 'نسخ رقم النظام',
        icon: <Hash className="size-4" aria-hidden />,
        disabled: !number,
        onSelect: () => void copyNumber(number ?? ''),
      },
      ...(canManage
        ? ([
            { type: 'separator' },
            {
              id: 'remove',
              label: 'إزالة الرابط',
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
    label: 'تحديث الروابط',
    icon: <RefreshCw className="size-4" aria-hidden />,
    onSelect: onRefresh,
  }

  const addButton = canManage ? (
    <Button type="button" size="sm" onClick={onAdd}>
      <Plus className="size-4" aria-hidden />
      إضافة رابط
    </Button>
  ) : undefined

  const hasQuery = Boolean(table.search) || Boolean(typeFilter) || Boolean(directionFilter)

  return (
    <DataTable
      tableId="document-relations"
      mode="client"
      caption="روابط الوثيقة"
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
      searchPlaceholder="ابحث باسم الوثيقة أو رقمها…"
      sortKey={table.sortKey}
      sortDirection={table.sortDirection}
      onSortChange={table.setSort}
      toolbar={addButton}
      filters={
        <div className="flex flex-wrap items-center gap-2">
          <Select
            aria-label="تصفية حسب نوع الرابط"
            value={typeFilter || 'all'}
            onValueChange={(value) => table.setFilter('type', value === 'all' ? null : value)}
            options={typeOptions}
            className="w-44"
          />
          <GuideChip id="document-details.relations-types" />
          <Select
            aria-label="تصفية حسب اتجاه الرابط"
            value={directionFilter || 'all'}
            onValueChange={(value) => table.setFilter('direction', value === 'all' ? null : value)}
            options={DIRECTION_OPTIONS}
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
              مسح البحث والمرشحات
            </Button>
          )}
        </div>
      }
      rowMenu={buildRowMenu}
      headerMenu={[refreshEntry]}
      emptyMenu={[refreshEntry]}
      emptyAction={(relations?.length ?? 0) === 0 ? addButton : undefined}
      emptyTitle={hasQuery ? 'لا توجد روابط مطابقة.' : 'لا روابط لهذه الوثيقة بعد.'}
      emptyDescription={
        hasQuery
          ? 'جرّب تعديل البحث أو المرشحات لعرض روابط أخرى.'
          : 'اربطها بردّ أو بوثيقة مرجعية أو بوثيقة تلغيها، فتظهر الروابط هنا وفي الوثيقة المقابلة.'
      }
    />
  )
}
