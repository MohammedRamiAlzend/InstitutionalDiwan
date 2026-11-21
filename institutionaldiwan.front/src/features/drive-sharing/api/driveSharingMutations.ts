import { useMutation, useQueryClient } from '@tanstack/react-query'
import { driveItemKeys, type DriveItemResponse, type DrivePermissionResponse } from '@/entities/drive-item'
import { httpClient } from '@/shared/api'

function useDrivePermissionInvalidation() {
  const queryClient = useQueryClient()
  return () => {
    void queryClient.invalidateQueries({ queryKey: driveItemKeys.all })
  }
}

export function useGrantDrivePermissionMutation(itemId: string) {
  const invalidate = useDrivePermissionInvalidation()
  return useMutation({
    mutationFn: async (request: { principalTypeId: number; principalId: string; roleId: number }) =>
      (await httpClient.post<DrivePermissionResponse>(`/drive/items/${itemId}/permissions`, request)).data,
    onSuccess: invalidate,
  })
}

export function useRevokeDrivePermissionMutation(itemId: string) {
  const invalidate = useDrivePermissionInvalidation()
  return useMutation({
    mutationFn: async (permissionId: string) => {
      await httpClient.delete(`/drive/items/${itemId}/permissions/${permissionId}`)
    },
    onSuccess: invalidate,
  })
}

export function useSetDriveInheritanceMutation(itemId: string) {
  const invalidate = useDrivePermissionInvalidation()
  return useMutation({
    mutationFn: async (breakInheritance: boolean) =>
      (await httpClient.put<DriveItemResponse>(`/drive/items/${itemId}/inheritance`, { breakInheritance })).data,
    onSuccess: invalidate,
  })
}
