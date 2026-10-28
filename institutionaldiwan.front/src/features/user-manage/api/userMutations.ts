import { useMutation, useQueryClient, type QueryClient } from '@tanstack/react-query'
import { httpClient } from '@/shared/api'
import { userKeys, type UserResponse } from '@/entities/user'
import type {
  AssignDepartmentScopeRequest,
  CreateUserRequest,
  ResetUserPasswordRequest,
  UpdateUserRequest,
} from '../model/types'

function invalidateUsers(queryClient: QueryClient, userId?: string) {
  void queryClient.invalidateQueries({ queryKey: userKeys.lists() })
  if (userId) {
    void queryClient.invalidateQueries({ queryKey: userKeys.detail(userId) })
    void queryClient.invalidateQueries({
      queryKey: userKeys.effectivePermissions(userId),
    })
  }
}

/** `POST /api/users` — creates an account; the server hashes the password. */
export function useCreateUserMutation() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async (request: CreateUserRequest): Promise<UserResponse> => {
      const { data } = await httpClient.post<UserResponse>('/users', request)
      return data
    },
    onSuccess: () => invalidateUsers(queryClient),
  })
}

/** `PUT /api/users/{id}` — department, employee link, scope, password flag. */
export function useUpdateUserMutation() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async ({
      userId,
      request,
    }: {
      userId: string
      request: UpdateUserRequest
    }): Promise<UserResponse> => {
      const { data } = await httpClient.put<UserResponse>(`/users/${userId}`, request)
      return data
    },
    onSuccess: (_data, variables) => invalidateUsers(queryClient, variables.userId),
  })
}

/** `POST /api/users/{id}/reset-password` — temporary password, forced change at next sign-in. */
export function useResetUserPasswordMutation() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async ({
      userId,
      request,
    }: {
      userId: string
      request: ResetUserPasswordRequest
    }): Promise<UserResponse> => {
      const { data } = await httpClient.post<UserResponse>(
        `/users/${userId}/reset-password`,
        request,
      )
      return data
    },
    onSuccess: (_data, variables) => invalidateUsers(queryClient, variables.userId),
  })
}

/** `DELETE /api/users/{id}` (deactivate) and `POST /api/users/{id}/activate`. */
export function useSetUserActiveMutation() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async ({
      userId,
      active,
    }: {
      userId: string
      active: boolean
    }): Promise<UserResponse> => {
      const { data } = active
        ? await httpClient.post<UserResponse>(`/users/${userId}/activate`)
        : await httpClient.delete<UserResponse>(`/users/${userId}`)
      return data
    },
    onSuccess: (_data, variables) => invalidateUsers(queryClient, variables.userId),
  })
}

/** `POST /api/users/{id}/roles` — grants a role to the account. */
export function useAssignUserRoleMutation() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async ({
      userId,
      roleId,
    }: {
      userId: string
      roleId: string
    }): Promise<UserResponse> => {
      const { data } = await httpClient.post<UserResponse>(`/users/${userId}/roles`, {
        roleId,
      })
      return data
    },
    onSuccess: (_data, variables) => invalidateUsers(queryClient, variables.userId),
  })
}

/** `DELETE /api/users/{id}/roles/{roleId}` — revokes a role. */
export function useRevokeUserRoleMutation() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async ({
      userId,
      roleId,
    }: {
      userId: string
      roleId: string
    }): Promise<UserResponse> => {
      const { data } = await httpClient.delete<UserResponse>(
        `/users/${userId}/roles/${roleId}`,
      )
      return data
    },
    onSuccess: (_data, variables) => invalidateUsers(queryClient, variables.userId),
  })
}

/** `POST /api/users/{id}/department-assignments` — widens the org scope. */
export function useAssignDepartmentScopeMutation() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async ({
      userId,
      request,
    }: {
      userId: string
      request: AssignDepartmentScopeRequest
    }): Promise<UserResponse> => {
      const { data } = await httpClient.post<UserResponse>(
        `/users/${userId}/department-assignments`,
        request,
      )
      return data
    },
    onSuccess: (_data, variables) => invalidateUsers(queryClient, variables.userId),
  })
}

/** `DELETE /api/users/{id}/department-assignments/{assignmentId}` — ends an assignment. */
export function useEndDepartmentScopeMutation() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async ({
      userId,
      assignmentId,
    }: {
      userId: string
      assignmentId: string
    }): Promise<UserResponse> => {
      const { data } = await httpClient.delete<UserResponse>(
        `/users/${userId}/department-assignments/${assignmentId}`,
      )
      return data
    },
    onSuccess: (_data, variables) => invalidateUsers(queryClient, variables.userId),
  })
}
