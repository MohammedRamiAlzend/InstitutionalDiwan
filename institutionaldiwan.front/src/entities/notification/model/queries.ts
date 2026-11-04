import { useQuery } from '@tanstack/react-query'
import { useSessionStore } from '@/entities/user'
import { notificationApi } from '../api/notificationApi'
import type { NotificationListParams } from './types'

/**
 * Every key carries the user id so a different account signing in on the same browser never reads
 * the previous account's cached inbox.
 */
export const notificationKeys = {
  all: (userId: string) => ['notifications', userId] as const,
  lists: (userId: string) => [...notificationKeys.all(userId), 'list'] as const,
  list: (userId: string, params: NotificationListParams) =>
    [...notificationKeys.lists(userId), params] as const,
  unread: (userId: string) => [...notificationKeys.all(userId), 'unread'] as const,
}

/** The caller's inbox page. */
export function useNotifications(params: NotificationListParams, options?: { enabled?: boolean }) {
  const userId = useSessionStore((state) => state.user?.userId)

  return useQuery({
    queryKey: notificationKeys.list(userId ?? '', params),
    queryFn: () => notificationApi.list(params),
    enabled: Boolean(userId) && (options?.enabled ?? true),
    placeholderData: (previous) => previous,
    staleTime: 15 * 1000,
  })
}

/**
 * Total and per-type unread counts for the bell badge. While the live connection is down the count
 * is polled every minute; once connected the push invalidates it instead.
 */
export function useUnreadCount(options: { connected: boolean }) {
  const userId = useSessionStore((state) => state.user?.userId)

  return useQuery({
    queryKey: notificationKeys.unread(userId ?? ''),
    queryFn: () => notificationApi.unreadCount(),
    enabled: Boolean(userId),
    staleTime: 15 * 1000,
    refetchOnWindowFocus: true,
    refetchInterval: options.connected ? false : 60 * 1000,
  })
}
