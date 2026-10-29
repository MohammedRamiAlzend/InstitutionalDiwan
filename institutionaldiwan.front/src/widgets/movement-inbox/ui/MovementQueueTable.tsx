import { useMemo, useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import {
  Copy,
  Eye,
  Hash,
  RefreshCw,
} from 'lucide-react'
import { toast } from 'sonner'
import { useDepartmentOptions } from '@/entities/department'
import { OfficialNumberTag } from '@/entities/document'
import {
  getMovementActions,
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
import { useSessionStore } from '@/entities/user'
import {
  DESTRUCTIVE_ACTIONS,
  MOVEMENT_ACTION_ICONS,
  MOVEMENT_ACTION_LABELS,
  MovementActionDialog,
} from '@/features/movement-routing'
import { Permissions, ROUTES } from '@/shared/config'
import { formatDate, formatDateTime, formatShortId, useTableState } from '@/shared/lib'
import {
  Button,
  DataTable,
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

  const statusOptions = useMemo(
    () => [
      { value: 'all', label: 'كل الحالات' },
      ...(statusesQuery.data ?? []).map((entry) => ({
        value: String(entry.id),
        label: entry.descAr,
      })),
    ],
    [statusesQuery.data],
  )

  const typeOptions = useMemo(
    () => [
      { value: 'all', label: 'كل الأنواع' },
      ...(typesQuery.data ?? []).map((entry) => ({
        value: String(entry.id),
        label: entry.descAr,
      })),
    ],
    [typesQuery.data],
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
        header: 'الوثيقة',
        sortable: true,
        sortKey: 'documentTitle',
        sortValue: (item) => item.documentTitle,
        hideable: false,
        cell: (item) => (
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
        header: 'نوع الحركة',
        cell: (item) => <MovementTypeBadge movementTypeId={item.movementTypeId} />,
      },
      {
        id: 'status',
        header: 'حالة الحركة',
        sortable: true,
        sortKey: 'status',
        sortValue: (item) => item.movementStatusId,
        cell: (item) => <MovementStatusBadge statusId={item.movementStatusId} />,
      },
      {
        id: 'step',
        header: 'الخطوة',
        sortable: true,
        sortKey: 'stepNo',
        sortValue: (item) => item.stepNo,
        cell: (item) => (
          <div className="flex flex-col gap-1">
            <span className="text-xs text-muted">
              الخطوة <span dir="ltr">{item.stepNo}</span>
            </span>
            <MovementStepStatusBadge stepStatusId={item.stepStatusId} />
          </div>
        ),
      },
      {
        id: 'route',
        header: 'المسار',
        cell: (item) => (
          <div className="flex min-w-44 flex-col gap-0.5 text-xs">
            <span className="text-muted">
              من:{' '}
              <span className="text-foreground">
                {departmentNames.get(item.fromDepartmentId) ??
                  formatShortId(item.fromDepartmentId)}
              </span>
            </span>
            <span className="text-muted">
              إلى:{' '}
              <span className="text-foreground">
                {departmentNames.get(item.toDepartmentId) ??
                  formatShortId(item.toDepartmentId)}
              </span>
            </span>
          </div>
        ),
      },
      {
        id: 'dueDate',
        header: 'تاريخ الاستحقاق',
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
              {overdue && <span className="ms-1 text-[10px]">متأخر</span>}
            </span>
          )
        },
      },
      {
        id: 'sentAt',
        header: 'تاريخ الإرسال',
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
        header: 'المكلَّف',
        defaultHidden: true,
        cell: (item) => (
          <span dir="ltr" className="font-mono text-xs text-muted">
            {item.assignedToUserId ? formatShortId(item.assignedToUserId) : '—'}
          </span>
        ),
      },
      {
        id: 'instructions',
        header: 'التعليمات',
        defaultHidden: true,
        cell: (item) => (
          <span className="line-clamp-2 min-w-40 text-xs text-muted">
            {item.instructions ?? '—'}
          </span>
        ),
      },
      {
        id: 'createdAt',
        header: 'تاريخ الإنشاء',
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
      toast.success(`تم نسخ ${label}.`)
    } catch {
      toast.error('تعذّر النسخ إلى الحافظة.')
    }
  }

  const actionsFor = (item: MovementQueueItemResponse): MovementAction[] => {
    const allowed = getMovementActions(
      item.movementStatusId,
      item.stepStatusId,
      canEdit,
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
        label: MOVEMENT_ACTION_LABELS[action],
        icon: <Icon className="size-4" aria-hidden />,
        danger: DESTRUCTIVE_ACTIONS.includes(action),
        onSelect: () => setPendingAction({ action, movementId: item.movementId }),
      }
    })

    return [
      {
        id: 'open-document',
        label: 'فتح الوثيقة',
        icon: <Eye className="size-4" aria-hidden />,
        onSelect: () => navigate(ROUTES.DOCUMENT_DETAIL(item.documentId)),
      },
      {
        id: 'copy-number',
        label: 'نسخ الرقم الرسمي',
        icon: <Hash className="size-4" aria-hidden />,
        disabled: !item.officialNumberText,
        onSelect: () => void copyValue(item.officialNumberText ?? '', 'الرقم الرسمي'),
      },
      {
        id: 'copy-id',
        label: 'نسخ معرّف الحركة',
        icon: <Copy className="size-4" aria-hidden />,
        onSelect: () => void copyValue(item.movementId, 'معرّف الحركة'),
      },
      ...(actionEntries.length > 0
        ? [{ type: 'separator' } as MenuEntry, ...actionEntries]
        : []),
    ]
  }

  const refreshEntry: MenuEntry = {
    id: 'refresh',
    label: 'تحديث الصندوق',
    icon: <RefreshCw className="size-4" aria-hidden />,
    onSelect: () => void queueQuery.refetch(),
  }

  const hasQuery = Boolean(table.search) || table.hasActiveFilters
  const isInbox = kind === 'inbox'

  return (
    <>
      <DataTable
        tableId={isInbox ? 'movements-inbox' : 'movements-outbox'}
        caption={isInbox ? 'صندوق وارد القسم' : 'صندوق صادر القسم'}
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
        searchPlaceholder="ابحث بعنوان الوثيقة أو رقمها أو التعليمات…"
        rowSearchText={(item) =>
          [item.documentTitle, item.officialNumberText, item.instructions]
            .filter(Boolean)
            .join(' ')
        }
        filters={
          <div className="flex flex-wrap items-center gap-2">
            <Select
              aria-label="تصفية حسب حالة الحركة"
              value={table.filters.statusId || 'all'}
              onValueChange={(value) =>
                table.setFilter('statusId', value === 'all' ? null : value)
              }
              options={statusOptions}
              className="w-44"
            />
            <Select
              aria-label="تصفية حسب نوع الحركة"
              value={table.filters.movementTypeId || 'all'}
              onValueChange={(value) =>
                table.setFilter('movementTypeId', value === 'all' ? null : value)
              }
              options={typeOptions}
              className="w-40"
            />
            {table.hasActiveFilters && (
              <Button type="button" variant="ghost" size="sm" onClick={table.clearFilters}>
                مسح المرشحات
              </Button>
            )}
          </div>
        }
        rowMenu={buildRowMenu}
        headerMenu={[refreshEntry]}
        emptyMenu={[refreshEntry]}
        rowLabel={(item) => item.documentTitle}
        emptyTitle={hasQuery ? 'لا توجد حركات مطابقة.' : 'الصندوق فارغ.'}
        emptyDescription={
          hasQuery
            ? 'جرّب تعديل البحث أو المرشحات لعرض حركات أخرى.'
            : isInbox
              ? 'ستظهر هنا الحركات الموجهة إلى قسمك المبدئي فور إرسالها.'
              : 'ستظهر هنا الحركات التي أرسلها قسمك المبدئي ولم تُغلق بعد.'
        }
        emptyAction={
          hasQuery ? (
            <Button type="button" variant="outline" onClick={table.clearQuery}>
              مسح البحث والمرشحات
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
