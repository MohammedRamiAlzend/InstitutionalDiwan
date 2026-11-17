import { useQuery } from '@tanstack/react-query'
import { uiLabelApi } from '../api/uiLabelApi'

export const uiLabelKeys = {
  all: ['ui-labels'] as const,
  overrides: () => [...uiLabelKeys.all, 'overrides'] as const,
}

/**
 * The system-wide label overrides. `LabelsBootGate` owns applying the result to the
 * labels store; a write feature (Phase 2) invalidates `uiLabelKeys.overrides()` to refresh it.
 */
export function useUiLabelsQuery(options?: { enabled?: boolean }) {
  return useQuery({
    queryKey: uiLabelKeys.overrides(),
    queryFn: uiLabelApi.get,
    staleTime: 5 * 60 * 1000,
    retry: false,
    ...options,
  })
}