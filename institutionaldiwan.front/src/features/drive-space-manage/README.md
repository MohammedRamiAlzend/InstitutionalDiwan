# `src/features/drive-space-manage` — Drive Space Commands

> **Status:** Implemented (front-end Phase 5) — space writes plus administrator Drive settings.

## Purpose

All Module 11 Drive **space** writes. Reads live in `entities/drive-space`; this slice owns the
mutations and their dialogs. Every mutation invalidates `driveSpaceKeys.all` so the overview and the
touched space's detail refetch. The personal space «ملفاتي» is ensured lazily by the Drive page on
first entry, not by a dialog here.

## Contents

| File | Purpose |
| --- | --- |
| `api/driveSpaceMutations.ts` | `useEnsurePersonalSpaceMutation` (`POST /drive/spaces/personal`), `useCreateSpaceMutation` (`POST /drive/spaces`), `useRenameSpaceMutation` (`PUT /drive/spaces/{id}`), `useSetSpaceQuotaMutation` (`PUT /drive/spaces/{id}/quota`); all invalidate `driveSpaceKeys.all`. |
| `model/types.ts` | Request bodies mirroring `DriveRequests.cs`: `CreateDriveSpaceRequest`, `RenameDriveSpaceRequest`, `SetDriveSpaceQuotaRequest`. |
| `model/schemas.ts` | `spaceNameSchema` (trimmed, ≤ 150 = `DriveSpace.MaxNameLength`), `spaceQuotaSchema` (positive whole amount + unit, empty = institution default), `quotaFormToBytes`, `QUOTA_UNIT_BYTES`. Catalog messages via `t()`. |
| `ui/CreateSpaceDialog.tsx` | Names a new shared space; the institution default quota applies. |
| `ui/RenameSpaceDialog.tsx` | Renames a space (owner or ADMIN), prefilled from the current name. |
| `ui/SpaceQuotaDialog.tsx` | ADMIN per-space quota override; amount + unit (بايت/ك.ب/م.ب/غ.ب), empty restores the default. |
| `ui/DriveSettingsDialog.tsx` | ADMIN institution defaults for quota, maximum file size and allowed extensions. |
| `index.ts` | Public barrel. |

## Permissions

- Ensure-personal, create, rename: `DRIVE_USE`; rename further needs ownership or ADMIN (server-enforced).
- Quota override: `DRIVE_MANAGE` (the sidebar only offers it to ADMIN; a quota below current usage is rejected with 409).

## Implementation status

| Item | State |
| --- | --- |
| Mutations, schemas, dialogs | ✅ Implemented (`npm run build` and `npm run lint` pass) |
| UI labels | ✅ Renders from `drive.dialog.*`, `drive.toast.*`, `drive.validation.*`, `drive.quota`, `common.action.*`, `common.size.*`; no Arabic literals. |
| Live verification | ⏳ Pending the Phase 6 browser round (manual login). |

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-11-21 | Copilot | Completed Phase 5 by adding the ADMIN Drive settings dialog and update mutation. |
| 2026-11-20 | Qoder | Front-end Phase 1: created the space-write slice (ensure/create/rename/quota) with Zod schemas and three dialogs. |