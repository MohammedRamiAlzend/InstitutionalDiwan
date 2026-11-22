# `src/pages/drive` — Drive (`/drive`)

> **Status:** Implemented (front-end Phases 1–6) — route, sidebar entry, workspace shell, spaces and quota, browse/upload, sharing/deletion, settings, and OnlyOffice preview/editor.

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
| `ui/DriveWorkspace.tsx` | Owns navigation and spaces, and mounts the browser, upload/preview/editor, version history/restore, permissions, deletion inbox, trash and settings panels. |
| `model/useDriveNavigation.ts` | URL-driven `view`/`space`/`folder` state and setters. |
| `index.ts` | Public barrel. |

## Permissions

- Page and all reads: `DRIVE_USE`.
- Space rename: owner or ADMIN; quota override and Drive settings: `DRIVE_MANAGE` (gated in the sidebar/feature).

## Implementation status

| Item | State |
| --- | --- |
| Route + sidebar entry | ✅ (`ROUTES.DRIVE`, `AppRouter`, `shell-nav` «الملفات» with `FolderOpen`) |
| Workspace shell, spaces, quota, responsive two-column composition | ✅ |
| Browse grid | ✅ Phase 2 (`widgets/drive-browser`: server grid/list, breadcrumb, filters, effective-role menus, DnD moves) |
| Upload / download / preview | ✅ Phase 3 (`features/drive-upload` queue dialog + new-version, `widgets/drive-preview-dialog`, `downloadDriveFile`; file-drop → upload) |
| Versions / permissions | ✅ Phase 4 |
| Deletion / trash / settings | ✅ Phase 5 |
| OnlyOffice editor / Office preview | ✅ Phase 6 (live deployment verification pending) |
| Live verification | ⏳ Pending OnlyOffice and SeaweedFS configuration. |

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-11-22 | Manus | Applied the polished workspace composition: a stable desktop navigation rail with a stacked small-screen layout. |
| 2026-11-21 | Copilot | Front-end Phase 6: mounted Office preview through the lazy OnlyOffice editor and completed Drive guides/documentation. |
| 2026-11-20 | Qoder | Front-end Phase 3: wired upload/download/preview into the workspace — `UploadDriveDialog` (toolbar/drop/new-version), `DrivePreviewDialog` (open-file), and `downloadDriveFile` (download action); the upload target follows the open folder/space and reads remaining quota. |
| 2026-11-21 | Copilot | Front-end Phase 4: mounted version history/download/restore and manager-gated permission/inheritance panels. |
| 2026-11-20 | Qoder | Front-end Phase 2: mounted `widgets/drive-browser` for the browse modes (server grid/list, breadcrumb, filters, effective-role menus, DnD moves); trash/deletion stay placeholders until Phase 5. |
| 2026-11-20 | Qoder | Front-end Phase 1: created the page, route, sidebar entry, URL navigation, lazy personal-space ensure, and the sidebar/quota shell. |
