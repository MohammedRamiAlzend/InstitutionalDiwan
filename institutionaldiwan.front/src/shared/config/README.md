# institutionaldiwan.front / src/shared/config — Permissions, Routes & Environment Configuration

> **Status:** Scaffolding & Rules — Config rules and permission constants defined; code pending.

## Purpose

Centralizes application constants, route paths, and the canonical permission codes shared with
`InstitutionalDiwan.Domain/Common/Permissions.cs` so route guards, sidebar items, and action buttons
never use magic strings.

## Contents (planned)

| File | Purpose |
| --- | --- |
| `permissions.ts` | Exports `Permissions` with all **16** codes (incl. `AUDIT_VIEW`, `AUDIT_MANAGE`), `PermissionCode`, and `AllPermissions`. Exact shape is specified in root `AGENTS.md`. |
| `routes.ts` | Typed builders: `LOGIN`, `DASHBOARD`, `DOCUMENTS`, `DOCUMENT_DETAIL(id)`, `MOVEMENTS`, `DEPARTMENTS`, `USERS`, `ROLES`, `EMPLOYEES`, `NUMBERING`, `AUDIT` (no `/archive` until Module 07). |
| `env.ts` | Typed `import.meta.env` access (`API_BASE_URL` optional; default relative `/api`), `DEFAULT_TIMEZONE = 'Asia/Damascus'`. |
| `index.ts` | Public barrel exporting `Permissions`, `ROUTES`, and `ENV`. |

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
| `permissions.ts`, `routes.ts`, `env.ts` | ❌ Not implemented |

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-10-25 | Cursor | Updated to 16 permissions (added audit); expanded routes for movements/audit; deferred archive route; linked AGENTS.md contract. |
| 2026-10-22 | opencode | Converted this folder's `README.md` from the Word document it was authored in to markdown. |
| 2026-10-19 | agent | Initialized `src/shared/config` segment and documented canonical permission and route constants. |
