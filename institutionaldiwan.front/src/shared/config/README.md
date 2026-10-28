# institutionaldiwan.front / src/shared/config — Permissions, Routes & Environment Configuration

> **Status:** Implemented (permissions, routes, lookup ids, env).

## Purpose

Centralizes application constants, route paths, and the canonical permission codes shared with
`InstitutionalDiwan.Domain/Common/Permissions.cs` so route guards, sidebar items, and action buttons
never use magic strings.

## Contents

| File | Purpose |
| --- | --- |
| `permissions.ts` | Exports `Permissions` with all **16** codes (incl. `AUDIT_VIEW`, `AUDIT_MANAGE`), `PermissionCode`, and `AllPermissions`. Exact shape is specified in root `AGENTS.md`. |
| `routes.ts` | Typed builders: `LOGIN`, `DASHBOARD`, `DOCUMENTS`, `DOCUMENT_DETAIL(id)`, `MOVEMENTS`, `DEPARTMENTS`, `USERS`, `ROLES`, `EMPLOYEES`, `NUMBERING`, `AUDIT` (no `/archive` until Module 07). |
| `lookupIds.ts` | Deterministic seeded ids shared by multiple entities: `USER_SCOPE_TYPE` (1–4), `ASSIGNMENT_TYPE` (1–2), `DEPARTMENT_TYPE` (1–5). Must match the API seeds. |
| `env.ts` | Typed `import.meta.env` access (`API_BASE_URL` optional; default relative `/api`), `DEFAULT_TIMEZONE = 'Asia/Damascus'`. |
| `index.ts` | Public barrel exporting `Permissions`, `ROUTES`, `ENV`, and the lookup id maps. |

## Example `routes.ts` shape

```ts
export const ROUTES = {
  LOGIN: '/login',
  CHANGE_PASSWORD: '/change-password',
  DASHBOARD: '/',
  DOCUMENTS: '/documents',
  DOCUMENT_DETAIL: (id: string) => `/documents/${id}`,
  MOVEMENTS: '/movements',
  DEPARTMENTS: '/departments',
  USERS: '/users',
  ROLES: '/roles',
  EMPLOYEES: '/employees',
  NUMBERING: '/numbering',
  AUDIT: '/audit',
} as const;
```

## Implementation status

| Item | State |
| --- | --- |
| `permissions.ts`, `routes.ts`, `lookupIds.ts`, `env.ts`, `index.ts` | ✅ Implemented |

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-10-27 | agent | Added `lookupIds.ts` (deterministic `UserScopeType`/`AssignmentType`/`DepartmentType` ids) so multiple entity slices share the seeds without sibling imports; verified against the live `/api/lookups` catalogs. |
| 2026-10-27 | agent | Implemented the segment exactly per the root `AGENTS.md` contracts (16 permissions, typed routes, relative API base URL). |
| 2026-10-25 | Cursor | Updated to 16 permissions (added audit); expanded routes for movements/audit; deferred archive route; linked AGENTS.md contract. |
| 2026-10-22 | opencode | Converted this folder's `README.md` from the Word document it was authored in to markdown. |
| 2026-10-19 | agent | Initialized `src/shared/config` segment and documented canonical permission and route constants. |
