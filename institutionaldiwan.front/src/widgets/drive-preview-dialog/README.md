# `src/widgets/drive-preview-dialog` — Drive File Preview (Module 11)

> **Status:** ✅ Implemented (2026-11-21, front-end Phase 6) — in-browser preview of images/PDF/
> video/audio from a short-lived presigned GET, Office viewing through the Drive editor, text
> editing, and canvas image editing for PNG/JPEG that saves a new version.

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
| `model/previewKind.ts` | `drivePreviewKind(fileName)` → image/pdf/video/audio/office/text/unsupported; `isUrlPreviewable(kind)` (the four media kinds rendered from a URL). |
| `ui/DrivePreviewDialog.tsx` | Media preview through `useDriveDownloadUrl`; Office mounts `DriveEditor`, `.txt`/`.md` mount `TextFileEditor`, `.png`/`.jpg`/`.jpeg` mount `ImageEditor` (editable images load through the authenticated content route instead of the presigned URL); loading/error states and Arabic download fallback for unsupported files. |
| `index.ts` | Public barrel — `DrivePreviewDialog`, `drivePreviewKind`, `isUrlPreviewable`, `DrivePreviewKind`. |

## Behavior notes

- **Presigned GET:** the URL is fetched fresh per open (`staleTime: 0`, `gcTime: 0`) because it expires
  in 5 minutes; a stale/failed URL shows `drive.preview.link-expired` with a retry.
- **Office:** classified separately and mounts OnlyOffice with the signed server config; the server
  decides edit/view mode from the caller's effective role.
- **Text:** `.txt`/`.md` open the in-app text editor, whose every save becomes a new immutable version.
- **Images:** `.png`/`.jpg`/`.jpeg` open the image editor (rotate/flip/crop); its save uploads the
  re-encoded image as a new immutable version. Other image formats (GIF/WebP/SVG) keep the plain
  read-only preview.
- **Download:** always available in the footer, through the authenticated Range-capable route, saving
  under the RFC 5987 `Content-Disposition` name (Arabic-safe).
- **Resizable modal:** the preview opens at 95% of the viewport width and 85% of the viewport height,
  and exposes the browser's native resize handle so the preview can be adjusted in both directions.

## Implementation status

| Item | State |
| --- | --- |
| Docs | ✅ |
| Image / PDF / video / audio preview | ✅ Phase 3 |
| Unsupported fallback + download | ✅ Phase 3 |
| OnlyOffice Office viewer | ✅ Phase 6 |
| Text/Markdown editor | ✅ |
| PNG/JPEG image editor (new version per save) | ✅ |
| Large, manually resizable preview | ✅ |
| Browser verification | ⏳ Phase 6 round (needs SeaweedFS keys for live bytes). |

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-11-23 | Copilot | Enlarged the preview to 95vw by 85dvh and added native two-direction resizing, including full-height PDF/Office/media surfaces. |
| 2026-11-23 | Buffy | Mounted `ImageEditor` for editable `.png`/`.jpg`/`.jpeg` images (rotate/flip/crop, saved as a new version); GIF/WebP/SVG keep the plain preview. |
| 2026-11-23 | Buffy | Classified `.txt`/`.md` as `text` and mounted `TextFileEditor` for them. |
| 2026-11-21 | Copilot | Mounted the OnlyOffice viewer for Office files while preserving download fallback. |
| 2026-11-20 | Qoder | Front-end Phase 3: created the widget — extension classifier + `DrivePreviewDialog` (presigned-GET media preview, loading/error/link-expired states, unsupported/Office download fallback). Front lint + build green. |