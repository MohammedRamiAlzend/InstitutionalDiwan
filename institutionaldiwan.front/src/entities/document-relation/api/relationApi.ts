import { httpClient } from '@/shared/api'
import type {
  DocumentRelationsResponse,
  RelationEventsPage,
  RelationGraphResponse,
} from '../model/types'

export const relationApi = {
  /** `GET /api/documents/{id}/relations` — links the caller may see, plus a hidden-link count. */
  async list(documentId: string): Promise<DocumentRelationsResponse> {
    const { data } = await httpClient.get<DocumentRelationsResponse>(
      `/documents/${documentId}/relations`,
    )
    return data
  },

  /** `GET /api/documents/{id}/relations/graph?depth=` — depth 1–3, at most 100 nodes. */
  async graph(documentId: string, depth: number): Promise<RelationGraphResponse> {
    const { data } = await httpClient.get<RelationGraphResponse>(
      `/documents/${documentId}/relations/graph`,
      { params: { depth } },
    )
    return data
  },

  /** `GET /api/documents/{id}/relations/events` — newest first, including removed links. */
  async events(documentId: string, page: number, pageSize: number): Promise<RelationEventsPage> {
    const { data } = await httpClient.get<RelationEventsPage>(
      `/documents/${documentId}/relations/events`,
      { params: { page, pageSize } },
    )
    return data
  },
}
