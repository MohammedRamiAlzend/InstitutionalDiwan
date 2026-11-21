# `src/widgets/drive-sidebar` — Drive Workspace Rail

> **Status:** Implemented (front-end Phase 1) — browse modes, space switcher, quota meter.

## Purpose

The left rail of `/drive`. Lists the five browse modes, the caller's personal space plus reachable
shared spaces, and the active space's quota meter. Space creation is open to every `DRIVE_USE`
holder; rename and quota actions are permission-filtered per row. The active mode/space is owned by
the page (URL state) and passed in — this widget only renders and emits selections.

## Contents

| File | Purpose |
| --- | --- |
| `ui/DriveSidebar.tsx` | Mode buttons (`my-files`/`shared`/`starred`/`trash`/`deletion`), the spaces list with a per-row overflow menu (rename for owner/ADMIN, quota for ADMIN), the `QuotaMeter` for the active space, and the create/rename/quota dialogs from `features/drive-space-manage`. |
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
| Widget code | ✅ Implemented (`npm run build` and `npm run lint` pass) |
| UI labels | ✅ Renders from `drive.sidebar.*`, `drive.my-files`/`shared-spaces`/`starred`/`trash`, `drive.view.deletion-requests`, `drive.action.*`; no Arabic literals. |
| Live verification | ⏳ Pending the Phase 6 browser round. |

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-11-20 | Qoder | Front-end Phase 1: created the rail with mode switcher, space list + per-row menu, and quota meter. |