# `src/features/drive-editor` - Drive Office Editor (Module 11)

> **Status:** Implemented (front-end Phase 6) - lazy DocsAPI loading, signed edit/view config,
> host-fill editor sizing, cleanup, and Arabic error states; plus the in-app text/Markdown editor
> whose every save becomes a new immutable version, an editor close that forces a save so an
> edited-then-closed Word/Excel document still commits its version, and canvas image editing
> (rotate/flip/crop) whose save uploads a new immutable version.

## Purpose

Loads the OnlyOffice browser API and creates a document editor/preview surface for Office files.

## Layer & Import Rules (FSD)

Allowed: `@/entities/drive-item`, `@/shared/*`, and `@/features/drive-upload` (its public
`useUploadDriveFileMutation`, reused by the image editor so an edited picture becomes a new version
through the same initiate → PUT → complete flow). Public exports are limited to the feature barrel.
Forbidden: `@/widgets/*`, `@/pages/*`, `@/app/*`, other sibling features.

## Contents

| File | Purpose |
| --- | --- |
| `api/onlyOfficeLoader.ts` | Loads `/web-apps/apps/api/documents/api.js` once per OnlyOffice server URL and validates `window.DocsAPI`. |
| `api/driveTextMutations.ts` | `useSaveDriveTextMutation` — `PUT /api/drive/files/{itemId}/text`, invalidating the Drive item keys (versions/size) and the space quota keys. |
| `ui/DriveEditor.tsx` | Reads the signed edit config, mounts `DocEditor`, tracks document modification, forces a save on close (so the callback commits a new version), wires error events, fills the preview host, and refreshes the Drive queries after the save callback. |
| `ui/TextFileEditor.tsx` | In-app editor for `.txt`/`.md`: loads the current version's text, gates editing by the caller's effective role (Viewers read only), keeps an item-keyed draft, and saves the whole document as a new version with Arabic toasts/errors. |
| `model/imageEditing.ts` | Canvas helpers for image editing: `isEditableImage`/`editableImageMimeType` (`.png`/`.jpg`/`.jpeg` only, so a save re-encodes to the file's own signature-checked format), `blobToDataUrl` (a data URL keeps the canvas same-origin and untainted), `transformImageDataUrl` (rotate/flip), `cropImageDataUrl`, and `encodeImage`. |
| `ui/ImageEditor.tsx` | In-app image editor: rotate/flip/crop on a canvas with a drag selection, reset to the stored bytes, and «حفظ إصدار جديد» uploading the re-encoded image through `useUploadDriveFileMutation` (`initiate → PUT → complete`), so the previous versions remain in the history. Viewers see the image without the tools. |
| `index.ts` | Public barrel exporting `DriveEditor`, `TextFileEditor`, `ImageEditor` and `isEditableImage`. |

## Implementation status

| Item | State |
| --- | --- |
| Lazy DocsAPI + signed config mounting | Implemented |
| Edit/view mode and Arabic error states | Implemented |
| Resizable preview host sizing | Implemented |
| Forcesave on close so an edited Office document commits its version | Implemented |
| Text/Markdown editor (new version per save, Viewer read-only) | Implemented |
| Image editor: rotate/flip/crop, save as a new upload version (Viewer read-only) | Implemented |
| Live OnlyOffice save verification | Pending deployment configuration and SeaweedFS |

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-11-23 | Buffy | Added the image editor (`ImageEditor` + `model/imageEditing.ts`): rotate, flip and drag-crop a `.png`/`.jpg`/`.jpeg` on a canvas, then save the re-encoded result through the existing presigned upload flow as a new immutable version; Viewers keep a read-only image. |
| 2026-11-23 | Buffy | Added the `.txt`/`.md` editor (`TextFileEditor` + `useSaveDriveTextMutation`, each save a new immutable version) and made closing an edited Office document call `serviceCommand('forcesave')` and then invalidate the Drive item queries, so an edit that is closed — not saved in the editor's own button — still lands as a version. |
| 2026-11-23 | Copilot | Allowed editor loading and error surfaces to fill the enlarged resizable preview host. |
| 2026-11-21 | Copilot | Added the lazy OnlyOffice editor feature for Drive. |
