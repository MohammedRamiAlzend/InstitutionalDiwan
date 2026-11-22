# `src/widgets/drive-browser` — Drive Content Region (Module 11)

> **Status:** ✅ Browse implemented — server-paged details/list/tiles/grid over
> `useDriveItems`, breadcrumb, toolbar (new-folder/refresh + upload hook), name search, kind/owner
> filters, effective-role-gated row/header/empty menus, and drag-and-drop moves. Upload/download/
> preview actions are wired in Phase 3, versions/permissions in Phase 4, deletion in Phase 5 via the
> optional `DriveItemActions` callbacks. Trash and deletion-requests are separate page views.

## Purpose

The main content region of `/drive` for the browse modes (`my-files` / `shared` / `starred`). It
reads the URL navigation state passed down by `pages/drive` (`view`/`space`/`folder`) and the spaces
overview, derives the server browse params, and renders either the shared `DataTable` (list) or a
thumbnail card grid — both driving the same URL search/sort/paging/filters and the same context
menus. Writes (folder/rename/move/star) come from `features/drive-item-manage`.

## Layer & Import Rules (FSD)

Allowed: `@/features/drive-item-manage`, `@/entities/{drive-item,drive-space,user}`, `@/shared/*`.
Forbidden: `@/pages/*`, `@/app/*`, sibling widgets. Only `DriveBrowser` (+ the `DriveItemActions`
type) is exported through `index.ts`.

## Contents

| File | Purpose |
| --- | --- |
| `ui/DriveBrowser.tsx` | Orchestrator: browse params from view/space/folder + `useTableState`, `useDriveItems`, batched effective roles, toolbar/filters, row/header/empty menus, four URL-persisted presentations, file selection/preview, DnD move + file-drop hook, and the create-folder/rename/move dialogs. Exports `DriveItemActions` (`onOpenFile`/`onDownload`/`onUploadVersion` wired in Phase 3; versions/permissions/deletion in later phases). |
| `ui/DriveBrowserControls.tsx` | Shared toolbar: debounced name search, details/list/tiles/grid switch, new-folder/upload buttons, kind/owner `Select` filters, refresh. |
| `ui/DriveBreadcrumb.tsx` | Root crumb (the space) + `useDriveBreadcrumb(folderId)` ancestors; each crumb navigates and is a drop target. |
| `ui/DriveItemsGrid.tsx` | Icon grid, compact list, and tiles: kind glyph, name, star, size/modified metadata, per-item context menu, selection, draggable items, folder drop targets, and a paging footer. |
| `ui/DrivePreviewPanel.tsx` | Selected-item side panel with metadata and available open/download actions; full media/Office preview continues through `DrivePreviewDialog`. |
| `ui/DriveDropTarget.tsx` | Folder drop zone wrapper (highlight on item-drag hover; ignores self-drops). |
| `model/useItemRoles.ts` | `useQueries` batch of `/effective` for the page's items → `Record<itemId, roleId\|undefined>`; `undefined` = loading, so write actions stay hidden until the role is known. |
| `model/dnd.ts` | `DRIVE_ITEM_MIME` + `startItemDrag`/`isItemDrag`/`readDraggedItemId` helpers. |
| `index.ts` | Public barrel — `DriveBrowser`, `DriveItemActions`. |

## Behavior notes

- **Browse params:** `my-files` → personal space + open folder; `shared` → the selected shared space
  (a «pick a space» empty state when none is chosen); `starred` → cross-space `starred=true`. Kind
  filter maps `folder`/`file` → `DRIVE_ITEM_KIND`; owner filter maps a user id; sort keys are
  `name`/`size`/`modifiedAt` (the server's accepted set).
- **Effective-role gating:** row write actions (rename/move/deletion) need Editor+, permissions needs
  Manager; the toolbar new-folder needs Editor on the open folder (or space owner/`DRIVE_MANAGE` at
  the root, always in `my-files`). Star/open/copy are available to any reachable item.
- **Drag-and-drop:** the name cell (list) and card (grid) are drag sources when the caller can edit;
  folder cells/cards and breadcrumb crumbs are drop targets → `move`. The move dialog is the
  keyboard/touch alternative. File drops are forwarded to `onDropFiles` (wired in Phase 3 to seed
  the upload queue); the toolbar upload button and the per-file «new version» action use the same
  feature.
- **Table convention:** list mode is `shared/ui/data-table` (`tableId="drive-items"`, server mode,
  column chooser persisted under `diwan.table.drive-items.columns.v1`); no bespoke `<table>`.

## Implementation status

| Item | State |
| --- | --- |
| Docs | ✅ |
| Browse details/list/tiles/grid + breadcrumb + toolbar + filters + URL state | ✅ |
| File selection side panel with metadata and preview/download actions | ✅ |
| Effective-role-gated menus + create-folder/rename/move/star | ✅ Phase 2 |
| Drag-and-drop move + move-dialog alternative | ✅ Phase 2 |
| Upload / download / preview actions | ✅ Phase 3 (`onOpenFile`/`onDownload`/`onUploadVersion` + `onDropFiles`/`onUpload` wired by the page) |
| Versions / permissions actions | ⏳ Phase 4 |
| Request-deletion action | ⏳ Phase 5 |
| Browser verification | ⏳ Phase 6 round (manual login) |

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-11-22 | Manus | Added Explorer-style details/list/tiles/grid presentations and a selected-file side panel with metadata and contextual actions. |
| 2026-11-20 | Qoder | Front-end Phase 3: added `onUploadVersion` to `DriveItemActions` and a per-file «رفع إصدار جديد» row-menu entry (Editor+, files only); the page now wires `onOpenFile`/`onDownload`/`onUploadVersion` + `onUpload`/`onDropFiles`. No browse/read change. |
| 2026-11-20 | Qoder | Front-end Phase 2: replaced the placeholder with the real browse region — `DataTable` (server, `drive-items`) + card grid sharing URL state/menus, breadcrumb, controls (search/layout/new-folder/kind/owner/refresh), batched effective-role gating, DnD moves + move dialog, and `DriveItemActions` extension points for Phases 3–5. Front lint + build green. |
| 2026-11-20 | Qoder | Front-end Phase 1: created the content-region placeholder ahead of the Phase 2 grid. |
