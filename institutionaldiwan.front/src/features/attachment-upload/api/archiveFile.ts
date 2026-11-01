import type { DocumentFileResponse } from '@/entities/document-file'
import { httpClient } from '@/shared/api'

/**
 * `POST /api/document-files/{fileId}/archive` — stops new versions for one file. Its bytes stay
 * readable and downloadable, and the call is idempotent: archiving an archived file returns the same
 * success instead of a conflict.
 */
export async function archiveFile(fileId: string): Promise<DocumentFileResponse> {
  const { data } = await httpClient.post<DocumentFileResponse>(
    `/document-files/${fileId}/archive`,
  )
  return data
}
