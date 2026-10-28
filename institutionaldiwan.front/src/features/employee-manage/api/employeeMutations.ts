import { useMutation, useQueryClient, type QueryClient } from '@tanstack/react-query'
import { httpClient } from '@/shared/api'
import { employeeKeys, type EmployeeResponse } from '@/entities/employee'
import type { CreateEmployeeRequest, UpdateEmployeeRequest } from '../model/types'

function invalidateEmployees(queryClient: QueryClient, employeeId?: string) {
  void queryClient.invalidateQueries({ queryKey: employeeKeys.lists() })
  if (employeeId) {
    void queryClient.invalidateQueries({ queryKey: employeeKeys.detail(employeeId) })
  }
}

/** `POST /api/employees` — creates an employee attached to a department. */
export function useCreateEmployeeMutation() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async (
      request: CreateEmployeeRequest,
    ): Promise<EmployeeResponse> => {
      const { data } = await httpClient.post<EmployeeResponse>('/employees', request)
      return data
    },
    onSuccess: () => invalidateEmployees(queryClient),
  })
}

/** `PUT /api/employees/{id}` — updates details and department. */
export function useUpdateEmployeeMutation() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async ({
      employeeId,
      request,
    }: {
      employeeId: string
      request: UpdateEmployeeRequest
    }): Promise<EmployeeResponse> => {
      const { data } = await httpClient.put<EmployeeResponse>(
        `/employees/${employeeId}`,
        request,
      )
      return data
    },
    onSuccess: (_data, variables) =>
      invalidateEmployees(queryClient, variables.employeeId),
  })
}

/** `DELETE /api/employees/{id}` (deactivate) and `POST /{id}/activate`. */
export function useSetEmployeeActiveMutation() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async ({
      employeeId,
      active,
    }: {
      employeeId: string
      active: boolean
    }): Promise<EmployeeResponse> => {
      const { data } = active
        ? await httpClient.post<EmployeeResponse>(`/employees/${employeeId}/activate`)
        : await httpClient.delete<EmployeeResponse>(`/employees/${employeeId}`)
      return data
    },
    onSuccess: (_data, variables) =>
      invalidateEmployees(queryClient, variables.employeeId),
  })
}
