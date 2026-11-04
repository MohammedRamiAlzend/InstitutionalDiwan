import { httpClient } from '@/shared/api'
import type { PagedResult } from '@/shared/types'
import type {
  NotificationListParams,
  NotificationResponse,
  UnreadCountResponse,
} from '../model/types'

export const notificationApi = {
  /** `GET /api/notifications` — the caller's own inbox; dismissed rows never appear. */
  async list(params: NotificationListParams): Promise<PagedResult<NotificationResponse>> {
    const { data } = await httpClient.get<PagedResult<NotificationResponse>>('/notifications', {
      params,
    })
    return data
  },

  /** `GET /api/notifications/unread-count` — not audited, safe to poll. */
  async unreadCount(): Promise<UnreadCountResponse> {
    const { data } = await httpClient.get<UnreadCountResponse>('/notifications/unread-count')
    return data
  },
}
