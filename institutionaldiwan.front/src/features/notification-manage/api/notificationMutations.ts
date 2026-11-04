import { useMutation, useQueryClient, type QueryClient } from '@tanstack/react-query'
import {
  notificationKeys,
  type NotificationResponse,
  type UnreadCountResponse,
} from '@/entities/notification'
import { useSessionStore } from '@/entities/user'
import { httpClient } from '@/shared/api'
import type { PagedResult } from '@/shared/types'

type InboxPage = PagedResult<NotificationResponse>

/** What the caller knows about the row, so the unread badge can move before the server answers. */
export interface NotificationTarget {
  recipientId: string
  typeId: number
  isRead: boolean
}

interface Snapshot {
  lists: [readonly unknown[], InboxPage | undefined][]
  unread: UnreadCountResponse | undefined
}

function takeSnapshot(queryClient: QueryClient, userId: string): Snapshot {
  return {
    lists: queryClient.getQueriesData<InboxPage>({ queryKey: notificationKeys.lists(userId) }),
    unread: queryClient.getQueryData<UnreadCountResponse>(notificationKeys.unread(userId)),
  }
}

function restoreSnapshot(queryClient: QueryClient, userId: string, snapshot: Snapshot | undefined) {
  if (!snapshot) return
  for (const [key, data] of snapshot.lists) queryClient.setQueryData(key, data)
  queryClient.setQueryData(notificationKeys.unread(userId), snapshot.unread)
}

/** One fewer unread row of `typeId`; the total never drops below zero. */
function decrementUnread(
  queryClient: QueryClient,
  userId: string,
  typeId: number,
) {
  queryClient.setQueryData<UnreadCountResponse>(notificationKeys.unread(userId), (current) => {
    if (!current) return current
    return {
      count: Math.max(0, current.count - 1),
      byType: current.byType
        .map((row) => (row.typeId === typeId ? { ...row, count: row.count - 1 } : row))
        .filter((row) => row.count > 0),
    }
  })
}

function useOptimisticContext() {
  const queryClient = useQueryClient()
  const userId = useSessionStore((state) => state.user?.userId ?? '')

  const begin = async (): Promise<Snapshot> => {
    await queryClient.cancelQueries({ queryKey: notificationKeys.all(userId) })
    return takeSnapshot(queryClient, userId)
  }
  const settle = () =>
    queryClient.invalidateQueries({ queryKey: notificationKeys.all(userId) })

  return { queryClient, userId, begin, settle }
}

/** `POST /api/notifications/{recipientId}/read` — idempotent. */
export function useMarkReadMutation() {
  const { queryClient, userId, begin, settle } = useOptimisticContext()

  return useMutation({
    mutationFn: async (target: NotificationTarget): Promise<void> => {
      await httpClient.post(`/notifications/${target.recipientId}/read`)
    },
    onMutate: async (target) => {
      const snapshot = await begin()
      queryClient.setQueriesData<InboxPage>({ queryKey: notificationKeys.lists(userId) }, (page) =>
        page
          ? {
              ...page,
              items: page.items.map((item) =>
                item.recipientId === target.recipientId ? { ...item, isRead: true } : item,
              ),
            }
          : page,
      )
      if (!target.isRead) decrementUnread(queryClient, userId, target.typeId)
      return snapshot
    },
    onError: (_error, _target, snapshot) => restoreSnapshot(queryClient, userId, snapshot),
    onSettled: settle,
  })
}

/** `POST /api/notifications/{recipientId}/dismiss` — idempotent; the row leaves the list. */
export function useDismissMutation() {
  const { queryClient, userId, begin, settle } = useOptimisticContext()

  return useMutation({
    mutationFn: async (target: NotificationTarget): Promise<void> => {
      await httpClient.post(`/notifications/${target.recipientId}/dismiss`)
    },
    onMutate: async (target) => {
      const snapshot = await begin()
      queryClient.setQueriesData<InboxPage>({ queryKey: notificationKeys.lists(userId) }, (page) =>
        page
          ? {
              ...page,
              items: page.items.filter((item) => item.recipientId !== target.recipientId),
              totalCount: Math.max(0, page.totalCount - 1),
            }
          : page,
      )
      if (!target.isRead) decrementUnread(queryClient, userId, target.typeId)
      return snapshot
    },
    onError: (_error, _target, snapshot) => restoreSnapshot(queryClient, userId, snapshot),
    onSettled: settle,
  })
}

/** `POST /api/notifications/read-all` — returns how many rows changed. */
export function useMarkAllReadMutation() {
  const { queryClient, userId, begin, settle } = useOptimisticContext()

  return useMutation({
    mutationFn: async (): Promise<number> => {
      const { data } = await httpClient.post<{ count: number }>('/notifications/read-all')
      return data.count
    },
    onMutate: async () => {
      const snapshot = await begin()
      queryClient.setQueriesData<InboxPage>({ queryKey: notificationKeys.lists(userId) }, (page) =>
        page
          ? { ...page, items: page.items.map((item) => ({ ...item, isRead: true })) }
          : page,
      )
      queryClient.setQueryData<UnreadCountResponse>(notificationKeys.unread(userId), {
        count: 0,
        byType: [],
      })
      return snapshot
    },
    onError: (_error, _vars, snapshot) => restoreSnapshot(queryClient, userId, snapshot),
    onSettled: settle,
  })
}
