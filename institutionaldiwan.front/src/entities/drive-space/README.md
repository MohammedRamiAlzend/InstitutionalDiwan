# `src/entities/drive-space` — Drive Space Read Model (Module 11)

> **Status:** ✅ Reads implemented (2026-11-20, Module 11 front-end Phase 0) — spaces overview,
> one space, institution Drive settings (opt-in), query keys/hooks, and the `QuotaMeter`. No page
> consumes it yet; the shell/sidebar and quota counter land in Phase 1. Writes live in
> `features/drive-space-manage` (later phases).

## Purpose

Read models and queries for Module 11 «الملفات» spaces: the caller's personal space plus the shared
spaces they can reach, one space's effective quota and usage, and the institution Drive defaults.
Drive is independent of document archiving (Module 07). Writes (create/rename/quota/ensure-personal/
settings-update) live in `features/drive-space-manage`; the sidebar and quota counter live in
`widgets/app-shell` / the Drive page.

## Contents

| File | Purpose |
| --- | --- |
| `api/driveSpaceApi.ts` | `overview()` → `GET /api/drive/spaces`; `get(spaceId)` → `GET /api/drive/spaces/{id}`; `settings()` → `GET /api/drive/settings` (`DRIVE_MANAGE`). |
| `model/types.ts` | `DriveSpaceResponse`, `DriveSpacesOverviewResponse`, `DriveSettingsResponse` — camelCase mirrors of the Application contracts. |
| `model/queries.ts` | `driveSpaceKeys` factory, `useDriveSpaces`, `useDriveSpace`, `useDriveSettings` (opt-in). |
| `ui/QuotaMeter.tsx` | A used/quota progress bar: percent clamped 0–100, «غير محدود» when `quotaBytes <= 0`, danger fill at 100%. Labels via `useLabel` (`drive.quota.*`); sizes via `formatFileSize` (`dir="ltr"`). |
| `index.ts` | Public barrel — import from `@/entities/drive-space`, never from a subpath. |

## Query keys (contract)

```ts
export const driveSpaceKeys = {
  all: ['drive-spaces'] as const,
  overview: () => [...driveSpaceKeys.all, 'overview'] as const,
  detail: (spaceId: string) => [...driveSpaceKeys.all, 'detail', spaceId] as const,
  settings: () => [...driveSpaceKeys.all, 'settings'] as const,
};
```

Mutations in `features/drive-space-manage` invalidate `driveSpaceKeys.overview()` and
`driveSpaceKeys.detail(spaceId)`; a quota change also invalidates the affected space's item browse
keys (`driveItemKeys.browse`) so usage counters refresh.

## Backend contract notes

- `GET /api/drive/spaces` needs `DRIVE_USE` and returns `{ personal, shared[] }`. `personal` is
  `null` until the caller's personal space is ensured (a write in a later phase); the UI must handle
  the null case rather than assume a space always exists.
- `quotaBytes` is the per-space override (`null` = institution default applies);
  `effectiveQuotaBytes` is the quota actually in force (override ?? default). `usedBytes` counts
  committed bytes across **all** versions and keeps counting trashed items until permanent purge.
- `GET /api/drive/settings` is `DRIVE_MANAGE`-only — a normal user gets `403`. `useDriveSettings`
  therefore defaults to `enabled: false` and is opted into only by an admin surface (Phase 5). Normal
  users pre-check uploads against the client mirror in `shared/config/driveLimits.ts`; the server
  re-checks every rule at `initiate`/`complete` and stays authoritative.
- Enum IDs (`spaceTypeId`) resolve through `DRIVE_SPACE_TYPE` in `shared/config/driveIds.ts`, never
  magic literals.

## Implementation status

| Item | State |
| --- | --- |
| Docs | ✅ |
| Types, API client, query keys/hooks | ✅ Module 11 front-end Phase 0 |
| `QuotaMeter` | ✅ Phase 0; mounted by the shell/sidebar quota counter in Phase 1; labels catalog-driven (`drive.quota.*`). |
| Spaces overview / sidebar / quota UI | ⏳ Phase 1 |
| Writes (create/rename/quota/ensure-personal/settings) | ⏳ `features/drive-space-manage`, later phases |

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-11-20 | Qoder | Module 11 front-end Phase 0 (`plans/module-11-drive-frontend.md`): implemented the read slice — `driveSpaceApi` (overview, by-id, opt-in settings), the three response types, `driveSpaceKeys` with `overview`/`detail`/`settings`, `useDriveSpaces` (30-second `staleTime`, previous-data placeholder), `useDriveSpace` (enabled by id), `useDriveSettings` (disabled until an admin opts in, because the route is `DRIVE_MANAGE`-only), and `QuotaMeter`. No page consumes it yet — the shell/sidebar and quota counter land in Phase 1. |