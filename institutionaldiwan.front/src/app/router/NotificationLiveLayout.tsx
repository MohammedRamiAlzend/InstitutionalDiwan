import { useCallback } from 'react'
import { Outlet, useNavigate } from 'react-router-dom'
import type { NotificationHubPayload } from '@/entities/notification'
import { useMarkReadMutation } from '@/features/notification-manage'
import { useNotificationHub } from '@/features/notification-live'
import { safeRelativeUrl } from '@/shared/lib'

/**
 * Route layout that keeps the notification hub open for every authenticated page. It lives in the
 * app layer because it joins two features (the live connection and mark-as-read) and needs the
 * router for the «فتح» action of a toast.
 */
export function NotificationLiveLayout() {
  const navigate = useNavigate()
  const markRead = useMarkReadMutation()
  const { mutate: markAsRead } = markRead

  const openFromToast = useCallback(
    (payload: NotificationHubPayload) => {
      markAsRead({ recipientId: payload.recipientId, typeId: payload.typeId, isRead: false })
      const target = safeRelativeUrl(payload.actionUrl)
      if (target) navigate(target)
    },
    [markAsRead, navigate],
  )

  useNotificationHub({ onOpen: openFromToast })

  return <Outlet />
}
