import { driveItemApi, type DriveItemResponse } from '@/entities/drive-item'
import { parseContentDisposition, saveBlobAsFile } from '@/shared/lib'

/**
 * Downloads a file (or a named version) through the authenticated Range-capable route and saves it
 * under the server's own `Content-Disposition` name — which carries Arabic undamaged via RFC 5987 —
 * falling back to the item name. A failure arrives as an `ApiError` with the real `Drive.*` code.
 */
export async function downloadDriveFile(
  item: DriveItemResponse,
  versionId?: string | null,
): Promise<void> {
  const { blob, contentDisposition } = versionId
    ? await driveItemApi.versionContent(item.itemId, versionId, 'attachment')
    : await driveItemApi.content(item.itemId, null, 'attachment')

  const fileName = parseContentDisposition(contentDisposition) ?? item.name
  saveBlobAsFile(blob, fileName)
}