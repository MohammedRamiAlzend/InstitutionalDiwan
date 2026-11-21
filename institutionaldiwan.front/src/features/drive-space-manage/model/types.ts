/** Write payloads for Module 11 Drive spaces; mirror `Server/Contracts/Drive/DriveRequests.cs`. */

/** `POST /api/drive/spaces` — a shared space owned by the caller; quota override needs `DRIVE_MANAGE`. */
export interface CreateDriveSpaceRequest {
  name: string
  quotaBytes?: number | null
}

/** `PUT /api/drive/spaces/{spaceId}` — owner or ADMIN. */
export interface RenameDriveSpaceRequest {
  name: string
}

/** `PUT /api/drive/spaces/{spaceId}/quota` — `null` restores the institution default (`DRIVE_MANAGE`). */
export interface SetDriveSpaceQuotaRequest {
  quotaBytes: number | null
}