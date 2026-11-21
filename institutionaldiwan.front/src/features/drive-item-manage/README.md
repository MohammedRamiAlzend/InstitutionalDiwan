# `src/features/drive-item-manage` — Drive Folder / Rename / Move / Star Writes (Module 11)

> **Status:** ✅ Implemented (2026-11-21, front-end Phase 4) — create-folder, rename, move (dialog +
> drag-and-drop), and the optimistic personal star toggle, with Zod name validation mirroring the
> domain rules and a lazy folder-tree destination picker. Consumed by `widgets/drive-browser`.

## Purpose

Write side of Module 11 Drive items: folder creation, rename, move, and star/unstar. Reads live in
`entities/drive-item`; this slice owns the mutations (POST/PUT/DELETE + `useMutation`), their Zod
schemas, and the dialogs. Mutations invalidate through `driveItemKeys`; the star toggle paints the
per-caller `starred` flag optimistically across cached browse pages and children lists.

## Layer & Import Rules (FSD)

Allowed: `@/entities/{drive-item,drive-space}`, `@/shared/*`. Forbidden: `@/pages/*`, `@/widgets/*`,
`@/app/*`, sibling features. Public API via `index.ts` only.

## Contents

| File | Purpose |
| --- | --- |
| `api/driveItemMutations.ts` | Folder/rename/move/star mutations plus `useRestoreDriveVersionMutation(itemId)` (POST `…/versions/{versionId}/restore`), all invalidating `driveItemKeys`. |
| `model/types.ts` | `CreateDriveFolderRequest`, `RenameDriveItemRequest`, `MoveDriveItemRequest`. |
| `model/schemas.ts` | `itemNameSchema` (trim, 1–255, no `\ / : * ? " < > \|`, not `.`/`..` — mirrors `DriveItem`), `moveItemSchema`; `ItemNameFormValues`, `MoveItemFormValues`. |
| `ui/CreateFolderDialog.tsx` | New folder in the active space/folder (`useDraftForm`, per-space draft id). |
| `ui/RenameItemDialog.tsx` | Rename a folder/file; current name prefills. |
| `ui/MoveItemDialog.tsx` | Destination picker + move; disabled when the destination equals the current parent. |
| `ui/FolderTreePicker.tsx` | Lazy folder tree of one space (root + expandable branches via `useDriveChildren`); the moving item's own node is disabled. |
| `index.ts` | Public barrel. |

## Backend contract notes

- Routes: `POST /api/drive/items/folders` `{spaceId,parentId?,name}`; `PUT …/{id}/rename` `{name}`;
  `PUT …/{id}/move` `{newParentId?}` (null = root); `POST …/{id}/star` / `DELETE …/{id}/star` (no body).
- Name rules mirror `DriveItem.MaxNameLength=255` and `ForbiddenNameChars`; the server also enforces
  sibling-name uniqueness (409), Editor access (403), and cycle/depth safety on move (409). Cross-space
  moves need Manager. The client surfaces these via `toArabicErrorMessage`.
- Star is idempotent and per-caller; the optimistic paint updates `browse` (PagedResult) and
  `children` (array) caches, rolls back on error, and invalidates on settle.

## Implementation status

| Item | State |
| --- | --- |
| Docs | ✅ |
| Mutations + schemas + types | ✅ Phase 2 |
| Create-folder / rename / move dialogs + folder tree | ✅ Phase 2 |
| Optimistic star toggle | ✅ Phase 2 |
| Version restore mutation | ✅ Phase 4 |
| Browser verification | ⏳ Phase 6 round |

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-11-20 | Qoder | Front-end Phase 2 (`plans/module-11-drive-frontend.md`): implemented the item write slice — create-folder/rename/move/star mutations (variable-based move + star for DnD/rows), Zod name schema mirroring the domain, the three dialogs and the lazy `FolderTreePicker`. Front lint + build green; no backend change. |
| 2026-11-21 | Copilot | Front-end Phase 4: added the immutable-version restore mutation. |