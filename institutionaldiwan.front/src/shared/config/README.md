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
| `routes.ts` | Typed builders: `LOGIN`, `CHANGE_PASSWORD`, `DASHBOARD`, `DOCUMENTS`, `DOCUMENT_DETAIL(id)`, `DOCUMENT_DETAIL_PATTERN`, `MOVEMENTS`, `DEPARTMENTS`, `INSTITUTION_SETTINGS`, `USERS`, `ROLES`, `EMPLOYEES`, `NUMBERING`, `AUDIT` (no `/archive` until Module 07). |
| `lookupIds.ts` | Seeded IDs shared across entities: `USER_SCOPE_TYPE` (1–4), `ASSIGNMENT_TYPE` (1–2), `DEPARTMENT_TYPE` (1–5), `NUMBERING_MODE` (Automatic 1 / Manual 2), `NUMBERING_RESET_POLICY` (Yearly 1 / Monthly 2 / Never 3), `DOCUMENT_DIRECTION` (Internal 1 / Incoming 2 / Outgoing 3 / All 4), `DOCUMENT_TYPE` (IncomingLetter 1 / OutgoingLetter 2 / InternalMemo 3 / Circular 4 / Decision 5), `DOCUMENT_STATUS` (Draft 1 / Registered 2 / InMovement 3 / Archived 4 / Canceled 5), `DOCUMENT_CONFIDENTIALITY` (Normal 1 … TopSecret 4), `DOCUMENT_PRIORITY` (Normal 1 / Urgent 2 / Immediate 3), `DOCUMENT_TYPE_DIRECTION` (type → direction pre-fill), `PARTICIPANT_ROLE` (Sender 1 / Receiver 2 / Cc 3 / Signatory 4) with `PARTICIPANT_TARGET_TYPE` (ExternalOrganization 1 / InternalDepartment 2 / Person 3), and the movement catalogs — `MOVEMENT_TYPE` (Referral 1 / Transfer 2 / Cc 3 / Return 4), `MOVEMENT_STATUS` (Draft 1 … Canceled 10), `MOVEMENT_STEP_STATUS` (Pending 1 … Redirected 7), and `MOVEMENT_EVENT_TYPE` (Created 1 … Canceled 9). Must match API seeds. |
| `env.ts` | Typed `import.meta.env` access (`API_BASE_URL` optional; default relative `/api`), `DEFAULT_TIMEZONE = 'Asia/Damascus'`. |
| `index.ts` | Public barrel exporting permissions, routes, environment, and all lookup ID maps, including numbering and the correspondence catalogs. |

## Example `routes.ts` shape

```ts
export const ROUTES = {
  LOGIN: '/login',
  CHANGE_PASSWORD: '/change-password',
  DASHBOARD: '/',
  DOCUMENTS: '/documents',
  DOCUMENT_DETAIL: (id: string) => `/documents/${id}`,
  DOCUMENT_DETAIL_PATTERN: '/documents/:documentId',
  MOVEMENTS: '/movements',
  DEPARTMENTS: '/departments',
  INSTITUTION_SETTINGS: '/institution-settings',
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
| `permissions.ts`, `routes.ts`, `lookupIds.ts`, `env.ts`, `index.ts` | Implemented, including `/institution-settings`, numbering ID maps, and the Module 03 document/participant/movement ID maps; final integration verification pending. |

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-10-29 | agent | Module 03 Phase 6: added the `MOVEMENT_TYPE`, `MOVEMENT_STATUS`, `MOVEMENT_STEP_STATUS`, and `MOVEMENT_EVENT_TYPE` deterministic ID maps (shared by the movement badges, transition rules, and routing dialogs) through the public barrel. |
| 2026-10-28 | agent | Module 03 Phase 3: added `PARTICIPANT_ROLE` and `PARTICIPANT_TARGET_TYPE` deterministic IDs for the participant dialog and details tab, and `DOCUMENT_DETAIL_PATTERN` for the registered `/documents/:documentId` route. |
| --- | --- | --- |
| 2026-10-28 | agent | Module 03 Phase 2: added deterministic document direction/type/status/confidentiality/priority IDs and `DOCUMENT_TYPE_DIRECTION`, exported through the barrel for the registry and create dialog. |
| 2026-10-28 | agent | Document the institution-settings route and numbering mode/reset-policy IDs exposed through the public config barrel for Module 01. |
| 2026-10-27 | agent | Added `lookupIds.ts` (deterministic `UserScopeType`/`AssignmentType`/`DepartmentType` ids) so multiple entity slices share the seeds without sibling imports; verified against the live `/api/lookups` catalogs. |
| 2026-10-27 | agent | Implemented the segment exactly per the root `AGENTS.md` contracts (16 permissions, typed routes, relative API base URL). |
| 2026-10-25 | Cursor | Updated to 16 permissions (added audit); expanded routes for movements/audit; deferred archive route; linked AGENTS.md contract. |
| 2026-10-22 | opencode | Converted this folder's `README.md` from the Word document it was authored in to markdown. |
| 2026-10-19 | agent | Initialized `src/shared/config` segment and documented canonical permission and route constants. |
