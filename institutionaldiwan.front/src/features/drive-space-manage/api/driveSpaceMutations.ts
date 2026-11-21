import { useMutation, useQueryClient } from '@tanstack/react-query'
import { driveSpaceKeys, type DriveSpaceResponse } from '@/entities/drive-space'
import { httpClient } from '@/shared/api'
import type {
  CreateDriveSpaceRequest,
  RenameDriveSpaceRequest,
  SetDriveSpaceQuotaRequest,
} from '../model/types'

/** Every space write changes the overview and the touched space's detail, so refetch the whole slice. */
function useSpaceInvalidation() {
  const queryClient = useQueryClient()
  return () => {
    void queryClient.invalidateQueries({ queryKey: driveSpaceKeys.all })
  }
}

/** `POST /api/drive/spaces/personal` — idempotent; creates «ملفاتي» on first use. */
export function useEnsurePersonalSpaceMutation() {
  const invalidate = useSpaceInvalidation()
  return useMutation({
    mutationFn: async (): Promise<DriveSpaceResponse> => {
      const { data } = await httpClient.post<DriveSpaceResponse>('/drive/spaces/personal')
      return data
    },
    onSuccess: invalidate,
  })
}

/** `POST /api/drive/spaces` — a shared space owned by the caller. */
export function useCreateSpaceMutation() {
  const invalidate = useSpaceInvalidation()
  return useMutation({
    mutationFn: async (request: CreateDriveSpaceRequest): Promise<DriveSpaceResponse> => {
      const { data } = await httpClient.post<DriveSpaceResponse>('/drive/spaces', request)
      return data
    },
    onSuccess: invalidate,
  })
}

/** `PUT /api/drive/spaces/{spaceId}` — rename (owner or ADMIN). */
export function useRenameSpaceMutation(spaceId: string) {
  const invalidate = useSpaceInvalidation()
  return useMutation({
    mutationFn: async (request: RenameDriveSpaceRequest): Promise<DriveSpaceResponse> => {
      const { data } = await httpClient.put<DriveSpaceResponse>(
        `/drive/spaces/${spaceId}`,
        request,
      )
      return data
    },
    onSuccess: invalidate,
  })
}

/** `PUT /api/drive/spaces/{spaceId}/quota` — per-space override (`DRIVE_MANAGE`); null = default. */
export function useSetSpaceQuotaMutation(spaceId: string) {
  const invalidate = useSpaceInvalidation()
  return useMutation({
    mutationFn: async (request: SetDriveSpaceQuotaRequest): Promise<DriveSpaceResponse> => {
      const { data } = await httpClient.put<DriveSpaceResponse>(
        `/drive/spaces/${spaceId}/quota`,
        request,
      )
      return data
    },
    onSuccess: invalidate,
  })
}