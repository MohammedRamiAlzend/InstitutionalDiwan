import { useMemo } from 'react'
import { useNavigate } from 'react-router-dom'
import { Check, CheckCheck, Copy, ExternalLink, EyeOff, RefreshCw } from 'lucide-react'
import { toast } from 'sonner'
import { LOOKUP_SLUGS, LookupLabel, useLookupEntries } from '@/entities/lookup'
import {
  NotificationPriorityBadge,
  NotificationTypeIcon,
  useNotifications,
  useUnreadCount,
  type NotificationResponse,
  type NotificationSortKey,
} from '@/entities/notification'
import {
  useDismissMutation,
  useMarkAllReadMutation,
  useMarkReadMutation,
} from '@/features/notification-manage'
import { useNotificationConnection } from '@/features/notification-live'
import { toArabicErrorMessage } from '@/shared/api'
import { cn, formatDateTime, formatNumber, safeRelativeUrl, useTableState } from '@/shared/lib'
import {
  Button,
  DataTable,
  GuideChip,
  Select,
  type DataTableColumn,
  type MenuEntry,
} from '@/shared/ui'

const SORT_KEYS: readonly NotificationSortKey[] = ['deliveredAt', 'priority', 'type', 'isRead']

function resolveSortKey(raw: string | null): NotificationSortKey {
  return SORT_KEYS.find((key) => key === raw) ?? 'deliveredAt'
}

function optionalId(value: string | undefined): number | undefined {
  if (!value) return undefined
  const parsed = Number(value)
  return Number.isInteger(parsed) && parsed > 0 ? parsed : undefined
}

