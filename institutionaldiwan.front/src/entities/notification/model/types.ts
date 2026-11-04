/** One inbox row — mirrors `NotificationResponse` (`GET /api/notifications`). */
export interface NotificationResponse {
  /** Per-user row: use this id for read and dismiss, never `notificationId`. */
  recipientId: string
  notificationId: string
  typeId: number
  titleAr: string
  bodyAr: string
  priorityId: number
  relatedDocumentId: string | null
  relatedMovementId: string | null
  /** Relative in-app path, validated by the server; still pass it through `safeRelativeUrl`. */
  actionUrl: string | null
  isRead: boolean
  readAt: string | null
  createdAt: string
}

export interface UnreadTypeCount {
  typeId: number
  count: number
}

/** `GET /api/notifications/unread-count`. */
export interface UnreadCountResponse {
  count: number
  byType: UnreadTypeCount[]
}

export type NotificationSortKey = 'deliveredAt' | 'priority' | 'type' | 'isRead'

export interface NotificationListParams {
  page?: number
  pageSize?: number
  unreadOnly?: boolean
  typeId?: number
  search?: string
  sortBy?: NotificationSortKey
  sortDescending?: boolean
}

/** Payload of the SignalR `notificationReceived` push — an alert only; the database is the source. */
export interface NotificationHubPayload {
  recipientId: string
  notificationId: string
  typeId: number
  titleAr: string
  bodyAr: string
  priorityId: number
  actionUrl: string | null
  createdAt: string
}
