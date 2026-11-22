# `src/widgets/drive-sidebar` — Drive Workspace Rail

> **Status:** Implemented (front-end Phase 1) — browse modes, space switcher, quota meter.

## Purpose

The navigation panel of `/drive` (right side in the Arabic RTL workspace). Three blocks: quick views
(المفضلة، سلة المحذوفات، طلبات الحذف), the caller's personal space plus reachable shared spaces with
the active space's quota meter, and the workspace settings entry (`DRIVE_MANAGE`). The personal space
is the single «ملفاتي» entry — it is not repeated as a separate view row. Space creation is open to
every `DRIVE_USE` holder; rename and quota actions are permission-filtered per row. The active
mode/space is owned by the page (URL state) and passed in — this widget only renders and emits
selections.

## Contents

| File | Purpose |
| --- | --- |
| `ui/DriveSidebar.tsx` | Quick views (`starred`/`trash`/`deletion`) as compact 36px rows, the spaces list (the personal space is `my-files`; shared rows select `shared` + space) with a hover-revealed overflow menu (rename for owner/ADMIN, quota for ADMIN), the `QuotaMeter` for the active space inline under the list, a bottom settings row that mounts `DriveSettingsDialog`, and the create/rename/quota dialogs from `features/drive-space-manage`. |
| `index.ts` | Public barrel. |

## Props

`personal`, `shared`, `isLoading` (from the page's `useDriveSpaces`), `view`, `spaceId`,
`onSelectView`, `onSelectSpace`. The widget reads the session (`useSessionStore`) only to gate the
rename/quota menu entries.

## Guide regions

`drive.spaces` (the space list) and `drive.quota` (the meter); the page wraps the whole rail in
`drive.sidebar`.

## Implementation status

| Item | State |
| --- | --- |
| Widget code | ✅ Implemented as a flat three-block panel (quick views → spaces + inline quota → settings) with compact rows, a single «ملفاتي» entry, and a hover/focus-revealed row menu; its placement mirrors naturally in LTR. |
| UI labels | ✅ Renders from `drive.sidebar.*`, `drive.my-files`/`shared-spaces`/`starred`/`trash`, `drive.view.deletion-requests`, `drive.action.*`; no Arabic literals. |
| Live verification | ⏳ Pending the Phase 6 browser round. |

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-11-22 | Codebuff | Restructured the rail: three blocks instead of five mixed ones, the personal space no longer duplicated as both view and space row, the quota meter sits inline under the space list, rows shrank to 36px with a hover/focus-revealed menu, and workspace settings moved here from the page header. |
| 2026-11-22 | Manus | Refined the active view surface and separated the space list from primary navigation to match the reference layout. |
| 2026-11-22 | Manus | Refined the rail's spacing, active view state, and storage meter surface using semantic theme tokens. |
| 2026-11-20 | Qoder | Front-end Phase 1: created the rail with mode switcher, space list + per-row menu, and quota meter. |
