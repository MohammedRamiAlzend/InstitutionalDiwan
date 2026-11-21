import { useQuery, type UseQueryOptions } from '@tanstack/react-query'
import { driveSpaceApi } from '../api/driveSpaceApi'
import type {
  DriveSettingsResponse,
  DriveSpaceResponse,
  DriveSpacesOverviewResponse,
} from './types'

export const driveSpaceKeys = {
  all: ['drive-spaces'] as const,
  overview: () => [...driveSpaceKeys.all, 'overview'] as const,
  detail: (spaceId: string) => [...driveSpaceKeys.all, 'detail', spaceId] as const,
  settings: () => [...driveSpaceKeys.all, 'settings'] as const,
}

type EnabledOptions<T> = Partial<Pick<UseQueryOptions<T>, 'enabled'>>

/** The caller's personal space plus reachable shared spaces; the sidebar and space switcher read this. */
export function useDriveSpaces(options?: EnabledOptions<DriveSpacesOverviewResponse>) {
  return useQuery({
    queryKey: driveSpaceKeys.overview(),
    queryFn: () => driveSpaceApi.overview(),
    placeholderData: (previous) => previous,
    staleTime: 30 * 1000,
    ...options,
  })
}

/** One space (quota meter, space header). Disabled until an id is present. */
export function useDriveSpace(
  spaceId: string | null | undefined,
  options?: EnabledOptions<DriveSpaceResponse>,
) {
  return useQuery({
    queryKey: driveSpaceKeys.detail(spaceId ?? ''),
    queryFn: () => driveSpaceApi.get(spaceId as string),
    placeholderData: (previous) => previous,
    staleTime: 30 * 1000,
    ...options,
    enabled: Boolean(spaceId) && (options?.enabled ?? true),
  })
}

/**
 * Institution Drive defaults. `DRIVE_MANAGE`-only, so disabled unless an admin surface opts in;
 * a normal user pre-checks against the `driveLimits` mirror instead of reading this.
 */
export function useDriveSettings(options?: EnabledOptions<DriveSettingsResponse>) {
  return useQuery({
    queryKey: driveSpaceKeys.settings(),
    queryFn: () => driveSpaceApi.settings(),
    staleTime: 60 * 1000,
    ...options,
    enabled: options?.enabled ?? false,
  })
}