import { useMutation, useQueryClient } from '@tanstack/react-query'
import { driveItemKeys, type DriveVersionResponse } from '@/entities/drive-item'
import { driveSpaceKeys } from '@/entities/drive-space'
import { putPresigned } from '@/shared/api'
import { sha256Hex } from '@/shared/lib'
import { driveUploadApi } from './driveUploadApi'
import type { DriveUploadTarget } from '../model/types'

export type UploadStage = 'preparing' | 'uploading' | 'verifying'

/** A failed direct-to-storage PUT; the gateway answers S3 XML, so the feature owns the wording. */
export class DriveUploadPutError extends Error {
  constructor() {
    super('Drive upload PUT failed')
    this.name = 'DriveUploadPutError'
  }
}

export interface UploadDriveFileVariables {
  target: DriveUploadTarget
  file: File
  onStage?: (stage: UploadStage) => void
  onProgress?: (percent: number) => void
}

/** A committed upload changes the browse grid, the file's versions and the space usage counter. */
function useUploadInvalidation() {
  const queryClient = useQueryClient()
  return () => {
    void queryClient.invalidateQueries({ queryKey: driveItemKeys.all })
    void queryClient.invalidateQueries({ queryKey: driveSpaceKeys.all })
  }
}

/**
 * One file's full presigned flow: initiate → direct PUT → complete, abandoning the reservation if
 * any step after initiate fails. The local SHA-256 is computed up front and sent to `complete` so the
 * server verifies the transfer as well as the storage. The `Content-Type` sent on the PUT must equal
 * the `mimeType` signed at initiate (the presigned signature covers that header).
 */
export function useUploadDriveFileMutation() {
  const invalidate = useUploadInvalidation()
  return useMutation({
    mutationFn: async ({
      target,
      file,
      onStage,
      onProgress,
    }: UploadDriveFileVariables): Promise<DriveVersionResponse> => {
      onStage?.('preparing')
      const contentType = file.type || 'application/octet-stream'

      let checksum: string | null
      try {
        checksum = await sha256Hex(file)
      } catch {
        checksum = null
      }

      const initiated = await driveUploadApi.initiate({
        spaceId: target.spaceId,
        parentId: target.parentId ?? null,
        fileName: target.fileName ?? file.name,
        declaredSizeBytes: file.size,
        mimeType: contentType,
        fileItemId: target.fileItemId ?? null,
      })

      try {
        onStage?.('uploading')
        try {
          await putPresigned(initiated.uploadUrl, file, { contentType, onProgress })
        } catch {
          throw new DriveUploadPutError()
        }

        onStage?.('verifying')
        return await driveUploadApi.complete(initiated.versionId, { checksumSha256: checksum })
      } catch (error) {
        await driveUploadApi.abandon(initiated.versionId).catch(() => undefined)
        throw error
      }
    },
    onSuccess: invalidate,
  })
}