/** Module 10 inbox grid: server search/sort/paging, unread and type filters, row menus. */
export function NotificationInbox() {
  const navigate = useNavigate()
  const table = useTableState({
    defaultSortKey: 'deliveredAt',
    defaultSortDirection: 'desc',
    filterKeys: ['unread', 'type'],
  })

  const listQuery = useNotifications({
    page: table.page,
    pageSize: table.pageSize,
    search: table.search || undefined,
    sortBy: resolveSortKey(table.sortKey),
    sortDescending: table.sortDirection === 'desc',
    unreadOnly: table.filters.unread === '1' ? true : undefined,
    typeId: optionalId(table.filters.type),
  })
  const connection = useNotificationConnection()
  const unreadQuery = useUnreadCount({ connected: connection === 'connected' })
  const typesQuery = useLookupEntries(LOOKUP_SLUGS.NotificationTypes)

  const markRead = useMarkReadMutation()
  const dismiss = useDismissMutation()
  const markAllRead = useMarkAllReadMutation()
  const fail = (error: unknown) => toast.error(toArabicErrorMessage(error))

  const unreadByType = useMemo(
    () => new Map((unreadQuery.data?.byType ?? []).map((row) => [row.typeId, row.count])),
    [unreadQuery.data],
  )
  const totalUnread = unreadQuery.data?.count ?? 0

  const typeOptions = useMemo(
    () => [
      { value: 'all', label: 'كل الأنواع' },
      ...(typesQuery.data ?? []).map((entry) => {
        const count = unreadByType.get(entry.id) ?? 0
        return {
          value: String(entry.id),
          label: count > 0 ? `${entry.descAr} (${formatNumber(count)})` : entry.descAr,
        }
      }),
    ],
    [typesQuery.data, unreadByType],
  )
  const readOptions = [
    { value: 'all', label: 'كل الإشعارات' },
    { value: '1', label: `غير المقروءة فقط (${formatNumber(totalUnread)})` },
  ]

  const target = (notification: NotificationResponse) => ({
    recipientId: notification.recipientId,
    typeId: notification.typeId,
    isRead: notification.isRead,
  })

  const open = (notification: NotificationResponse) => {
    markRead.mutate(target(notification), { onError: fail })
    const to = safeRelativeUrl(notification.actionUrl)
    if (to) navigate(to)
  }

  const copyLink = async (notification: NotificationResponse) => {
    const to = safeRelativeUrl(notification.actionUrl)
    if (!to) return
    try {
      await navigator.clipboard.writeText(`${window.location.origin}${to}`)
      toast.success('تم نسخ الرابط.')
    } catch {
      toast.error('تعذّر النسخ إلى الحافظة.')
    }
  }

  const columns = useMemo<DataTableColumn<NotificationResponse>[]>(
    () => [
      {
        id: 'title',
        header: 'الإشعار',
        hideable: false,
        cell: (notification) => (
          <button
            type="button"
            onClick={() => open(notification)}
            className="flex min-w-60 cursor-pointer items-start gap-2 text-start"
          >
            <span
              className={cn(
                'mt-1.5 size-2 shrink-0 rounded-full',
                notification.isRead ? 'bg-transparent' : 'bg-accent',
              )}
              aria-label={notification.isRead ? undefined : 'غير مقروء'}
            />
            <span className="flex min-w-0 flex-col gap-0.5">
              <span
                className={cn(
                  'text-sm text-foreground',
                  !notification.isRead && 'font-semibold',
                )}
              >
                {notification.titleAr}
              </span>
              <span className="line-clamp-2 text-xs text-muted">{notification.bodyAr}</span>
            </span>
          </button>
        ),
      },
      {
        id: 'type',
        header: 'النوع',
        sortable: true,
        sortKey: 'type',
        cell: ({ typeId }) => (
          <span className="flex items-center gap-1.5 text-xs text-foreground">
            <NotificationTypeIcon typeId={typeId} className="size-4 text-muted" />
            <LookupLabel lookup={LOOKUP_SLUGS.NotificationTypes} id={typeId} className="text-xs" />
          </span>
        ),
      },
      {
        id: 'priority',
        header: 'الأولوية',
        sortable: true,
        sortKey: 'priority',
        cell: ({ priorityId }) =>
          priorityId === 1 ? (
            <LookupLabel lookup={LOOKUP_SLUGS.NotificationPriorities} id={priorityId} className="text-xs text-muted" />
          ) : (
            <NotificationPriorityBadge priorityId={priorityId} />
          ),
      },
      {
        id: 'state',
        header: 'الحالة',
        sortable: true,
        sortKey: 'isRead',
        cell: ({ isRead }) => (
          <span className={cn('text-xs', isRead ? 'text-muted' : 'font-medium text-accent')}>
            {isRead ? 'مقروء' : 'غير مقروء'}
          </span>
        ),
      },
      {
        id: 'createdAt',
        header: 'وقت الوصول',
        sortable: true,
        sortKey: 'deliveredAt',
        cell: ({ createdAt }) => (
          <span dir="ltr" className="whitespace-nowrap text-xs text-foreground">
            {formatDateTime(createdAt)}
          </span>
        ),
      },
      {
        id: 'readAt',
        header: 'وقت القراءة',
        defaultHidden: true,
        cell: ({ readAt }) => (
          <span dir="ltr" className="whitespace-nowrap text-xs text-muted">
            {readAt ? formatDateTime(readAt) : '—'}
          </span>
        ),
      },
    ],
    // `open` only closes over stable mutation handles and the router.
    // eslint-disable-next-line react-hooks/exhaustive-deps
    [],
  )

  const buildRowMenu = (notification: NotificationResponse): MenuEntry[] => [
    {
      id: 'open',
      label: safeRelativeUrl(notification.actionUrl) ? 'فتح الوثيقة' : 'تعليم كمقروء وفتح',
      icon: <ExternalLink className="size-4" aria-hidden />,
      disabled: !safeRelativeUrl(notification.actionUrl),
      onSelect: () => open(notification),
    },
    {
      id: 'read',
      label: 'تعليم كمقروء',
      icon: <Check className="size-4" aria-hidden />,
      disabled: notification.isRead,
      onSelect: () => markRead.mutate(target(notification), { onError: fail }),
    },
    {
      id: 'dismiss',
      label: 'إخفاء الإشعار',
      icon: <EyeOff className="size-4" aria-hidden />,
      onSelect: () => dismiss.mutate(target(notification), { onError: fail }),
    },
    {
      id: 'copy',
      label: 'نسخ الرابط',
      icon: <Copy className="size-4" aria-hidden />,
      disabled: !safeRelativeUrl(notification.actionUrl),
      onSelect: () => void copyLink(notification),
    },
  ]

  const readAllEntry: MenuEntry = {
    id: 'read-all',
    label: 'قراءة الكل',
    icon: <CheckCheck className="size-4" aria-hidden />,
    disabled: totalUnread === 0,
    onSelect: () => markAllRead.mutate(undefined, { onError: fail }),
  }
  const refreshEntry: MenuEntry = {
    id: 'refresh',
    label: 'تحديث البيانات',
    icon: <RefreshCw className="size-4" aria-hidden />,
    onSelect: () => void listQuery.refetch(),
  }
  const headerMenu: MenuEntry[] = [readAllEntry, refreshEntry]

  const hasQuery = Boolean(table.search) || table.hasActiveFilters

  return (
    <DataTable
      tableId="notifications"
      caption="الإشعارات"
      columns={columns}
      rows={listQuery.data?.items}
      rowKey={(notification) => notification.recipientId}
      isLoading={listQuery.isLoading}
      error={listQuery.error}
      onRetry={() => void listQuery.refetch()}
      onRefresh={() => void listQuery.refetch()}
      isFetching={listQuery.isFetching}
      page={table.page}
      pageSize={table.pageSize}
      totalCount={listQuery.data?.totalCount ?? 0}
      onPageChange={table.setPage}
      onPageSizeChange={table.setPageSize}
      search={table.search}
      onSearchChange={table.setSearch}
      sortKey={table.sortKey}
      sortDirection={table.sortDirection}
      onSortChange={table.setSort}
      searchPlaceholder="ابحث في عنوان الإشعار أو نصه…"
      filters={
        <div className="flex flex-wrap items-center gap-2">
          <GuideChip id="notifications.filters" />
          <Select
            aria-label="تصفية حسب حالة القراءة"
            value={table.filters.unread || 'all'}
            onValueChange={(value) => table.setFilter('unread', value === 'all' ? null : value)}
            options={readOptions}
            className="w-52"
          />
          <Select
            aria-label="تصفية حسب نوع الإشعار"
            value={table.filters.type || 'all'}
            onValueChange={(value) => table.setFilter('type', value === 'all' ? null : value)}
            options={typeOptions}
            className="w-52"
          />
          {table.hasActiveFilters && (
            <Button type="button" variant="ghost" size="sm" onClick={table.clearFilters}>
              مسح المرشحات
            </Button>
          )}
        </div>
      }
      rowMenu={buildRowMenu}
      headerMenu={headerMenu}
      emptyMenu={headerMenu}
      rowLabel={(notification) => notification.titleAr}
      toolbar={
        <>
          <Button
            type="button"
            variant="outline"
            disabled={totalUnread === 0 || markAllRead.isPending}
            onClick={() => markAllRead.mutate(undefined, { onError: fail })}
          >
            <CheckCheck className="size-4" aria-hidden />
            قراءة الكل
          </Button>
          <GuideChip id="notifications.read-all" />
        </>
      }
      emptyTitle={hasQuery ? 'لا توجد إشعارات مطابقة.' : 'لا توجد إشعارات.'}
      emptyDescription={
        hasQuery
          ? 'جرّب تعديل البحث أو المرشحات.'
          : 'تصلك هنا تنبيهات الوثائق والإحالات الموجّهة إليك أو إلى قسمك.'
      }
      emptyAction={
        hasQuery ? (
          <Button type="button" variant="outline" onClick={table.clearQuery}>
            مسح البحث والمرشحات
          </Button>
        ) : undefined
      }
    />
  )
}
