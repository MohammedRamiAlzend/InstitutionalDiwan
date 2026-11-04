import { useEffect, useId, useRef, useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { Bell, BellRing, Check, CheckCheck, X } from 'lucide-react'
import { toast } from 'sonner'
import {
  NotificationItem,
  useNotifications,
  useUnreadCount,
  type NotificationResponse,
} from '@/entities/notification'
import { useNotificationConnection } from '@/features/notification-live'
import {
  useDismissMutation,
  useMarkAllReadMutation,
  useMarkReadMutation,
} from '@/features/notification-manage'
import { toArabicErrorMessage } from '@/shared/api'
import { ROUTES } from '@/shared/config'
import { cn, formatNumber, safeRelativeUrl } from '@/shared/lib'
import { Button, Skeleton } from '@/shared/ui'

const PANEL_SIZE = 8

/** Header bell: unread badge, live-connection indicator and the latest notifications. */
export function NotificationBell() {
  const navigate = useNavigate()
  const panelId = useId()
  const rootRef = useRef<HTMLDivElement>(null)
  const [open, setOpen] = useState(false)

  const connection = useNotificationConnection()
  const unreadQuery = useUnreadCount({ connected: connection === 'connected' })
  const listQuery = useNotifications({ page: 1, pageSize: PANEL_SIZE }, { enabled: open })
  const markRead = useMarkReadMutation()
  const dismiss = useDismissMutation()
  const markAllRead = useMarkAllReadMutation()

  const unread = unreadQuery.data?.count ?? 0
  const badge = unread > 99 ? '99+' : formatNumber(unread)
  const items = listQuery.data?.items ?? []

  useEffect(() => {
    if (!open) return

    const onPointerDown = (event: PointerEvent) => {
      if (rootRef.current && !rootRef.current.contains(event.target as Node)) setOpen(false)
    }
    const onKeyDown = (event: KeyboardEvent) => {
      if (event.key === 'Escape') setOpen(false)
    }
    document.addEventListener('pointerdown', onPointerDown)
    document.addEventListener('keydown', onKeyDown)
    return () => {
      document.removeEventListener('pointerdown', onPointerDown)
      document.removeEventListener('keydown', onKeyDown)
    }
  }, [open])

  const fail = (error: unknown) => toast.error(toArabicErrorMessage(error))

  const openNotification = (notification: NotificationResponse) => {
    markRead.mutate(
      { recipientId: notification.recipientId, typeId: notification.typeId, isRead: notification.isRead },
      { onError: fail },
    )
    setOpen(false)
    const target = safeRelativeUrl(notification.actionUrl)
    if (target) navigate(target)
  }

  const connectionLabel =
    connection === 'connected'
      ? 'التحديث الفوري يعمل'
      : connection === 'idle'
        ? ''
        : 'غير متصل بالتحديث الفوري — يُحدَّث العدّاد كل دقيقة'

  return (
    <div ref={rootRef} className="relative">
      <Button
        type="button"
        variant="ghost"
        size="icon"
        className="relative"
        aria-label={unread > 0 ? `الإشعارات، ${unread} غير مقروءة` : 'الإشعارات'}
        aria-expanded={open}
        aria-controls={panelId}
        title="الإشعارات"
        onClick={() => setOpen((previous) => !previous)}
      >
        {unread > 0 ? (
          <BellRing className="size-5" aria-hidden />
        ) : (
          <Bell className="size-5" aria-hidden />
        )}
        {unread > 0 && (
          <span
            aria-hidden
            className="absolute -top-0.5 -end-0.5 flex min-w-4 items-center justify-center rounded-full bg-danger px-1 text-[10px] font-bold leading-4 text-white"
          >
            {badge}
          </span>
        )}
      </Button>

      {open && (
        <div
          id={panelId}
          role="dialog"
          aria-label="آخر الإشعارات"
          className="absolute end-0 top-full z-40 mt-2 w-[min(26rem,calc(100vw-2rem))] rounded-xl border border-border bg-background shadow-xl animate-content-in"
        >
            <div className="flex items-center justify-between gap-2 border-b border-border px-3 py-2">
              <div className="flex flex-col">
                <span className="flex items-center gap-1.5 text-sm font-semibold text-foreground">
                  الإشعارات
                </span>
                {connectionLabel && (
                  <span
                    className={cn(
                      'flex items-center gap-1.5 text-[11px]',
                      connection === 'connected' ? 'text-success' : 'text-warning',
                    )}
                  >
                    <span
                      aria-hidden
                      className={cn(
                        'size-1.5 rounded-full',
                        connection === 'connected' ? 'bg-success' : 'bg-warning',
                      )}
                    />
                    {connectionLabel}
                  </span>
                )}
              </div>
              <Button
                type="button"
                variant="ghost"
                size="sm"
                disabled={unread === 0 || markAllRead.isPending}
                onClick={() => markAllRead.mutate(undefined, { onError: fail })}
              >
                <CheckCheck className="size-4" aria-hidden />
                قراءة الكل
              </Button>
            </div>

          <div className="max-h-96 overflow-y-auto p-1.5">
            {listQuery.isLoading ? (
              <div className="flex flex-col gap-2 p-2" aria-busy>
                <Skeleton className="h-14 w-full" />
                <Skeleton className="h-14 w-full" />
                <Skeleton className="h-14 w-full" />
              </div>
            ) : listQuery.isError ? (
              <div className="flex flex-col items-center gap-2 p-6 text-center">
                <p className="text-sm text-danger">{toArabicErrorMessage(listQuery.error)}</p>
                <Button type="button" variant="outline" size="sm" onClick={() => void listQuery.refetch()}>
                  إعادة المحاولة
                </Button>
              </div>
            ) : items.length === 0 ? (
              <p className="p-6 text-center text-sm text-muted">لا توجد إشعارات حتى الآن.</p>
            ) : (
              <ul className="flex flex-col gap-0.5">
                {items.map((notification) => (
                  <li key={notification.recipientId}>
                    <NotificationItem
                      notification={notification}
                      onOpen={openNotification}
                      actions={
                        <>
                          {!notification.isRead && (
                            <Button
                              type="button"
                              variant="ghost"
                              size="icon"
                              className="size-7"
                              aria-label="تعليم كمقروء"
                              title="تعليم كمقروء"
                              onClick={() =>
                                markRead.mutate(
                                  {
                                    recipientId: notification.recipientId,
                                    typeId: notification.typeId,
                                    isRead: false,
                                  },
                                  { onError: fail },
                                )
                              }
                            >
                              <Check className="size-4" aria-hidden />
                            </Button>
                          )}
                          <Button
                            type="button"
                            variant="ghost"
                            size="icon"
                            className="size-7"
                            aria-label="إخفاء الإشعار"
                            title="إخفاء الإشعار"
                            onClick={() =>
                              dismiss.mutate(
                                {
                                  recipientId: notification.recipientId,
                                  typeId: notification.typeId,
                                  isRead: notification.isRead,
                                },
                                { onError: fail },
                              )
                            }
                          >
                            <X className="size-4" aria-hidden />
                          </Button>
                        </>
                      }
                    />
                  </li>
                ))}
              </ul>
            )}
          </div>

          <div className="border-t border-border p-2">
            <Link
              to={ROUTES.NOTIFICATIONS}
              onClick={() => setOpen(false)}
              className="block rounded-lg px-3 py-2 text-center text-sm font-medium text-accent hover:bg-surface-muted"
            >
              عرض كل الإشعارات
            </Link>
          </div>
        </div>
      )}
    </div>
  )
}
