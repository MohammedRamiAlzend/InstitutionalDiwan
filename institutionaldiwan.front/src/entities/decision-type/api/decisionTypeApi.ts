import { httpClient } from '@/shared/api'
import type { DecisionTypeListParams, DecisionTypeResponse } from '../model/types'

export const decisionTypeApi = {
  list: async (params: DecisionTypeListParams = {}): Promise<DecisionTypeResponse[]> => {
    const { data } = await httpClient.get<DecisionTypeResponse[]>('/decision-types', {
      params: {
        documentTypeId: params.documentTypeId,
        isActive: params.isActive,
      },
    })
    return data
  },
}
