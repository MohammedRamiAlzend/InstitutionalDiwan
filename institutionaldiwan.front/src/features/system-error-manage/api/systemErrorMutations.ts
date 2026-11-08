import { useMutation, useQueryClient } from '@tanstack/react-query'
import { systemErrorKeys, type SystemErrorDetailsResponse, type SystemErrorStatus } from '@/entities/system-error'
import { httpClient } from '@/shared/api'

export function useUpdateSystemErrorMutation(id: string) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: async (request: { status: SystemErrorStatus; notes: string | null }) => {
      const { data } = await httpClient.put<SystemErrorDetailsResponse>(`/system-errors/${id}`, request)
      return data
    },
    onSuccess: () => queryClient.invalidateQueries({ queryKey: systemErrorKeys.all }),
  })
}

export function usePurgeSystemErrorsMutation() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: async (olderThanDays: number) => {
      const { data } = await httpClient.post<number>('/system-errors/purge', { olderThanDays })
      return data
    },
    onSuccess: () => queryClient.invalidateQueries({ queryKey: systemErrorKeys.all }),
  })
}
