import { useQuery, type UseQueryOptions } from '@tanstack/react-query'
import { documentFileApi } from '../api/documentFileApi'
import type {
  AttachmentChecksumResponse,
  AttachmentEditConfigResponse,
  DocumentFileResponse,
} from './types'

export const documentFileKeys = {
  all: ['document-files'] as const,
  byDocument: (documentId: string) =>
    [...documentFileKeys.all, 'by-document', documentId] as const,
  verify: (fileId: string, versionId?: string | null) =>
    [...documentFileKeys.all, 'verify', fileId, versionId ?? 'current'] as const,
  editConfig: (fileId: string) => [...documentFileKeys.all, 'edit-config', fileId] as const,
}

type EnabledOptions<T> = Partial<Pick<UseQueryOptions<T>, 'enabled'>>

/**
 * Every file of one document (`DOCUMENT_VIEW`): archived files included, files oldest first,
 * and each file's versions newest first. The route is unpaged, so grids filter locally.
 */
export function useDocumentFiles(
  documentId: string | null | undefined,
  options?: EnabledOptions<DocumentFileResponse[]>,
) {
  return useQuery({
    queryKey: documentFileKeys.byDocument(documentId ?? ''),
    queryFn: () => documentFileApi.listByDocument(documentId as string),
    placeholderData: (previous) => previous,
    staleTime: 30 * 1000,
    ...options,
    enabled: Boolean(documentId) && (options?.enabled ?? true),
  })
}

/**
 * Re-computes one version's SHA-256 on the server and compares it with the recorded value.
 * Disabled unless the caller opts in, because it reads the stored bytes and is triggered by an
 * explicit «التحقق من البصمة» action rather than by rendering the grid.
 */
export function useVerifyChecksum(
  params: { fileId: string | null | undefined; versionId?: string | null },
  options?: EnabledOptions<AttachmentChecksumResponse>,
) {
  return useQuery({
    queryKey: documentFileKeys.verify(params.fileId ?? '', params.versionId),
    queryFn: () => documentFileApi.verify(params.fileId as string, params.versionId),
    ...options,
    enabled: Boolean(params.fileId) && (options?.enabled ?? false),
  })
}

export function useAttachmentEditConfig(
  fileId: string | null | undefined,
  options?: EnabledOptions<AttachmentEditConfigResponse>,
) {
  return useQuery({
    queryKey: documentFileKeys.editConfig(fileId ?? ''),
    queryFn: () => documentFileApi.editConfig(fileId as string),
    staleTime: 0,
    gcTime: 0,
    ...options,
    enabled: Boolean(fileId) && (options?.enabled ?? false),
  })
}
