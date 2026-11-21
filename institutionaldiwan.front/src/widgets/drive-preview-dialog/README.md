# `src/widgets/drive-preview-dialog` — Drive File Preview (Module 11)

> **Status:** ✅ Implemented (2026-11-21, front-end Phase 6) — in-browser preview of images/PDF/
> video/audio from a short-lived presigned GET and Office viewing through the Drive editor.

## Purpose

A modal preview opened from a Drive file row (`actions.onOpenFile`). It classifies the file by
extension and renders media directly from the presigned download URL (the SeaweedFS gateway serves
bytes with Range support for video), so no blob passes through the API and no CORS is needed for
display. Every preview offers a download under the server's own Arabic file name.

## Layer & Import Rules (FSD)

Allowed: `@/features/{drive-upload,drive-editor}`, `@/entities/drive-item`, `@/shared/*`. Forbidden: `@/pages/*`,
`@/app/*`, sibling widgets. Only `DrivePreviewDialog` (+ the classifier helpers) is exported.

## Contents

| File | Purpose |
| --- | --- |
| `model/previewKind.ts` | `drivePreviewKind(fileName)` → image/pdf/video/audio/office/unsupported; `isUrlPreviewable(kind)` (the four media kinds rendered from a URL). |
| `ui/DrivePreviewDialog.tsx` | Media preview through `useDriveDownloadUrl`; Office mounts `DriveEditor`; loading/error states and Arabic download fallback for unsupported files. |
| `index.ts` | Public barrel — `DrivePreviewDialog`, `drivePreviewKind`, `isUrlPreviewable`, `DrivePreviewKind`. |

## Behavior notes

- **Presigned GET:** the URL is fetched fresh per open (`staleTime: 0`, `gcTime: 0`) because it expires
  in 5 minutes; a stale/failed URL shows `drive.preview.link-expired` with a retry.
- **Office:** classified separately and mounts OnlyOffice with the signed server config; the server
  decides edit/view mode from the caller's effective role.
- **Download:** always available in the footer, through the authenticated Range-capable route, saving
  under the RFC 5987 `Content-Disposition` name (Arabic-safe).

## Implementation status

| Item | State |
| --- | --- |
| Docs | ✅ |
| Image / PDF / video / audio preview | ✅ Phase 3 |
| Unsupported fallback + download | ✅ Phase 3 |
| OnlyOffice Office viewer | ✅ Phase 6 |
| Browser verification | ⏳ Phase 6 round (needs SeaweedFS keys for live bytes). |

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-11-21 | Copilot | Mounted the OnlyOffice viewer for Office files while preserving download fallback. |
| 2026-11-20 | Qoder | Front-end Phase 3: created the widget — extension classifier + `DrivePreviewDialog` (presigned-GET media preview, loading/error/link-expired states, unsupported/Office download fallback). Front lint + build green. |