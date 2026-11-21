# `src/entities/drive-item` — Drive Item / Version / Permission Read Model (Module 11)

> **Status:** ✅ Reads implemented (2026-11-20, Module 11 front-end Phase 0) — browse, by-id,
> breadcrumb, children, versions, permissions (opt-in), effective role, deletion requests, trash,
> presigned download URL, and the two byte reads (current/named version), plus query keys/hooks and
> six presentational badges. The browse grid/list (`widgets/drive-browser`, Phase 2) consumes browse,
> breadcrumb, children and effective; upload and preview land in Phase 3, versions/permissions in
> Phase 4, deletion/trash in Phase 5. Writes live in the `drive-*` features.

## Purpose

Read models and queries for Module 11 «الملفات» items (the unified folder/file node), file versions,
explicit permission grants, approval-gated deletion requests, and trash. Drive is independent of
document archiving (Module 07). Writes (folder/rename/move/star/upload/restore/grant/revoke/delete/
purge) live in the `drive-*` features; the browser, breadcrumb, grids and dialogs live in
`widgets/drive-*` and `pages/drive`.

## Contents

| File | Purpose |
| --- | --- |
| `api/driveItemApi.ts` | `browse(params)` → `GET /api/drive/items` (paged); `get`, `breadcrumb`, `children(spaceId, parentId?)`, `versions`, `permissions`, `effective`, `deletionRequests(params)` (paged), `trash(params)` (paged), `downloadUrl(itemId, versionId?)`, `content(itemId, versionId?, disposition?)` and `versionContent(itemId, versionId, disposition?)` (both `BlobDownload`). |
| `model/types.ts` | `DriveItemResponse`, `DriveVersionResponse`, `DrivePermissionResponse`, `DriveDeletionRequestResponse`, `DriveBreadcrumbNode`, `DriveEffectiveRoleResponse`, `DriveDownloadUrlResponse`, and the `DriveBrowseParams` / `DriveDeletionRequestParams` / `DriveTrashParams` query shapes. |
| `model/queries.ts` | `driveItemKeys` factory and `useDriveItems`, `useDriveItem`, `useDriveBreadcrumb`, `useDriveChildren`, `useDriveVersions`, `useDriveDownloadUrl` (presigned GET, never cached across opens), `useDrivePermissions` (opt-in), `useDriveEffectiveRole`, `useDriveDeletionRequests`, `useDriveTrash`. |
| `model/navigation.ts` | `DriveView` (`my-files`/`shared`/`starred`/`trash`/`deletion`), `DRIVE_VIEWS`, `isDriveView` — the URL browse-mode contract shared by `pages/drive` and the `drive-*` widgets (Phase 1). |
| `ui/ItemKindIcon.tsx` | lucide `Folder` / `FileText` from `itemKindId`; `aria-label` via `useLabel` (`drive.item-kind.*`). |
| `ui/RoleBadge.tsx` | «بلا صلاحية» / «مشاهد» / «محرّر» / «مدير» from `roleId` (`DRIVE_ROLE`), unknown → None (`drive.role.*`). |
| `ui/VersionStatusBadge.tsx` | pending = warning, committed = success, abandoned = outline (`drive.version-status.*`). |
| `ui/VersionSourceBadge.tsx` | upload = neutral, editor-save = accent, restore = neutral (`drive.version-source.*`). |
| `ui/DeletionStatusBadge.tsx` | pending = warning, approved = success, rejected = danger, cancelled = outline (`drive.deletion-status.*`). |
| `ui/StarIndicator.tsx` | lucide `Star`, filled warning when `starred`; `aria-label` via `useLabel` (`drive.action.star` / `drive.action.unstar`). |
| `index.ts` | Public barrel — import from `@/entities/drive-item`, never from a subpath. |

## Query keys (contract)

```ts
export const driveItemKeys = {
  all: ['drive-items'] as const,
  browse: (params) => [...all, 'browse', params] as const,
  detail: (itemId) => [...all, 'detail', itemId] as const,
  breadcrumb: (itemId) => [...all, 'breadcrumb', itemId] as const,
  children: (spaceId, parentId?) => [...all, 'children', spaceId, parentId ?? 'root'] as const,
  versions: (itemId) => [...all, 'versions', itemId] as const,
  downloadUrl: (itemId) => [...all, 'download-url', itemId] as const,
  permissions: (itemId) => [...all, 'permissions', itemId] as const,
  effective: (itemId) => [...all, 'effective', itemId] as const,
  deletionRequests: (params) => [...all, 'deletion-requests', params] as const,
  trash: (params) => [...all, 'trash', params] as const,
};
```

The whole `params` object is the browse/trash/deletion-requests cache key (server-paged). Mutations in
the `drive-*` features invalidate the affected `browse`/`children`/`detail`/`versions`/`permissions`/
`trash`/`deletionRequests` keys, and a star toggle invalidates `browse` (the `starred` flag is
per-caller). Keys are not user-scoped, following the `archive` precedent — the server filters by
caller access on every read.

