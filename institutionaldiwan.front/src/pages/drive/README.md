# `src/pages/drive` — Drive (`/drive`)

> **Status:** Implemented (front-end Phases 1–3) — route, sidebar entry, workspace shell, spaces and quota, the browse grid/list, and upload/download/preview; versions/permissions/deletion land in Phases 4–5.

## Purpose

Module 11 «الملفات»: an independent file workspace (not the Module 07 document archive). A personal
space plus shared spaces, folders and versioned files, hierarchical permissions, approval-gated
deletion and a temporary trash. Phase 1 ships the shell: navigation, the spaces overview, lazy
personal-space creation, and the quota meter.

## Routes & URL state

`/drive?view=&space=&folder=`

- `view` — `my-files` (default) | `shared` | `starred` | `trash` | `deletion`.
- `space` — the selected shared space id (or an optional filter for starred/trash); `my-files`
  resolves to the personal space automatically.
- `folder` — the open folder id; null is the space root (consumed by the Phase 2 grid).

Switching view or space resets `folder`. The page is wrapped in `RequirePermission` for `DRIVE_USE`;
the server further limits every read to what the caller can reach.

## Contents

| File | Purpose |
| --- | --- |
| `ui/DrivePage.tsx` | `DRIVE_USE` guard + `drive` tutorial provider around the workspace. |
| `ui/DriveWorkspace.tsx` | Owns `useDriveNavigation` + `useDriveSpaces`, ensures the personal space on first entry, renders the page header, the sidebar rail and the content region. Browse modes mount `widgets/drive-browser` with upload/new-version/preview/download actions wired (`UploadDriveDialog`, `DrivePreviewDialog`, `downloadDriveFile`); trash/deletion render a temporary placeholder until Phase 5. |
| `model/useDriveNavigation.ts` | URL-driven `view`/`space`/`folder` state and setters. |
| `index.ts` | Public barrel. |

## Permissions

- Page and all reads: `DRIVE_USE`.
- Space rename: owner or ADMIN; quota override and Drive settings: `DRIVE_MANAGE` (gated in the sidebar/feature).

## Implementation status

| Item | State |
| --- | --- |
| Route + sidebar entry | ✅ (`ROUTES.DRIVE`, `AppRouter`, `shell-nav` «الملفات» with `FolderOpen`) |
| Workspace shell, spaces, quota | ✅ (`npm run build` and `npm run lint` pass) |
| Browse grid | ✅ Phase 2 (`widgets/drive-browser`: server grid/list, breadcrumb, filters, effective-role menus, DnD moves) |
| Upload / download / preview | ✅ Phase 3 (`features/drive-upload` queue dialog + new-version, `widgets/drive-preview-dialog`, `downloadDriveFile`; file-drop → upload) |
| Versions / permissions | ⏳ Phase 4 |
| Deletion / trash / settings | ⏳ Phase 5 (replaces the trash/deletion placeholders) |
| Live verification | ⏳ Pending the Phase 6 browser round (manual login). |

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-11-20 | Qoder | Front-end Phase 3: wired upload/download/preview into the workspace — `UploadDriveDialog` (toolbar/drop/new-version), `DrivePreviewDialog` (open-file), and `downloadDriveFile` (download action); the upload target follows the open folder/space and reads remaining quota. |
| 2026-11-20 | Qoder | Front-end Phase 2: mounted `widgets/drive-browser` for the browse modes (server grid/list, breadcrumb, filters, effective-role menus, DnD moves); trash/deletion stay placeholders until Phase 5. |
| 2026-11-20 | Qoder | Front-end Phase 1: created the page, route, sidebar entry, URL navigation, lazy personal-space ensure, and the sidebar/quota shell. |