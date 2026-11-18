import { useMemo, useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import {
  Copy,
  Eye,
  Hash,
  RefreshCw,
} from 'lucide-react'
import { toast } from 'sonner'
import { useDepartmentOptions, DepartmentLabel } from '@/entities/department'
import {
  documentRowLabel,
  isDocumentRestricted,
  OfficialNumberTag,
  RestrictedDocumentLabel,
} from '@/entities/document'
import {
  getMovementActions,
  getMovementCustody,
  isMovementTerminal,
  MovementStatusBadge,
  MovementStepStatusBadge,
  MovementTypeBadge,
  useMovementQueue,
  type MovementAction,
  type MovementQueueItemResponse,
  type MovementQueueKind,
} from '@/entities/movement'
import { LOOKUP_SLUGS, useLookupEntries } from '@/entities/lookup'
import { useCoversDepartment, useSessionStore, UserLabel } from '@/entities/user'
import {
  DESTRUCTIVE_ACTIONS,
  MOVEMENT_ACTION_ICONS,
  MOVEMENT_ACTION_LABEL_KEYS,
  MovementActionDialog,
} from '@/features/movement-routing'
import { Permissions, ROUTES } from '@/shared/config'
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

/** From the sender's own queue only these actions make sense; receives belong to the inbox. */
const SENDER_SIDE_ACTIONS: readonly MovementAction[] = [
  'send',
  'redirect',
  'close',
  'cancel',
]

interface MovementQueueTableProps {
  kind: MovementQueueKind
}

/** Client-side department queue: search, status/type filters, sorting, and routing menus. */
export function MovementQueueTable({ kind }: MovementQueueTableProps) {
  const table = useTableState({
    defaultSortKey: 'sentAt',
    defaultSortDirection: 'desc',
    filterKeys: ['statusId', 'movementTypeId'],
  })

  const hasPermission = useSessionStore((state) => state.hasPermission)
  const canEdit = hasPermission(Permissions.DocumentEdit)
  const coversDepartment = useCoversDepartment()
  const navigate = useNavigate()

  const queueQuery = useMovementQueue(kind, undefined)
  const departmentsQuery = useDepartmentOptions()
  const statusesQuery = useLookupEntries(LOOKUP_SLUGS.MovementStatuses)
  const typesQuery = useLookupEntries(LOOKUP_SLUGS.MovementTypes)

  const [pendingAction, setPendingAction] = useState<{
    action: MovementAction
    movementId: string
  } | null>(null)

  const departmentNames = useMemo(
    () =>
      new Map(
        (departmentsQuery.data ?? []).map((department) => [
          department.departmentId,
          department.nameAr,
        ]),
      ),
    [departmentsQuery.data],
  )

  const allStatusesLabel = useLabel('movements.filter.all-statuses')
  const allTypesLabel = useLabel('movements.filter.all-types')

  const statusOptions = useMemo(
    () => [
      { value: 'all', label: allStatusesLabel },
      ...(statusesQuery.data ?? []).map((entry) => ({
        value: String(entry.id),
        label: entry.descAr,
      })),
    ],
    [statusesQuery.data, allStatusesLabel],
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

  const rows = useMemo(
    () =>
      (queueQuery.data ?? []).filter((item) => {
        const statusId = table.filters.statusId
        if (statusId && String(item.movementStatusId) !== statusId) return false
        const typeId = table.filters.movementTypeId
        if (typeId && String(item.movementTypeId) !== typeId) return false
        return true
      }),
    [queueQuery.data, table.filters.statusId, table.filters.movementTypeId],
  )

  const today = new Date().toISOString().slice(0, 10)

  const columns = useMemo<DataTableColumn<MovementQueueItemResponse>[]>(
    () => [
      {
        id: 'document',
        headerKey: 'movements.column.document',
        sortable: true,
        sortKey: 'documentTitle',
        sortValue: (item) => item.documentTitle,
        hideable: false,
        cell: (item) =>
          isDocumentRestricted(item.documentTitle) ? (
            <div className="flex min-w-56 flex-col gap-1">
              <RestrictedDocumentLabel />
            </div>
          ) : (
            <div className="flex min-w-56 flex-col gap-1">
              <Link
                to={ROUTES.DOCUMENT_DETAIL(item.documentId)}
                className="text-sm font-medium text-foreground hover:text-accent hover:underline"
              >
                {item.documentTitle}
              </Link>
              <OfficialNumberTag officialNumberText={item.officialNumberText} />
            </div>
          ),
      },
      {
        id: 'type',
        headerKey: 'movements.column.type',
        cell: (item) => <MovementTypeBadge movementTypeId={item.movementTypeId} />,
      },
      {
        id: 'status',
        headerKey: 'movements.column.status',
        sortable: true,
        sortKey: 'status',
        sortValue: (item) => item.movementStatusId,
        cell: (item) => <MovementStatusBadge statusId={item.movementStatusId} />,
      },
      {
        id: 'step',
        headerKey: 'movements.column.step',
        sortable: true,
        sortKey: 'stepNo',
        sortValue: (item) => item.stepNo,
        cell: (item) => (
          <div className="flex flex-col gap-1">
            <span className="text-xs text-muted">
              <Label k="movements.step.prefix" /> <span dir="ltr">{item.stepNo}</span>
            </span>
            <MovementStepStatusBadge stepStatusId={item.stepStatusId} />
          </div>
        ),
      },
      {
        id: 'route',
        headerKey: 'movements.column.route',
        cell: (item) => (
          <div className="flex min-w-44 flex-col gap-0.5 text-xs">
            <span className="text-muted">
              <Label k="movements.route.from" />{' '}
              <span className="text-foreground">
                {departmentNames.get(item.fromDepartmentId) ?? (
                  <DepartmentLabel departmentId={item.fromDepartmentId} />
                )}
              </span>
            </span>
            <span className="text-muted">
              <Label k="movements.route.to" />{' '}
              <span className="text-foreground">
                {departmentNames.get(item.toDepartmentId) ?? (
                  <DepartmentLabel departmentId={item.toDepartmentId} />
                )}
              </span>
            </span>
          </div>
        ),
      },
      {
        id: 'dueDate',
        headerKey: 'movements.column.due-date',
        sortable: true,
        sortKey: 'dueDate',
        sortValue: (item) => item.dueDate,
        cell: (item) => {
          const overdue =
            Boolean(item.dueDate) &&
            !isMovementTerminal(item.movementStatusId) &&
            item.dueDate!.slice(0, 10) < today
          return (
            <span
              dir="ltr"
              className={
                overdue
                  ? 'whitespace-nowrap text-xs font-medium text-danger'
                  : 'whitespace-nowrap text-xs text-muted'
              }
            >
              {item.dueDate ? formatDate(item.dueDate) : '—'}
              {overdue && <span className="ms-1 text-[10px]"><Label k="movements.overdue" /></span>}
            </span>
          )
        },
      },
      {
        id: 'sentAt',
        headerKey: 'movements.column.sent-at',
        sortable: true,
        sortKey: 'sentAt',
        sortValue: (item) => item.sentAt ?? item.createdAt,
        cell: (item) => (
          <span dir="ltr" className="whitespace-nowrap text-xs text-muted">
            {formatDateTime(item.sentAt ?? item.createdAt)}
          </span>
        ),
      },
      {
        id: 'assignee',
        headerKey: 'movements.column.assignee',
        defaultHidden: true,
        cell: (item) => <UserLabel userId={item.assignedToUserId} />,
      },
      {
        id: 'instructions',
        headerKey: 'movements.column.instructions',
        defaultHidden: true,
        cell: (item) => (
          <span className="line-clamp-2 min-w-40 text-xs text-muted">
            {item.instructions ?? '—'}
          </span>
        ),
      },
      {
        id: 'createdAt',
        headerKey: 'movements.column.created-at',
        defaultHidden: true,
        cell: (item) => (
          <span dir="ltr" className="whitespace-nowrap text-xs text-muted">
            {formatDateTime(item.createdAt)}
          </span>
        ),
      },
    ],
    [departmentNames, today],
  )

  const copyValue = async (value: string, label: string) => {
    try {
      await navigator.clipboard.writeText(value)
      toast.success(t('movements.toast.copied', { label }))
    } catch {
      toast.error(t('movements.toast.copy-failed'))
    }
  }

  const actionsFor = (item: MovementQueueItemResponse): MovementAction[] => {
    const allowed = getMovementActions(
      item.movementStatusId,
      item.stepStatusId,
      canEdit,
      getMovementCustody(item, coversDepartment),
    )
    return kind === 'inbox'
      ? allowed
      : allowed.filter((action) => SENDER_SIDE_ACTIONS.includes(action))
  }

  const buildRowMenu = (item: MovementQueueItemResponse): MenuEntry[] => {
    const actions = actionsFor(item)

    const actionEntries: MenuEntry[] = actions.map((action) => {
      const Icon = MOVEMENT_ACTION_ICONS[action]
      return {
        id: `action-${action}`,
        label: t(MOVEMENT_ACTION_LABEL_KEYS[action]),
        icon: <Icon className="size-4" aria-hidden />,
        danger: DESTRUCTIVE_ACTIONS.includes(action),
        onSelect: () => setPendingAction({ action, movementId: item.movementId }),
      }
    })

    return [
      ...(isDocumentRestricted(item.documentTitle)
        ? []
        : [
            {
              id: 'open-document',
              label: t('movements.row.open'),
              icon: <Eye className="size-4" aria-hidden />,
              onSelect: () => navigate(ROUTES.DOCUMENT_DETAIL(item.documentId)),
            },
          ]),
      {
        id: 'copy-number',
        label: t('movements.row.copy-number'),
        icon: <Hash className="size-4" aria-hidden />,
        disabled: !item.officialNumberText,
        onSelect: () => void copyValue(item.officialNumberText ?? '', t('movements.copy.number-label')),
      },
      {
        id: 'copy-id',
        label: t('movements.row.copy-id'),
        icon: <Copy className="size-4" aria-hidden />,
        onSelect: () => void copyValue(item.movementId, t('movements.copy.id-label')),
      },
      ...(actionEntries.length > 0
        ? [{ type: 'separator' } as MenuEntry, ...actionEntries]
        : []),
    ]
  }

  const refreshEntry: MenuEntry = {
    id: 'refresh',
    label: t('movements.action.refresh'),
    icon: <RefreshCw className="size-4" aria-hidden />,
    onSelect: () => void queueQuery.refetch(),
  }

  const hasQuery = Boolean(table.search) || table.hasActiveFilters
  const isInbox = kind === 'inbox'

  return (
    <>
      <DataTable
        tableId={isInbox ? 'movements-inbox' : 'movements-outbox'}
        caption={isInbox ? t('movements.caption.inbox') : t('movements.caption.outbox')}
        mode="client"
        columns={columns}
        rows={rows}
        rowKey={(item) => item.movementId}
        isLoading={queueQuery.isLoading}
        error={queueQuery.error}
        onRetry={() => void queueQuery.refetch()}
        onRefresh={() => void queueQuery.refetch()}
        isFetching={queueQuery.isFetching}
        page={table.page}
        pageSize={table.pageSize}
        totalCount={rows.length}
        onPageChange={table.setPage}
        onPageSizeChange={table.setPageSize}
        search={table.search}
        onSearchChange={table.setSearch}
        sortKey={table.sortKey}
        sortDirection={table.sortDirection}
        onSortChange={table.setSort}
        searchPlaceholder={t('movements.search-placeholder')}
        rowSearchText={(item) =>
          [documentRowLabel(item.documentTitle), item.officialNumberText, item.instructions]
            .filter(Boolean)
            .join(' ')
        }
        filters={
          <div className="flex flex-wrap items-center gap-2">
            <GuideChip id="movements.filters" />
            <Select
              aria-label={t('movements.filter.status-aria')}
              value={table.filters.statusId || 'all'}
              onValueChange={(value) =>
                table.setFilter('statusId', value === 'all' ? null : value)
              }
              options={statusOptions}
              className="w-44"
            />
            <Select
              aria-label={t('movements.filter.type-aria')}
              value={table.filters.movementTypeId || 'all'}
              onValueChange={(value) =>
                table.setFilter('movementTypeId', value === 'all' ? null : value)
              }
              options={typeOptions}
              className="w-40"
            />
            {table.hasActiveFilters && (
              <Button type="button" variant="ghost" size="sm" onClick={table.clearFilters}>
                <Label k="movements.filter.clear" />
              </Button>
            )}
          </div>
        }
        rowMenu={buildRowMenu}
        headerMenu={[refreshEntry]}
        emptyMenu={[refreshEntry]}
        rowLabel={(item) => documentRowLabel(item.documentTitle)}
        emptyTitle={hasQuery ? t('movements.empty.no-match-title') : t('movements.empty.none-title')}
        emptyDescription={
          hasQuery
            ? t('movements.empty.no-match-description')
            : isInbox
              ? t('movements.empty.inbox-description')
              : t('movements.empty.outbox-description')
        }
        emptyAction={
          hasQuery ? (
            <Button type="button" variant="outline" onClick={table.clearQuery}>
              <Label k="movements.empty.clear-query" />
            </Button>
          ) : undefined
        }
      />

      {pendingAction && (
        <MovementActionDialog
          action={pendingAction.action}
          movementId={pendingAction.movementId}
          open
          onOpenChange={(open) => {
            if (!open) setPendingAction(null)
          }}
        />
      )}
    </>
  )
}
