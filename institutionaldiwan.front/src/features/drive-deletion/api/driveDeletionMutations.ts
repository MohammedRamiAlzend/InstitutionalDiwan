import { useMutation, useQueryClient } from '@tanstack/react-query'
import { driveItemKeys, type DriveDeletionRequestResponse, type DriveItemResponse } from '@/entities/drive-item'
import { driveSpaceKeys } from '@/entities/drive-space'
import { httpClient } from '@/shared/api'

function useDriveDeletionInvalidation() {
  const queryClient = useQueryClient()
  return () => {
    void queryClient.invalidateQueries({ queryKey: driveItemKeys.all })
    void queryClient.invalidateQueries({ queryKey: driveSpaceKeys.all })
  }
}

export function useRequestDriveDeletionMutation(itemId: string) {
  const invalidate = useDriveDeletionInvalidation()
  return useMutation({
    mutationFn: async (reason: string) =>
      (await httpClient.post<DriveDeletionRequestResponse>(`/drive/items/${itemId}/deletion-requests`, { reason })).data,
    onSuccess: invalidate,
  })
}

export function useCancelDriveDeletionMutation() {
  const invalidate = useDriveDeletionInvalidation()
  return useMutation({
    mutationFn: async (requestId: string) =>
      (await httpClient.post<DriveDeletionRequestResponse>(`/drive/deletion-requests/${requestId}/cancel`)).data,
    onSuccess: invalidate,
  })
}

export function useReviewDriveDeletionMutation(action: 'approve' | 'reject') {
  const invalidate = useDriveDeletionInvalidation()
  return useMutation({
    mutationFn: async ({ requestId, note }: { requestId: string; note?: string }) =>
      (await httpClient.post<DriveDeletionRequestResponse>(
        `/drive/deletion-requests/${requestId}/${action}`,
        { note: note || null },
      )).data,
    onSuccess: invalidate,
  })
}

export function useDirectDeleteDriveItemMutation() {
  const invalidate = useDriveDeletionInvalidation()
  return useMutation({
    mutationFn: async (itemId: string) =>
      (await httpClient.delete<DriveItemResponse>(`/drive/items/${itemId}`)).data,
    onSuccess: invalidate,
  })
}

export function useRestoreDriveTrashMutation() {
  const invalidate = useDriveDeletionInvalidation()
  return useMutation({
    mutationFn: async (itemId: string) =>
      (await httpClient.post<DriveItemResponse>(`/drive/trash/${itemId}/restore`)).data,
    onSuccess: invalidate,
  })
}

export function usePurgeDriveTrashMutation() {
  const invalidate = useDriveDeletionInvalidation()
  return useMutation({
    mutationFn: async (itemId: string) => {
      await httpClient.delete(`/drive/trash/${itemId}`)
    },
    onSuccess: invalidate,
  })
}