## Backend contract notes

- **Paged vs. array:** `browse`, `trash`, `deletionRequests` return `PagedResult<T>` (server-paged);
  `children`, `versions`, `permissions`, `breadcrumb` return plain arrays. Grids for the paged reads
  use `shared/ui/data-table` in `server` mode; the array reads render directly.
- `children(spaceId, parentId?)` lists a folder's direct children (or the space root when `parentId`
  is null), folders first, unpaged — the move dialog and tree read this.
- `effective(itemId)` returns `{ roleId }` (0 = no access) and needs no Viewer role, so it is safe to
  gate per-item buttons with it. Drive **has** this endpoint (unlike attachments), so actions can be
  disabled accurately instead of predicting a `403`.
- `permissions(itemId)` lists **explicit grants only** (Manager); inherited access is computed by the
  recursive access evaluator, not listed. `useDrivePermissions` defaults to `enabled: false` and is
  opted into while the permissions dialog is open (Phase 4).
- `downloadUrl` returns a short-lived presigned GET (5 min) for direct download, media preview, or the
  OnlyOffice server. `content`/`versionContent` stream bytes through the authenticated route, which
  supports HTTP Range (206 + `Content-Range`); `disposition: 'attachment'` forces a download, the
  default is inline. Both go through `downloadBlob`, so a failure arrives as a blob-bodied
  ProblemDetails normalized to an `ApiError` with the real domain code (`toApiErrorAsync`).
- `DriveVersionResponse.checksumSha256` is nullable; `relativePath`-style internal storage paths are
  never exposed. Enum IDs resolve through `DRIVE_ITEM_KIND` / `DRIVE_VERSION_STATUS` /
  `DRIVE_VERSION_SOURCE` / `DRIVE_ROLE` / `DRIVE_PRINCIPAL_TYPE` / `DRIVE_DELETION_STATUS` in
  `shared/config/driveIds.ts`, never magic literals.
- Deletion is approval-gated (department-head approval even for personal files); `deletionRequests`
  visibility is filtered server-side (`mineOnly` narrows to the caller's own requests).

## Implementation status

| Item | State |
| --- | --- |
| Docs | ✅ |
| Types, API client, query keys/hooks | ✅ Module 11 front-end Phase 0 |
| Six badges (`ItemKindIcon`, `RoleBadge`, `VersionStatusBadge`, `VersionSourceBadge`, `DeletionStatusBadge`, `StarIndicator`) | ✅ Phase 0; mounted by the browser/grid (Phase 2), versions/permissions dialogs (Phase 4) and deletion/trash grids (Phase 5); labels catalog-driven (`drive.*`). |
| Byte reads (`content`, `versionContent`, `downloadUrl`) | ✅ Phase 0 client; `useDriveDownloadUrl` hook added in Phase 3, consumed by the preview dialog (and the Phase 6 editor); `downloadDriveFile` uses the byte reads. |
| Browser / breadcrumb / folders / drag-and-drop UI | ✅ Phase 2 (`widgets/drive-browser` reads browse/breadcrumb/children/effective; writes in `features/drive-item-manage`) |
| Upload / download / preview UI | ✅ Phase 3 (`features/drive-upload` + `widgets/drive-preview-dialog`) |
| Versions / permissions UI | ⏳ Phase 4 |
| Deletion / trash / settings UI | ⏳ Phase 5 |
| OnlyOffice editor | ⏳ Phase 6 |

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-11-20 | Qoder | Front-end Phase 3: added the `downloadUrl` query key and `useDriveDownloadUrl` hook (presigned GET, `staleTime`/`gcTime` 0) for the preview dialog and the Phase 6 editor; exported from the barrel. No backend change. |
| 2026-11-20 | Qoder | Front-end Phase 2: `widgets/drive-browser` now consumes `browse`/`breadcrumb`/`children`/`effective` (batched via `useItemRoles`); `features/drive-item-manage` writes invalidate these keys. No read-model change. |
| 2026-11-20 | Qoder | Front-end Phase 1: added `model/navigation.ts` (`DriveView`, `DRIVE_VIEWS`, `isDriveView`) and exported it from the barrel; `pages/drive` now drives the URL browse mode from it. |
| 2026-11-20 | Qoder | Module 11 front-end Phase 0 (`plans/module-11-drive-frontend.md`): implemented the read slice — `driveItemApi` (browse/get/breadcrumb/children/versions/permissions/effective/deletion-requests/trash/download-url + the two Range-capable byte reads), the seven response types and three query-param shapes, `driveItemKeys` (ten keys) with nine hooks (`useDrivePermissions` opt-in, `useDriveEffectiveRole` for accurate action gating), and six presentational badges resolving `drive.*` catalog keys off `DRIVE_*` ID maps. No page consumes it yet — the browser lands in Phase 2. |