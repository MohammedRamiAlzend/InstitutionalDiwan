import { httpClient } from '@/shared/api'
import type { MovementQueueItemResponse, MovementResponse } from '../model/types'

function queueParams(departmentId?: string) {
  return departmentId ? { departmentId } : undefined
}

/** Typed reads for movement detail, a document's routing history, and department queues. */
export const movementApi = {
  byId: async (movementId: string): Promise<MovementResponse> => {
    const { data } = await httpClient.get<MovementResponse>(`/movements/${movementId}`)
    return data
  },

  byDocument: async (documentId: string): Promise<MovementResponse[]> => {
    const { data } = await httpClient.get<MovementResponse[]>(
      `/movements/by-document/${documentId}`,
    )
    return data
  },

  inbox: async (departmentId?: string): Promise<MovementQueueItemResponse[]> => {
    const { data } = await httpClient.get<MovementQueueItemResponse[]>('/movements/inbox', {
      params: queueParams(departmentId),
    })
    return data
  },

  outbox: async (departmentId?: string): Promise<MovementQueueItemResponse[]> => {
    const { data } = await httpClient.get<MovementQueueItemResponse[]>('/movements/outbox', {
      params: queueParams(departmentId),
    })
    return data
  },
}
