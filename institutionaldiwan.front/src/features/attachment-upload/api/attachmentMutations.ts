import { useMutation, useQueryClient } from '@tanstack/react-query'
import { documentFileKeys, type DocumentFileResponse } from '@/entities/document-file'
import { archiveFile } from './archiveFile'
import { uploadInitial, type UploadInitialInput } from './uploadInitial'
import { uploadVersion, type UploadVersionInput } from './uploadVersion'

/** The document's own file list is the only thing a write changes, so it is all that is refetched. */
function useInvalidateDocumentFiles() {
  const queryClient = useQueryClient()

  return (documentId: string) =>
    queryClient.invalidateQueries({ queryKey: documentFileKeys.byDocument(documentId) })
}

/**
 * Atomic first-version upload. Only the document's own file list is invalidated: the grid refetches
 * and the new file appears with its version, checksum, and size already resolved.
 */
export function useUploadInitialMutation(documentId: string) {
  const invalidateDocumentFiles = useInvalidateDocumentFiles()

  return useMutation({
    mutationFn: async (
      input: Omit<UploadInitialInput, 'documentId'>,
    ): Promise<DocumentFileResponse> => uploadInitial({ ...input, documentId }),
    onSuccess: (created) => {
      invalidateDocumentFiles(created.documentId)
    },
  })
}

/**
 * Next-version upload. The response carries the whole file with its version history, so the caller
 * can show the new version number without a refetch; the grid is invalidated for the same reason.
 */
export function useUploadVersionMutation(fileId: string) {
  const invalidateDocumentFiles = useInvalidateDocumentFiles()

  return useMutation({
    mutationFn: async (
      input: Omit<UploadVersionInput, 'fileId'>,
    ): Promise<DocumentFileResponse> => uploadVersion({ ...input, fileId }),
    onSuccess: (updated) => {
      invalidateDocumentFiles(updated.documentId)
    },
  })
}

/** Archiving is reversible only by a later module: there is no un-archive endpoint. */
export function useArchiveFileMutation() {
  const invalidateDocumentFiles = useInvalidateDocumentFiles()

  return useMutation({
    mutationFn: (fileId: string): Promise<DocumentFileResponse> => archiveFile(fileId),
    onSuccess: (archived) => {
      invalidateDocumentFiles(archived.documentId)
    },
  })
}
