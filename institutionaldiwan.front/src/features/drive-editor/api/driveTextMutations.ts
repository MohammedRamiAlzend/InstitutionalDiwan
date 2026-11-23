import { useMutation, useQueryClient } from '@tanstack/react-query'
import { driveItemKeys, type DriveVersionResponse } from '@/entities/drive-item'
import { driveSpaceKeys } from '@/entities/drive-space'
import { httpClient } from '@/shared/api'

export interface SaveDriveTextVariables {
  itemId: string
  content: string
}

/**
 * `PUT /api/drive/files/{itemId}/text` — stores the posted text as a new immutable version.
 * Every save changes the browse grid (size/updated), the version history and the space usage.
 */
export function useSaveDriveTextMutation() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: async ({ itemId, content }: SaveDriveTextVariables): Promise<DriveVersionResponse> => {
      const { data } = await httpClient.put<DriveVersionResponse>(`/drive/files/${itemId}/text`, {
        content,
      })
      return data
    },
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: driveItemKeys.all })
      void queryClient.invalidateQueries({ queryKey: driveSpaceKeys.all })
    },
  })
}
