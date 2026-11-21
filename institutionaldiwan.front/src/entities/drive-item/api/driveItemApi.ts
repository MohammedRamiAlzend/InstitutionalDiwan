import { downloadBlob, httpClient, type BlobDownload } from '@/shared/api'
import type { PagedResult } from '@/shared/types'
import type {
  DriveBreadcrumbNode,
  DriveBrowseParams,
  DriveDeletionRequestParams,
  DriveDeletionRequestResponse,
  DriveDownloadUrlResponse,
  DriveEditConfigResponse,
  DriveEffectiveRoleResponse,
  DriveItemResponse,
  DrivePermissionResponse,
  DriveTrashParams,
  DriveVersionResponse,
} from '../model/types'

/**
 * Typed reads for Module 11 Drive items, versions, permissions, deletion requests and trash. Writes
 * live in the `drive-*` features. Browse/trash/deletion-requests are server-paged (`PagedResult`);
 * children/versions/permissions/breadcrumb return plain arrays. Routes are relative to `/api`.
 */
export const driveItemApi = {
  browse: async (params: DriveBrowseParams): Promise<PagedResult<DriveItemResponse>> => {
    const { data } = await httpClient.get<PagedResult<DriveItemResponse>>('/drive/items', { params })
    return data
  },

  get: async (itemId: string): Promise<DriveItemResponse> => {
    const { data } = await httpClient.get<DriveItemResponse>(`/drive/items/${itemId}`)
    return data
  },

  breadcrumb: async (itemId: string): Promise<DriveBreadcrumbNode[]> => {
    const { data } = await httpClient.get<DriveBreadcrumbNode[]>(`/drive/items/${itemId}/breadcrumb`)
    return data
  },

  /** Direct children of a folder (or the space root when `parentId` is null); folders first, unpaged. */
  children: async (spaceId: string, parentId?: string | null): Promise<DriveItemResponse[]> => {
    const { data } = await httpClient.get<DriveItemResponse[]>('/drive/items/children', {
      params: { spaceId, parentId: parentId || undefined },
    })
    return data
  },

  versions: async (itemId: string): Promise<DriveVersionResponse[]> => {
    const { data } = await httpClient.get<DriveVersionResponse[]>(`/drive/files/${itemId}/versions`)
    return data
  },

  /** Explicit grants only (Manager); inherited access is computed, not listed. */
  permissions: async (itemId: string): Promise<DrivePermissionResponse[]> => {
    const { data } = await httpClient.get<DrivePermissionResponse[]>(`/drive/items/${itemId}/permissions`)
    return data
  },

  /** The caller's effective role (0 = no access). Needs no Viewer role, so it is safe to gate UI with. */
  effective: async (itemId: string): Promise<DriveEffectiveRoleResponse> => {
    const { data } = await httpClient.get<DriveEffectiveRoleResponse>(`/drive/items/${itemId}/effective`)
    return data
  },

  deletionRequests: async (
    params: DriveDeletionRequestParams,
  ): Promise<PagedResult<DriveDeletionRequestResponse>> => {
    const { data } = await httpClient.get<PagedResult<DriveDeletionRequestResponse>>(
      '/drive/deletion-requests',
      { params },
    )
    return data
  },

  trash: async (params: DriveTrashParams): Promise<PagedResult<DriveItemResponse>> => {
    const { data } = await httpClient.get<PagedResult<DriveItemResponse>>('/drive/trash', { params })
    return data
  },

  /** A short-lived presigned GET (5 min) for direct download, media preview, or the OnlyOffice server. */
  downloadUrl: async (
    itemId: string,
    versionId?: string | null,
  ): Promise<DriveDownloadUrlResponse> => {
    const { data } = await httpClient.get<DriveDownloadUrlResponse>(
      `/drive/files/${itemId}/download-url`,
      { params: { versionId: versionId || undefined } },
    )
    return data
  },

  editConfig: async (itemId: string): Promise<DriveEditConfigResponse> => {
    const { data } = await httpClient.get<DriveEditConfigResponse>(`/drive/files/${itemId}/edit-config`)
    return data
  },

  /**
   * The current (or a named) version's bytes through the authenticated route, which supports HTTP
   * Range. `disposition: 'attachment'` forces a download; the default is inline. A failure arrives as
   * a blob-bodied ProblemDetails and is normalized to an `ApiError` with the domain code.
   */
  content: (
    itemId: string,
    versionId?: string | null,
    disposition?: 'inline' | 'attachment',
  ): Promise<BlobDownload> =>
    downloadBlob(`/drive/files/${itemId}/content`, {
      versionId: versionId || undefined,
      disposition: disposition || undefined,
    }),

  /** A named version's bytes (route-based version id), with the same Range/disposition behavior. */
  versionContent: (
    itemId: string,
    versionId: string,
    disposition?: 'inline' | 'attachment',
  ): Promise<BlobDownload> =>
    downloadBlob(`/drive/files/${itemId}/versions/${versionId}/content`, {
      disposition: disposition || undefined,
    }),
}