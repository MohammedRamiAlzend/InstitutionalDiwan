import { httpClient } from '@/shared/api'
import type { PagedResult } from '@/shared/types'
import type {
  DecisionRegisterItemResponse,
  DecisionRegisterParams,
  DocumentDecisionResponse,
  JoinCandidateResponse,
  NextActionResponse,
} from '../model/types'

export const documentDecisionApi = {
  listByDocument: async (documentId: string): Promise<DocumentDecisionResponse[]> => {
    const { data } = await httpClient.get<DocumentDecisionResponse[]>(
      `/documents/${documentId}/decisions`,
    )
    return data
  },

  nextAction: async (documentId: string): Promise<NextActionResponse> => {
    const { data } = await httpClient.get<NextActionResponse>(
      `/documents/${documentId}/next-action`,
    )
    return data
  },

  joinCandidates: async (documentId: string): Promise<JoinCandidateResponse[]> => {
    const { data } = await httpClient.get<JoinCandidateResponse[]>(
      `/documents/${documentId}/join-candidates`,
    )
    return data
  },

  register: async (
    params: DecisionRegisterParams,
  ): Promise<PagedResult<DecisionRegisterItemResponse>> => {
    const { data } = await httpClient.get<PagedResult<DecisionRegisterItemResponse>>('/decisions', {
      params: {
        page: params.page,
        pageSize: params.pageSize,
        search: params.search?.trim() || undefined,
        sortBy: params.sortBy,
        sortDescending: params.sortDescending,
        decisionTypeId: params.decisionTypeId || undefined,
        statusId: params.statusId,
        issuingDepartmentId: params.issuingDepartmentId || undefined,
        year: params.year,
      },
    })
    return data
  },
}
