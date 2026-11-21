import { useQuery, type UseQueryOptions } from '@tanstack/react-query'
import type { PagedResult } from '@/shared/types'
import { driveItemApi } from '../api/driveItemApi'
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
} from './types'

export const driveItemKeys = {
  all: ['drive-items'] as const,
  browse: (params: DriveBrowseParams) => [...driveItemKeys.all, 'browse', params] as const,
  detail: (itemId: string) => [...driveItemKeys.all, 'detail', itemId] as const,
  breadcrumb: (itemId: string) => [...driveItemKeys.all, 'breadcrumb', itemId] as const,
  children: (spaceId: string, parentId?: string | null) =>
    [...driveItemKeys.all, 'children', spaceId, parentId ?? 'root'] as const,
  versions: (itemId: string) => [...driveItemKeys.all, 'versions', itemId] as const,
  downloadUrl: (itemId: string) => [...driveItemKeys.all, 'download-url', itemId] as const,
  editConfig: (itemId: string) => [...driveItemKeys.all, 'edit-config', itemId] as const,
  permissions: (itemId: string) => [...driveItemKeys.all, 'permissions', itemId] as const,
  effective: (itemId: string) => [...driveItemKeys.all, 'effective', itemId] as const,
  deletionRequests: (params: DriveDeletionRequestParams) =>
    [...driveItemKeys.all, 'deletion-requests', params] as const,
  trash: (params: DriveTrashParams) => [...driveItemKeys.all, 'trash', params] as const,
}

type EnabledOptions<T> = Partial<Pick<UseQueryOptions<T>, 'enabled'>>

/** Access-filtered, server-paged browse grid; the whole params object is the cache key. */
export function useDriveItems(
  params: DriveBrowseParams,
  options?: EnabledOptions<PagedResult<DriveItemResponse>>,
) {
  return useQuery({
    queryKey: driveItemKeys.browse(params),
    queryFn: () => driveItemApi.browse(params),
    placeholderData: (previous) => previous,
    staleTime: 30 * 1000,
    ...options,
  })
}

export function useDriveItem(
  itemId: string | null | undefined,
  options?: EnabledOptions<DriveItemResponse>,
) {
  return useQuery({
    queryKey: driveItemKeys.detail(itemId ?? ''),
    queryFn: () => driveItemApi.get(itemId as string),
    placeholderData: (previous) => previous,
    staleTime: 30 * 1000,
    ...options,
    enabled: Boolean(itemId) && (options?.enabled ?? true),
  })
}

export function useDriveBreadcrumb(
  itemId: string | null | undefined,
  options?: EnabledOptions<DriveBreadcrumbNode[]>,
) {
  return useQuery({
    queryKey: driveItemKeys.breadcrumb(itemId ?? ''),
    queryFn: () => driveItemApi.breadcrumb(itemId as string),
    ...options,
    enabled: Boolean(itemId) && (options?.enabled ?? true),
  })
}

/** Direct children of a folder or the space root; the move dialog and tree read this. */
export function useDriveChildren(
  spaceId: string | null | undefined,
  parentId?: string | null,
  options?: EnabledOptions<DriveItemResponse[]>,
) {
  return useQuery({
    queryKey: driveItemKeys.children(spaceId ?? '', parentId),
    queryFn: () => driveItemApi.children(spaceId as string, parentId),
    ...options,
    enabled: Boolean(spaceId) && (options?.enabled ?? true),
  })
}

export function useDriveVersions(
  itemId: string | null | undefined,
  options?: EnabledOptions<DriveVersionResponse[]>,
) {
  return useQuery({
    queryKey: driveItemKeys.versions(itemId ?? ''),
    queryFn: () => driveItemApi.versions(itemId as string),
    ...options,
    enabled: Boolean(itemId) && (options?.enabled ?? true),
  })
}

/** A short-lived presigned GET (5 min) for preview/media/OnlyOffice; never cached across opens. */
export function useDriveDownloadUrl(
  itemId: string | null | undefined,
  options?: EnabledOptions<DriveDownloadUrlResponse>,
) {
  return useQuery({
    queryKey: driveItemKeys.downloadUrl(itemId ?? ''),
    queryFn: () => driveItemApi.downloadUrl(itemId as string),
    staleTime: 0,
    gcTime: 0,
    ...options,
    enabled: Boolean(itemId) && (options?.enabled ?? true),
  })
}

export function useDriveEditConfig(
  itemId: string | null | undefined,
  options?: EnabledOptions<DriveEditConfigResponse>,
) {
  return useQuery({
    queryKey: driveItemKeys.editConfig(itemId ?? ''),
    queryFn: () => driveItemApi.editConfig(itemId as string),
    staleTime: 0,
    gcTime: 0,
    ...options,
    enabled: Boolean(itemId) && (options?.enabled ?? false),
  })
}

/** Explicit grants (Manager-only). Opt-in: enabled while the permissions dialog is open. */
export function useDrivePermissions(
  itemId: string | null | undefined,
  options?: EnabledOptions<DrivePermissionResponse[]>,
) {
  return useQuery({
    queryKey: driveItemKeys.permissions(itemId ?? ''),
    queryFn: () => driveItemApi.permissions(itemId as string),
    ...options,
    enabled: Boolean(itemId) && (options?.enabled ?? false),
  })
}

/** The caller's effective role on an item (0 = no access); gates per-item actions accurately. */
export function useDriveEffectiveRole(
  itemId: string | null | undefined,
  options?: EnabledOptions<DriveEffectiveRoleResponse>,
) {
  return useQuery({
    queryKey: driveItemKeys.effective(itemId ?? ''),
    queryFn: () => driveItemApi.effective(itemId as string),
    staleTime: 30 * 1000,
    ...options,
    enabled: Boolean(itemId) && (options?.enabled ?? true),
  })
}

export function useDriveDeletionRequests(
  params: DriveDeletionRequestParams,
  options?: EnabledOptions<PagedResult<DriveDeletionRequestResponse>>,
) {
  return useQuery({
    queryKey: driveItemKeys.deletionRequests(params),
    queryFn: () => driveItemApi.deletionRequests(params),
    placeholderData: (previous) => previous,
    staleTime: 30 * 1000,
    ...options,
  })
}

export function useDriveTrash(
  params: DriveTrashParams,
  options?: EnabledOptions<PagedResult<DriveItemResponse>>,
) {
  return useQuery({
    queryKey: driveItemKeys.trash(params),
    queryFn: () => driveItemApi.trash(params),
    placeholderData: (previous) => previous,
    staleTime: 30 * 1000,
    ...options,
  })
}