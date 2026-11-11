import { useQuery, type UseQueryOptions } from '@tanstack/react-query'
import { decisionTypeApi } from '../api/decisionTypeApi'
import type { DecisionTypeListParams, DecisionTypeResponse } from './types'

export const decisionTypeKeys = {
  all: ['decision-types'] as const,
  list: (params: DecisionTypeListParams) => [...decisionTypeKeys.all, params] as const,
}

type EnabledOptions<T> = Partial<Pick<UseQueryOptions<T>, 'enabled'>>

export function useDecisionTypes(
  params: DecisionTypeListParams = {},
  options?: EnabledOptions<DecisionTypeResponse[]>,
) {
  return useQuery({
    queryKey: decisionTypeKeys.list(params),
    queryFn: () => decisionTypeApi.list(params),
    staleTime: 30 * 1000,
    ...options,
  })
}
