import { downloadBlob, httpClient, type BlobDownload } from '@/shared/api'
import type { AttachmentChecksumResponse, DocumentFileResponse } from '../model/types'

/**
 * Typed reads for Module 06 document files. Writes live in `features/attachment-upload`.
 * The document-scoped list route is preferred over `/document-files/by-document/{id}`,
 * which returns the identical payload.
 */
export const documentFileApi = {
  listByDocument: async (documentId: string): Promise<DocumentFileResponse[]> => {
    const { data } = await httpClient.get<DocumentFileResponse[]>(
      `/documents/${documentId}/files`,
    )
    return data
  },

  /**
   * One version's bytes plus the `Content-Disposition` carrying the original name; omit the version
   * for the current one. Download needs the per-document DOWNLOAD access level, which the client is
   * never told, so a `403` here is a real denial to translate — not something to gate up front.
   */
  download: (fileId: string, versionId?: string | null): Promise<BlobDownload> =>
    downloadBlob(`/document-files/${fileId}/download`, { versionId: versionId || undefined }),

  verify: async (
    fileId: string,
    versionId?: string | null,
  ): Promise<AttachmentChecksumResponse> => {
    const { data } = await httpClient.get<AttachmentChecksumResponse>(
      `/document-files/${fileId}/verify`,
      { params: { versionId: versionId || undefined } },
    )
    return data
  },
}
