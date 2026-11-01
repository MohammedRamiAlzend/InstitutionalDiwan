import { parseContentDisposition, saveBlobAsFile } from '@/shared/lib'
import { documentFileApi } from './documentFileApi'
import type { DocumentFileResponse, DocumentFileVersionResponse } from '../model/types'

/**
 * Streams one version to disk and returns the name it was saved under; omit the version for the
 * current one. The `Content-Disposition` wins because it names the bytes actually returned, and only
 * its RFC 5987 form carries an Arabic name undamaged. The stored original name covers a server that
 * omits the header.
 *
 * Failures throw the normalized `ApiError`, including the per-document DOWNLOAD denial (403) that the
 * client can never predict, so callers translate rather than inspect.
 */
export async function downloadFileVersion(
  file: DocumentFileResponse,
  version?: DocumentFileVersionResponse | null,
): Promise<string> {
  const result = await documentFileApi.download(file.fileId, version?.versionId)
  const fileName =
    parseContentDisposition(result.contentDisposition) ?? version?.originalFileName ?? file.title

  saveBlobAsFile(result.blob, fileName)

  return fileName
}
