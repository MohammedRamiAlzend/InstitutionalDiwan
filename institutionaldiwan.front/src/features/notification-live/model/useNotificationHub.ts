import { useEffect, useRef } from 'react'
import { useQueryClient } from '@tanstack/react-query'
import type { HubConnection } from '@microsoft/signalr'
import { toast } from 'sonner'
import { notificationKeys, type NotificationHubPayload } from '@/entities/notification'
import { useSessionStore } from '@/entities/user'
import { tokenStorage } from '@/shared/api'
import { ENV, NOTIFICATION_PRIORITY } from '@/shared/config'
import { safeRelativeUrl } from '@/shared/lib'
import { useConnectionStore } from './connectionStore'

const HUB_PATH = `${ENV.HUB_BASE_URL}/notifications`
/** Delays between SignalR's own reconnect attempts; after these it hands over to `scheduleRetry`. */
const AUTOMATIC_RECONNECT_DELAYS = [0, 2000, 10000, 30000]
const MAX_RETRY_DELAY_MS = 30_000
const SEEN_LIMIT = 200

interface HubOptions {
  /** The user chose «فتح» on a toast: the caller marks it read and navigates. */
  onOpen: (payload: NotificationHubPayload) => void
}

/**
 * Keeps one SignalR connection open for the signed-in user. The push is only an alert: every
 * alert, reconnect and fresh connection invalidates the notification queries, and the database
 * stays the source of truth. The library is imported lazily so it never enters the login chunk.
 */
export function useNotificationHub({ onOpen }: HubOptions): void {
  const userId = useSessionStore((state) => state.user?.userId)
  const queryClient = useQueryClient()
  const setConnectionState = useConnectionStore((store) => store.setState)

  // The effect below must not re-run (and reconnect) just because the callback identity changed.
  const onOpenRef = useRef(onOpen)
  useEffect(() => {
    onOpenRef.current = onOpen
  }, [onOpen])

  useEffect(() => {
    if (!userId) {
      setConnectionState('idle')
      return
    }

    let disposed = false
    let connection: HubConnection | null = null
    let retryTimer: ReturnType<typeof setTimeout> | undefined
    let attempt = 0
    const seen = new Set<string>()

    const refresh = () => {
      void queryClient.invalidateQueries({ queryKey: notificationKeys.all(userId) })
    }

    const remember = (notificationId: string): boolean => {
      if (seen.has(notificationId)) return false
      seen.add(notificationId)
      if (seen.size > SEEN_LIMIT) {
        const oldest = seen.values().next().value
        if (oldest !== undefined) seen.delete(oldest)
      }
      return true
    }

    const announce = (payload: NotificationHubPayload) => {
      if (!remember(payload.notificationId)) return

      const canOpen = safeRelativeUrl(payload.actionUrl) !== null
      toast(payload.titleAr, {
        description: payload.bodyAr,
        duration: payload.priorityId === NOTIFICATION_PRIORITY.Urgent ? Infinity : 6000,
        action: canOpen
          ? { label: 'فتح', onClick: () => onOpenRef.current(payload) }
          : undefined,
      })
    }

    const scheduleRetry = () => {
      if (disposed || !tokenStorage.has()) return
      const delay = Math.min(MAX_RETRY_DELAY_MS, 2000 * 2 ** attempt)
      attempt += 1
      retryTimer = setTimeout(() => void connect(), delay)
    }

    const connect = async (): Promise<void> => {
      if (disposed) return
      setConnectionState(attempt === 0 ? 'connecting' : 'reconnecting')

      const signalR = await import('@microsoft/signalr')
      if (disposed) return

      const next = new signalR.HubConnectionBuilder()
        .withUrl(HUB_PATH, { accessTokenFactory: () => tokenStorage.get() ?? '' })
        .withAutomaticReconnect(AUTOMATIC_RECONNECT_DELAYS)
        .configureLogging(signalR.LogLevel.Warning)
        .build()
      connection = next

      next.on('notificationReceived', (payload: NotificationHubPayload) => {
        refresh()
        announce(payload)
      })
      next.onreconnecting(() => {
        if (!disposed) setConnectionState('reconnecting')
      })
      next.onreconnected(() => {
        if (disposed) return
        setConnectionState('connected')
        refresh()
      })
      next.onclose(() => {
        if (disposed) return
        setConnectionState('disconnected')
        scheduleRetry()
      })

      try {
        await next.start()
        if (disposed) {
          await next.stop()
          return
        }
        attempt = 0
        setConnectionState('connected')
        refresh()
      } catch {
        // `stop()` during negotiation rejects `start()`; that is the normal unmount path.
        if (disposed) return
        setConnectionState('disconnected')
        scheduleRetry()
      }
    }

    void connect()

    return () => {
      disposed = true
      clearTimeout(retryTimer)
      const current = connection
      connection = null
      if (current) void current.stop().catch(() => undefined)
      setConnectionState('idle')
    }
  }, [queryClient, setConnectionState, userId])
}
