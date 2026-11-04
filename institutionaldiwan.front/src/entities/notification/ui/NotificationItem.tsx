import type { ReactNode } from 'react'
import { LOOKUP_SLUGS, LookupLabel } from '@/entities/lookup'
import { cn, formatDateTime } from '@/shared/lib'
import type { NotificationResponse } from '../model/types'
import { NotificationPriorityBadge } from './NotificationPriorityBadge'
import { NotificationTypeIcon } from './NotificationTypeIcon'

interface NotificationItemProps {
  notification: NotificationResponse
  /** Activating the body (open the target and mark read). */
  onOpen: (notification: NotificationResponse) => void
  /** Inline actions such as «تعليم كمقروء» / «إخفاء». */
  actions?: ReactNode
  className?: string
}

/** One notification row shared by the bell dropdown and the inbox page. */
export function NotificationItem({ notification, onOpen, actions, className }: NotificationItemProps) {
  const { isRead } = notification

  return (
    <div
      className={cn(
        'flex items-start gap-3 rounded-lg px-3 py-2.5 transition-colors hover:bg-surface-muted',
        !isRead && 'bg-accent/5',
        className,
      )}
    >
      <span
        className={cn(
          'mt-0.5 flex size-8 shrink-0 items-center justify-center rounded-full',
          isRead ? 'bg-surface-muted text-muted' : 'bg-accent/10 text-accent',
        )}
      >
        <NotificationTypeIcon typeId={notification.typeId} />
      </span>

      <button
        type="button"
        onClick={() => onOpen(notification)}
        className="flex min-w-0 flex-1 cursor-pointer flex-col items-start gap-0.5 text-start"
      >
        <span className="flex w-full items-center gap-2">
          {!isRead && <span className="size-2 shrink-0 rounded-full bg-accent" aria-label="غير مقروء" />}
          <span className={cn('truncate text-sm', isRead ? 'text-foreground' : 'font-semibold text-foreground')}>
            {notification.titleAr}
          </span>
          <NotificationPriorityBadge priorityId={notification.priorityId} />
        </span>
        <span className="line-clamp-2 text-xs text-muted">{notification.bodyAr}</span>
        <span className="flex items-center gap-2 text-[11px] text-muted">
          <LookupLabel lookup={LOOKUP_SLUGS.NotificationTypes} id={notification.typeId} className="text-[11px]" />
          <span aria-hidden>·</span>
          <span dir="ltr">{formatDateTime(notification.createdAt)}</span>
        </span>
      </button>

      {actions && <div className="flex shrink-0 items-center gap-1">{actions}</div>}
    </div>
  )
}
