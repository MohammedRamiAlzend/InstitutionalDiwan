# Application / Features / Drive — Google-Drive-like file module (Module 11)

> **Status:** Phase 4 (OnlyOffice editing) implemented — the application core (spaces, folders/files,
> stars, presigned uploads, download/Range, immutable versions + restore, hierarchical permissions,
> approval-gated deletion, trash/restore/purge, name search, institution Drive settings) is exposed
> through eight thin HTTP controllers (`DRIVE_USE`; settings/quota `DRIVE_MANAGE`), plus in-site
> Word/Excel/PowerPoint editing via a self-hosted OnlyOffice Document Server (signed editor config +
> save callback → new immutable version, conflict rejection, dedup). The comprehensive test pass
> (Phase 5) is complete — a full-lifecycle integration test, concurrent quota/version load tests and a
> security review; `dotnet test` green at 1264. Independent of document archiving (Module 07).

## Purpose

Module 11 («الملفات») is a standalone file workspace: a personal space «ملفاتي» per user plus freely
created shared spaces, a folder/file tree, hierarchical permissions (Viewer / Editor / Manager) granted
to users or departments with break-inheritance, unlimited immutable file versions, approval-gated
deletion (the owner's department head, with an ADMIN fallback — even for personal files), a 30-day
trash, per-space quotas, and HTTP Range streaming for video. Binaries live in SeaweedFS through its S3
API; the browser uploads directly to a presigned PUT URL, so bytes never pass through the API server.

Every use case is a MediatR command/query with a FluentValidation validator and unified `Drive.*`
errors. Handlers read the ambient `IUserContext`, gate access through `IDriveAccessEvaluator`, stamp
writes with `IUnitOfWork.UtcNow`, and persist through the Drive repositories. Invariants live in the
domain aggregates (`DriveSpace`, `DriveItem`, `DriveFileVersion`, `DrivePermission`,
`DriveDeletionRequest`, `DriveEditSession`, `DriveStar`); the handlers add persistence + authorization.

## Contents

| File | Purpose |
| --- | --- |
| `DriveSpaceCommands.cs` | `CreateDriveSpaceCommand` (shared space; quota override needs ADMIN), `RenameDriveSpaceCommand` (owner/ADMIN), `SetDriveSpaceQuotaCommand` (ADMIN; rejects below current usage), `EnsurePersonalDriveSpaceCommand` (idempotent lazy personal space). Hosts the shared `EffectiveQuota(space, setting)` helper. |
| `DriveSpaceQueries.cs` | `GetDriveSpaceQuery` (owner/ADMIN/member), `ListDriveSpacesQuery` → `DriveSpacesOverviewResponse` (personal + shared the caller can reach). |
| `DriveSpaceValidators.cs` | Name length, positive optional quota, non-empty ids. |
| `DriveItemCommands.cs` | `CreateDriveFolderCommand` (root needs space owner/ADMIN; inside a folder needs Editor+; depth = parent+1; sibling-name check), `RenameDriveItemCommand` (Editor+), `MoveDriveItemCommand` (cycle/depth via domain; cross-space needs Manager), `StarDriveItemCommand` / `UnstarDriveItemCommand` (Viewer+, idempotent). |
| `DriveItemQueries.cs` | `BrowseDriveItemsQuery` (paged, access-filtered, search/kind/owner/trashed/starred/modified filters + sorting), `GetDriveItemQuery` (Viewer+), `GetDriveBreadcrumbQuery` (root-first ancestors + item), `GetDriveChildrenQuery`. |
| `DriveItemValidators.cs` | Folder/rename/move/star input rules. |
| `DriveUploadCommands.cs` | `InitiateDriveUploadCommand` (extension/size/quota pre-check, reserves a Pending version, returns a presigned PUT URL; new file or new version of an existing file), `CompleteDriveUploadCommand` (HEAD size + magic bytes + streamed SHA-256 + optional client checksum, then commit + usage + current version in one save), `AbandonDriveUploadCommand` (abandon + delete object). |
| `DriveUploadValidators.cs` | Upload initiate/complete/abandon input rules. |
| `DriveUploadGuard.cs` | Shared declared-size/extension pre-check and magic-byte signature check; maps `Attachments.*` signature errors to `Drive.*`. |
| `DriveFileQueries.cs` | `GetDriveFileContentQuery` (current or named version, optional Range; returns `DriveFileContentResult` with the file name and the effective inclusive range bounds for the controller's 206/`Content-Range`), `GetDriveDownloadUrlQuery` (presigned GET), `ListDriveFileVersionsQuery` (newest first). Hosts `DriveVersionResolver` (file + committed-version resolution behind an access gate). |
| `DriveFileVersionCommands.cs` | `RestoreDriveFileVersionCommand` — restores by **copying** the old object to a new version (server-side S3 `CopyAsync`, `Source=RestoreId`); history is never edited. |
| `DrivePermissionCommands.cs` | `GrantDrivePermissionCommand` (Manager+, upsert, notifies user grantees or a department's users), `RevokeDrivePermissionCommand` (Manager+), `SetDriveItemInheritanceCommand` (break/restore inheritance). |
| `DrivePermissionQueries.cs` | `ListDriveItemPermissionsQuery` (Manager+), `GetDriveEffectiveRoleQuery` (the caller's effective role, 0 when none). |
| `DriveDeletionCommands.cs` | `RequestDriveItemDeletionCommand` (Editor+, one pending per item, notifies the approver), `ApproveDriveDeletionCommand` / `RejectDriveDeletionCommand` (reviewer only; approve trashes the item), `CancelDriveDeletionCommand` (requester only), `DeleteDriveItemDirectlyCommand` (ADMIN, or department head with Manage). |
| `DriveDeletionApprovals.cs` | Shared reviewer gate (`CanReviewAsync`) and approver-recipient resolution (owner's department head, else every active ADMIN). |
| `DriveDeletionQueries.cs` | `ListDriveDeletionRequestsQuery` — requester's own (`MineOnly`), ADMIN sees all, a head sees the requests they may review (SQL-filtered). |
| `DriveTrashCommands.cs` | `RestoreDriveItemCommand` (Manager+; returns to the space root when the original parent is gone/trashed; name check), `PurgeDriveItemCommand` (ADMIN/head+Manage; permanent tree delete via `IDrivePurgeService`). |
| `DriveTrashQueries.cs` | `ListDriveTrashQuery` (paged trashed items, optional space/search). |
| `DriveSettingsFeature.cs` | `GetDriveSettingsQuery` / `UpdateDriveSettingsCommand` (ADMIN; default quota, max file size, allowed extensions on `InstitutionSetting`). |
| `DriveNotificationPublisher.cs` | `IDriveNotificationPublisher` implementation staging Drive inbox rows (grant, deletion requested/approved/rejected). |
| `DriveResponses.cs` | All Drive wire DTOs with static `From` mappers (spaces, items, versions, permissions, deletion requests, settings, breadcrumb, upload/download payloads, file content). |
| `DriveEditResponses.cs` | OnlyOffice DTOs: `DriveEditConfigResponse` (server URL, document type, mode, session id, version, full signed config), `DriveOnlyOfficeCallbackResult` (`error=0/1`), `DriveOnlyOfficeCallbackPayload` (key/status/url/users/forcesavetype/token), and `DriveDocumentTypes` (extension → word/cell/slide, editable set). |
| `DriveEditConfigQuery.cs` | `GetDriveEditConfigQuery` — builds the OnlyOffice editor config: presigned document URL, unique key `{itemId:N}-{versionNumber}`, callback URL, edit/view mode by effective role, and an HS256 token whose payload is the config itself. Opens or reuses the one open edit session per key. |
| `DriveReadinessQuery.cs` | `GetDriveReadinessQuery` — secret-free readiness for the editing path: the OnlyOffice enable switch, which of the server URL / callback base URL / JWT secret are present, the resolved `callbackUrl`, and whether Drive storage is configured. `isReady` requires OnlyOffice **and** storage to be configured; no secret or credential value is returned. |
| `DriveOnlyOfficeCallbackCommand.cs` | `ProcessDriveOnlyOfficeCallbackCommand` — verifies the callback JWT (no ambient user; actor = session owner), then status 1 touch / 4 close / 2+6 save. Save rejects conflicts (`CurrentVersionId` ≠ `BaseVersionId`), dedups by SHA-256, re-checks size/quota/magic bytes, writes the bytes server-side (`WriteAsync`), and commits a new `Source=EditorSave` version. The `Result` always succeeds so the transaction commits and a save-failure notification to the editor persists; the protocol error code is the payload. |

## Conventions / Notes

- **Access is hierarchical and evaluated, not stored per caller.** `IDriveAccessEvaluator` walks the
  ancestor chain (recursive CTE) and unions user/department grants; the space owner always has Manage.
  Viewer=1, Editor=2, Manager=3; 0 means no access. Trashed items are reachable only by
  owner/space-owner/ADMIN/head; live items under a trashed ancestor are unreachable except by ADMIN.
- **Uploads are direct-to-storage.** Initiate reserves a Pending version and pre-checks
  extension/size/quota; the browser PUTs to SeaweedFS; complete verifies the stored object (HEAD size,
  magic bytes, streamed SHA-256) before committing. Each committed version adds its bytes to the
  space's `UsedBytes` — versions accumulate, and trash keeps counting until permanent purge.
- **Restore never edits history.** Restoring a version copies its object into a brand-new version
  (`Source=RestoreId`) and makes it current; the old rows stay immutable.
- **Deletion is approval-gated, even for personal files.** A request notifies the owner's department
  head (or every active ADMIN when there is no head). Direct delete and permanent purge are allowed
  only for ADMIN, or a department head who also has Manage on the item.
- **Purge is shared and ordered.** `IDrivePurgeService` deletes S3 objects first, then rows
  (deepest-first), then releases quota, with one audit row per tree. It severs the
  `DriveItem.CurrentVersionId` reference before deleting versions to avoid the Restrict/Cascade FK
  cycle. Both the manual purge handler and `DriveMaintenanceJob` use it.
- **Storage errors are domain errors.** An unconfigured storage provider answers
  `Drive.StorageNotConfigured`; a missing object answers `Drive.StorageObjectMissing`.
- **OnlyOffice editing is a shared room per document key.** The key `{itemId:N}-{versionNumber}` is
  unique per item+version (OnlyOffice collaboration rule); a filtered unique index allows one **open**
  edit session per key, so collaborators reuse it. The caller's own identity goes into the editor
  config; the session owner is the actor stamped on saved versions. The save callback has no ambient
  user session — it is authenticated by the HS256 callback token alone (`[AllowAnonymous]` + handler
  verification). A save is rejected when the file's current version moved on since the session opened
  (lost-update guard, applied to forcesave too), and an unchanged SHA-256 updates the session without
  polluting history. Editing needs `Drive:OnlyOffice` (server URL, callback base URL, JWT secret) and
  `Drive:Storage` configured; otherwise `Drive.OnlyOfficeNotConfigured` / `Drive.StorageNotConfigured`.

## Implementation status

| Item | State |
| --- | --- |
| Spaces (create/get/list/rename/quota/ensure-personal) | ✅ |
| Items (folder/rename/move/browse/breadcrumb/children/stars) | ✅ |
| Upload (initiate/complete/abandon) + guard | ✅ |
| Files (content/Range/download-url/versions) + restore | ✅ |
| Permissions (grant/revoke/list/effective/inheritance) | ✅ |
| Deletion (request/list/approve/reject/cancel/direct) | ✅ |
| Trash (list/restore/purge) | ✅ |
| Settings (get/update) | ✅ |
| Validators + unified `Drive.*` errors | ✅ |
| Handler tests (quotas, break-inheritance, direct-delete vs request, ADMIN fallback) | ✅ (`Tests/Application/Drive*HandlerTests.cs`) |
| HTTP controllers | ✅ Phase 3–4 (`Server/Controllers/Drive*Controller.cs`, live-verified on port 5028) |
| OnlyOffice in-site editing (edit-config + save callback) | ✅ Phase 4 (handler + token tests; live editing session needs the OnlyOffice server + SeaweedFS keys) |
| OnlyOffice/storage readiness probe (`GET /api/drive/onlyoffice/readiness`) | ✅ `GetDriveReadinessQuery` — presence flags and non-secret URLs only (`DriveReadinessTests`) |
| Comprehensive tests + security review | ✅ Phase 5 (`Tests/Application/DriveLifecycleIntegrationTests`, `DriveConcurrencyTests`, `DriveSecurityTests`; 1264 tests green; a real `DrivePurgeService` tracked-session bug found and fixed in Infrastructure) |

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-11-22 | Buffy | Added `GetDriveReadinessQuery` (secret-free readiness for OnlyOffice editing + Drive storage; `IsReady` = both configured) backing `GET /api/drive/onlyoffice/readiness`, with four handler tests. |
| 2026-11-20 | Qoder | Drive Phase 5 (plan `plans/module-11-drive-backend.md`): comprehensive tests + security review (no application-code change). Added a full-lifecycle integration test, concurrent quota/version load tests and a security suite (14 new tests; suite green at 1264). Security review confirmed: presigned URLs are scoped to one bucket+key+verb with short lifetimes (upload 15 min, download 5 min, editor document URL 15 min) and GUID object keys never carry the user file name; the OnlyOffice callback is HS256-verified with no ambient user; `DriveItem.ValidateName` + `S3DriveStorageService.ValidateKey` reject name injection and `..`/`\`/leading-`/` traversal; `DriveVersionResolver` gates reads at Viewer, isolates versions across files and refuses pending bytes; trash/purge stay ADMIN-or-head+Manage. The integration test surfaced a real `DrivePurgeService` bug (edit sessions removed via `ExecuteDeleteAsync` bypassed the change tracker), fixed in Infrastructure with a regression test. |
| 2026-11-19 | Qoder | Drive Phase 4 (plan `plans/module-11-drive-backend.md`): OnlyOffice in-site editing — `GetDriveEditConfigQuery` (signed editor config, edit/view by role, one open session per document key) and `ProcessDriveOnlyOfficeCallbackCommand` (JWT-verified save callback → new `EditorSave` version, conflict rejection + editor notification, SHA-256 dedup, size/quota/magic-byte guards, server-side `WriteAsync`). Added `DriveEditResponses.cs` and the `Drive.OnlyOfficeNotConfigured`/`EditingNotSupported`/`CallbackTokenInvalid`/`CallbackDownloadFailed` errors. 27 new handler + token tests; suite green at 1250. |
| 2026-11-19 | Qoder | Drive Phase 3 (plan `plans/module-11-drive-backend.md`): exposed the application core through seven thin controllers + `Contracts/Drive` request records. Enhanced `DriveFileContentResult` with `FileName` and the effective `RangeStart`/`RangeEnd` so the content endpoints serve HTTP Range manually (206 + `Content-Range`, `Accept-Ranges: bytes`). Live-verified on the dev API (port 5028): all metadata routes 200, auth 401, validation 400; file binaries await SeaweedFS keys. OnlyOffice deferred to Phase 4. |
| 2026-11-19 | Qoder | Drive Phase 2 (plan `plans/module-11-drive-backend.md`): the full application core — spaces, items, stars, presigned uploads, download/Range, immutable versions + copy-restore, hierarchical permissions, approval-gated deletion + direct delete, trash/restore/purge, name search, and Drive settings; validators + unified errors + handler tests. Added `IDriveStorageService.CopyAsync`, `IDrivePurgeService`, and the deletion-request reviewer filter. |