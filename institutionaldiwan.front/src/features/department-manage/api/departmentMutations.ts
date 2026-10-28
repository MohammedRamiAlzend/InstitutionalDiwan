import { useMutation, useQueryClient } from '@tanstack/react-query'
import { httpClient } from '@/shared/api'
import { departmentKeys, type DepartmentResponse } from '@/entities/department'
import type { CreateDepartmentRequest, UpdateDepartmentRequest } from '../model/types'

/** `POST /api/departments` — creates a department under an optional parent. */
export function useCreateDepartmentMutation() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async (
      request: CreateDepartmentRequest,
    ): Promise<DepartmentResponse> => {
      const { data } = await httpClient.post<DepartmentResponse>(
        '/departments',
        request,
      )
      return data
    },
    onSuccess: () => void queryClient.invalidateQueries({ queryKey: departmentKeys.all }),
  })
}

/** `PUT /api/departments/{id}` — updates names, parent, manager, policy, and notes. */
export function useUpdateDepartmentMutation() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async ({
      departmentId,
      request,
    }: {
      departmentId: string
      request: UpdateDepartmentRequest
    }): Promise<DepartmentResponse> => {
      const { data } = await httpClient.put<DepartmentResponse>(
        `/departments/${departmentId}`,
        request,
      )
      return data
    },
    onSuccess: () => void queryClient.invalidateQueries({ queryKey: departmentKeys.all }),
  })
}

/** `DELETE /api/departments/{id}` (deactivate) and `POST /{id}/activate`. */
export function useSetDepartmentActiveMutation() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async ({
      departmentId,
      active,
    }: {
      departmentId: string
      active: boolean
    }): Promise<DepartmentResponse> => {
      const { data } = active
        ? await httpClient.post<DepartmentResponse>(
            `/departments/${departmentId}/activate`,
          )
        : await httpClient.delete<DepartmentResponse>(`/departments/${departmentId}`)
      return data
    },
    onSuccess: () => void queryClient.invalidateQueries({ queryKey: departmentKeys.all }),
  })
}
