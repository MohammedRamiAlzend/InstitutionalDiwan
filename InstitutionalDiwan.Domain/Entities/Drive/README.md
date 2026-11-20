# Domain / Entities / Drive

> **Status:** Phase 0 implemented (plan `plans/module-11-drive-backend.md`, 2026-11-19). Entities, errors, events and repository contracts exist and are unit-tested; persistence, handlers and API follow in Phases 1–4.

## Purpose

Module 11 — the institutional file system (Google Drive–style), independent of Module 07 document archiving. A space owns a tree of unified folder/file nodes; files carry immutable content versions stored in SeaweedFS via S3; access is granted per user/department with hierarchical inheritance; deletion is approval-gated by the owner's department head; OnlyOffice edit sessions track collaborative saves.

## Contents

| File | Purpose |
| --- | --- |
| `DriveCatalog.cs` | Structural code constants (not editable lookups): `DriveSpaceType` (1 personal, 2 shared), `DriveItemKind` (1 folder, 2 file), `DriveRole` (1 viewer, 2 editor, 3 manager), `DrivePrincipalType` (1 user, 2 department), `DriveVersionStatus` (1 pending, 2 committed, 3 abandoned), `DriveVersionSource` (1 upload, 2 editor save, 3 restore), `DriveDeletionRequestStatus` (1 pending, 2 approved, 3 rejected, 4 cancelled), `DriveEditSessionStatus` (1 open, 2 saved, 3 closed, 4 expired). |
| `DriveSpace.cs` | Aggregate root: a personal or shared storage space with quota override, atomic `UsedBytes` counter (`AddUsage` enforces the effective quota, `ReleaseUsage` frees bytes), rename, trash/restore (a personal space cannot be trashed). Raises `DriveSpaceCreatedEvent`. |
| `DriveItem.cs` | Aggregate root: one unified tree node (folder or file) with name rules (≤255, no `\ / : * ? " < > \|`, not `.`/`..`), depth limit 30, cycle-checked `Move`, `SetCurrentVersion` (files only, raises `DriveFileVersionCommittedEvent`), break/restore permission inheritance, and trash/restore raising `DriveItemTrashedEvent`/`DriveItemRestoredEvent`. Trashing a folder implicitly trashes its subtree at query level. |
| `DriveFileVersion.cs` | One immutable content version: created `Pending` for a presigned direct upload, `Commit(checksum, actualSize)` verifies and freezes it, `Abandon` cancels a pending upload. Committed versions are never modified; restore creates a new version. References `FileStorageProvider` (S3/SeaweedFS) by id and stores the `ObjectKey`. |
| `DrivePermission.cs` | An explicit grant on an item for a user or department with a viewer/editor/manager role; `ChangeRole` upgrades/downgrades. Effective access is computed by the access evaluator walking ancestors — only explicit grants are stored. |
| `DriveDeletionRequest.cs` | Aggregate root: approval-gated deletion. Requires a reason (≤500); `Approve`/`Reject` (reviewer + optional note ≤500) work once on a pending request and raise `DriveDeletionApprovedEvent`/`DriveDeletionRejectedEvent`; only the requester may `Cancel`. Raises `DriveDeletionRequestedEvent` on creation. |
| `DriveEditSession.cs` | An OnlyOffice editing session: unique `DocumentKey` (≤200) per (item, version), `BaseVersionId` for conflict detection, lifecycle `Open → Touch/MarkSaved → Close` or `Expire` for idle sessions. |
| `DriveStar.cs` | A user's personal favorite marker on an item; never inherited or shared. |

## Conventions / Notes

- Follows the layer rules: factories return `Result<T>`, mutations return `Result`, timestamps take a trailing optional `utcNow`, aggregate roots raise immutable events.
- `DriveSpace` and `DriveItem` expose `byte[]? RowVersion` for optimistic concurrency (EF wiring in Phase 1).
- Quota accounting: trashed items still count toward `UsedBytes` until permanent deletion; the effective quota (space override or institution default) is passed into `AddUsage` by the Application layer.
- Cycle and depth checks on `Move` receive the target's ancestor chain and depth from the caller (repository reads), keeping the domain pure.

## Implementation status

| Item | State |
| --- | --- |
| Entities + invariants + events | ✅ Phase 0, covered by `DriveDomainTests` |
| Errors (`DomainErrors.Drive`) | ✅ |
| Repository contracts (`IDriveRepositories.cs`) | ✅ Defined; implementations in Phase 1 |
| EF configurations / migration | ❌ Phase 1 |
| Application handlers / API | ❌ Phases 2–4 |

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-11-19 | Qoder | Phase 2 of `plans/module-11-drive-backend.md`: entities unchanged; `IDriveDeletionRequestRepository.ListPagedAsync` gained an optional `reviewerUserId` filter so a department head's deletion-request inbox is restricted in SQL to the items they may review. |
| 2026-11-19 | Qoder | Phase 0 of `plans/module-11-drive-backend.md`: created the seven Drive entities, structural constants, `DomainErrors.Drive`, seven domain events, and the repository contracts; added `DRIVE_USE`/`DRIVE_MANAGE` permissions. |