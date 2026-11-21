# `src/features/drive-upload` — Drive File Transfer (Module 11)

> **Status:** ✅ Implemented (2026-11-20, front-end Phase 3) — presigned direct-to-storage upload
> (initiate → PUT → complete, abandon on failure), a sequential multi-file queue dialog with
> three-stage progress and local SHA-256 verification, new-version uploads (`FileItemId`), and the
> Arabic-named download helper. Live byte verification needs the SeaweedFS keys (user-secrets);
> until then the flow is checked headless and the live path is not visually verified.

## Purpose

The write side of Module 11 file binaries. Bytes never pass through the API: `initiate` reserves a
pending version and signs a 15-minute presigned PUT, the browser uploads straight to SeaweedFS with
`putPresigned`, then `complete` verifies (HEAD size, magic bytes, streamed SHA-256, optional client
checksum) and commits — or `abandon` cleans up a failed/cancelled reservation. Download streams
through the authenticated Range-capable route and saves under the server's RFC 5987 name.

## Layer & Import Rules (FSD)

Allowed: `@/entities/{drive-item,drive-space}`, `@/shared/*`. Forbidden: `@/pages/*`, `@/app/*`,
`@/widgets/*`, sibling features. Only the barrel exports below are public.

## Contents

| File | Purpose |
| --- | --- |
| `model/types.ts` | `InitiateDriveUploadRequest`, `CompleteDriveUploadRequest`, `DriveUploadInitiatedResponse`, and `DriveUploadTarget` (space + folder, or an existing file for a new version). |
| `api/driveUploadApi.ts` | `initiate` (`POST /drive/files/initiate`), `complete` (`POST /drive/files/{versionId}/complete`), `abandon` (`DELETE /drive/files/{versionId}/abandon`). |
| `api/driveUploadMutations.ts` | `useUploadDriveFileMutation` — one file's full flow (checksum → initiate → `putPresigned` → complete, abandon on any post-initiate failure); invalidates `driveItemKeys.all` + `driveSpaceKeys.all`. `DriveUploadPutError` tags a failed gateway PUT; `UploadStage` = preparing/uploading/verifying. |
| `model/download.ts` | `downloadDriveFile(item, versionId?)` — `content`/`versionContent` (attachment) + `parseContentDisposition` + `saveBlobAsFile`, falling back to the item name. |
| `ui/UploadDriveDialog.tsx` | Multi-file picker (single file for a new version), per-file pre-check via `driveFileRejection` + remaining quota, sequential upload with a three-stage progress bar per file, cancel-after-current, and a done/error summary. Seeds dropped files via `initialFiles`. |
| `index.ts` | Public barrel. |

## Behavior notes

- **Content-Type parity:** the presigned PUT signature covers `Content-Type`, so the mutation sends
  the exact `mimeType` it passed to `initiate` (`file.type || 'application/octet-stream'`).
- **Pre-check:** `driveFileRejection` (the `driveLimits` mirror) rejects empty/zero/oversized files,
  disallowed extensions, and insufficient remaining quota before any reservation; the server
  re-checks every rule at initiate/complete and stays authoritative.
- **Checksum:** the local SHA-256 is computed up front (best-effort) and sent to `complete`, so the
  server verifies the transfer as well as the storage. A hashing failure simply omits the checksum.
- **Cancel:** stops the queue after the current file (the in-flight PUT cannot be aborted mid-body);
  a failed reservation is abandoned by the mutation.
- **Invalidation:** a committed upload changes the browse grid, the file's versions and the space
  usage counter, so both entity slices are invalidated.

## Implementation status

| Item | State |
| --- | --- |
| Docs | ✅ |
| initiate/complete/abandon API + orchestrated mutation | ✅ Phase 3 |
| Multi-file queue dialog + three-stage progress + pre-check | ✅ Phase 3 |
| New-version upload (`FileItemId`) | ✅ Phase 3 |
| Download helper (Arabic name) | ✅ Phase 3 |
| Live byte verification | ⏳ Needs SeaweedFS keys; headless logic check until then (Phase 6 round). |

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-11-20 | Qoder | Front-end Phase 3: created the slice — presigned upload API + `useUploadDriveFileMutation` (initiate→PUT→complete/abandon, Content-Type parity, local checksum), `UploadDriveDialog` (sequential multi-file queue, three-stage progress, `driveFileRejection` pre-check, new-version mode, dropped-file seed), and `downloadDriveFile`. Front lint + build green. |