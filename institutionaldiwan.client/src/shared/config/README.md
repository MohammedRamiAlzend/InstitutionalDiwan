# institutionaldiwan.client / src/shared/config — Permissions, Routes & Environment Configuration
> **Status:** Scaffolding & Rules — Config rules and permission constants defined; code pending.
## Purpose
Centralizes application constants, route paths, and the canonical permission codes shared with `InstitutionalDiwan.Domain/Common/Permissions.cs` so route guards, sidebar items, and action buttons never use magic strings.
## Contents
| File | Purpose |
| --- | --- |
| `permissions.ts` | Exports `Permissions` constant object with all 14 canonical codes (`DOCUMENT_VIEW`, `DOCUMENT_CREATE`, `DOCUMENT_EDIT`, `DOCUMENT_DELETE`, `DOCUMENT_APPROVE`, `NUMBERING_VIEW`, `NUMBERING_MANAGE`, `DEPARTMENT_MANAGE`, `ROLE_MANAGE`, `USER_MANAGE`, `EMPLOYEE_MANAGE`, `ACCESS_GRANT_MANAGE`, `REPORT_VIEW`, `LOOKUP_MANAGE`) and `PermissionCode` union type. |
| `routes.ts` | Typed route path builders (`ROUTES.LOGIN`, `ROUTES.DOCUMENTS`, `ROUTES.DOCUMENT_DETAIL(id)`, `ROUTES.DEPARTMENTS`, `ROUTES.USERS`, `ROUTES.ROLES`, `ROUTES.EMPLOYEES`, `ROUTES.NUMBERING`, `ROUTES.MOVEMENTS`, `ROUTES.ARCHIVE`, `ROUTES.AUDIT`). |
| `env.ts` | Typed access to `import.meta.env` variables (`API_BASE_URL`, `DEFAULT_TIMEZONE = 'Asia/Damascus'`). |
| `index.ts` | Public barrel exporting `Permissions`, `ROUTES`, and `ENV`. |
## Implementation status
| Item | State |
| --- | --- |
| `permissions.ts`, `routes.ts`, `env.ts` | ❌ Not implemented |
## Change Log
| Date | Author | Change |
| --- | --- | --- |
| 2026-10-22 | opencode | Converted this folder's `README.md` from the Word document it was authored in to markdown, so the `README.md`-per-folder rule in `AGENT.md` holds and the content is diffable and agent-editable. |
| 2026-10-19 | agent | Initialized `src/shared/config` segment and documented canonical permission and route constants. |
