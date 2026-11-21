/** `DriveSpaceResponse` — one file space with its effective quota and current usage. */
export interface DriveSpaceResponse {
  spaceId: string
  name: string
  /** `DRIVE_SPACE_TYPE`: 1 personal, 2 shared. */
  spaceTypeId: number
  ownerUserId: string
  /** Per-space override; `null` means the institution default applies. */
  quotaBytes: number | null
  /** The quota actually in force (override ?? institution default). */
  effectiveQuotaBytes: number
  /** Committed bytes across all versions; trash keeps counting until permanent purge. */
  usedBytes: number
  createdAt: string
  createdByUserId: string
  updatedAt: string | null
  trashedAt: string | null
}

/** `DriveSpacesOverviewResponse` — the caller's personal space plus the shared spaces they can reach. */
export interface DriveSpacesOverviewResponse {
  personal: DriveSpaceResponse | null
  shared: DriveSpaceResponse[]
}

/** `DriveSettingsResponse` — institution Drive defaults (`DRIVE_MANAGE` read). */
export interface DriveSettingsResponse {
  defaultQuotaBytes: number
  maxFileSizeBytes: number
  allowedExtensionsCsv: string
}