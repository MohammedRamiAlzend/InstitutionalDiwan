import { httpClient } from '@/shared/api'
import type { PagedResult } from '@/shared/types'
import type {
  DocumentAccessGrantResponse,
  DocumentDetailResponse,
  DocumentListParams,
  DocumentResponse,
  DocumentTimelineEntryResponse,
} from '../model/types'

/** Typed read calls for the correspondence registry and its per-document views. */
export const documentApi = {
  list: async (params: DocumentListParams): Promise<PagedResult<DocumentResponse>> => {
    const { data } = await httpClient.get<PagedResult<DocumentResponse>>('/documents', {
      params: {
        page: params.page,
        pageSize: params.pageSize,
        search: params.search?.trim() || undefined,
        sortBy: params.sortBy,
        sortDescending: params.sortDescending,
        statusId: params.statusId,
        owningDepartmentId: params.owningDepartmentId || undefined,
        directionId: params.directionId,
        documentTypeId: params.documentTypeId,
      },
    })
    return data
  },

  byId: async (documentId: string): Promise<DocumentResponse> => {
    const { data } = await httpClient.get<DocumentResponse>(`/documents/${documentId}`)
    return data
  },

  details: async (documentId: string): Promise<DocumentDetailResponse> => {
    const { data } = await httpClient.get<DocumentDetailResponse>(
      `/documents/${documentId}/details`,
    )
    return data
  },

  timeline: async (documentId: string): Promise<DocumentTimelineEntryResponse[]> => {
    const { data } = await httpClient.get<DocumentTimelineEntryResponse[]>(
      `/documents/${documentId}/timeline`,
    )
    return data
  },

  accessGrants: async (documentId: string): Promise<DocumentAccessGrantResponse[]> => {
    const { data } = await httpClient.get<DocumentAccessGrantResponse[]>(
      `/documents/${documentId}/access-grants`,
    )
    return data
  },
}
