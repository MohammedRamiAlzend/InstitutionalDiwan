# institutionaldiwan.client / src/features — User Interactions & Business Mutations Layer
> **Status:** Scaffolding & Rules — FSD feature slices mapped to backend CQRS Commands; code pending.
## Purpose
`src/features/` encapsulates user-facing actions and state-changing business workflows that deliver value to an Institutional Diwan user. In our architecture, every backend CQRS `Command` (`POST`, `PUT`, `DELETE`, `PATCH`) maps to a slice in `src/features/`—including its Zod validation schema (mirroring backend FluentValidation rules), TanStack Query `useMutation` hook, RFC 7807 `ProblemDetails` error handling (`400`, `403`, `409 Conflict`), and form/dialog UI.
## Layer & Import Rules (FSD Strict Hierarchy)
`features` sits below `widgets` and above `entities` (app → pages → widgets → features → entities → shared):
- Allowed imports: `@/entities/*`, `@/shared/*` (only via public `index.ts` barrels).
- Forbidden imports: Cannot import from `@/app/*`, `@/pages/*`, `@/widgets/*`, or any other slice in `@/features/*`. If two features must coordinate, compose them inside a `widget` or `page`.
- Internal segment layout per feature slice (`features/<slice>/`):
   - `ui/` — Form, modal dialog, or action button components.
   - `model/` — Zod validation schemas, form types, and `useMutation` hooks (invalidating the relevant `@/entities/*` query keys on success).
   - `api/` — Typed `POST`/`PUT`/`DELETE` request functions calling `@/shared/api`.
   - `index.ts` — Public barrel exporting only the UI triggers/forms and necessary hooks.
## Required Packages
| Package | Version / Role | Used in `src/features/` for |
| --- | --- | --- |
| `react-hook-form` | `^7.x` | Declarative form state and field registration |
| `zod` + `@hookform/resolvers` | `^3.x` / `^5.x` | Client-side validation mirroring backend `FluentValidation` rules |
| `@tanstack/react-query` | `^5.x` | `useMutation` + `queryClient.invalidateQueries` |
| `sonner` | `^2.x` | Success/error feedback toasts displaying Arabic messages & RFC 7807 `ProblemDetails` |
## Planned Feature Slices & Backend Command Mapping
| Slice Folder | Backend Endpoints & CQRS Commands | Key Domain Rules & Error Handling | State |
| --- | --- | --- | --- |
| `auth/` | `POST /api/auth/login`, `POST /api/auth/register`, `POST /api/auth/change-password` | Stores JWT, decodes `must_change_password` / `permission` claims; maps `401` (`Identity.InvalidCredentials`) and `403` (`Identity.UserInactive`). | ❌ Planned |
| `document-create/` | `POST /api/documents` (`CreateDocumentCommand`) | Validates title (≤500), `DocumentTypeId`, `DirectionId`, `OfficialNumberText` (≤100), `NumberingYear` (`[1900, 2200]`); surfaces `409 Conflict` (`Numbering.OfficialNumberTaken`) inline on the official number field. | ❌ Planned |
| `document-access-grant/` | `POST /api/documents/{id}/access-grants`, `DELETE .../{grantId}` | Requires `ACCESS_GRANT_MANAGE`; validates future `ExpiresAt` and `AccessLevelId` (`VIEW=1`, `DOWNLOAD=2`, `FORWARD=3`, `MANAGE=4`). | ❌ Planned |
| `department-manage/` | `POST /api/departments`, `PUT /api/departments/{id}`, `PUT /api/departments/{id}/parent`, `DELETE`, `POST /activate` | Requires `DEPARTMENT_MANAGE`; handles `409 Conflict` on duplicate code (`Department.CodeAlreadyExists`), hierarchy cycles (`Department.CyclicHierarchy`), or deactivating with active children (`Department.HasActiveChildren`). | ❌ Planned |
| `user-manage/` | `/api/users` (create, update, activate, deactivate, assign/revoke role) + `/api/employees` CRUD | Requires `USER_MANAGE` / `EMPLOYEE_MANAGE`; handles `409 Conflict` on duplicate username (`Identity.UsernameTaken`) or duplicate role (`Identity.RoleAlreadyAssigned`). | ❌ Planned |
| `role-manage/` | `/api/roles` (create, update, delete, activate, grant/revoke permission) | Requires `ROLE_MANAGE`; blocks deactivating system roles (`403 Identity.SystemRoleProtected`). | ❌ Planned |
| `movement-routing/` | Planned Module 05 endpoints (send, receive, complete, reject, return, redirect) | Enforces Central Diwan routing hub rules and mandatory rejection/return reasons. | ❌ Planned |
| `attachment-upload/` | Planned Module 06 endpoints (upload file version, soft delete) | Validates allowed extensions/size and computes/displays SHA-256 checksum. | ❌ Planned |
## Conventions / Vibe-Coding Notes (OpenCode & Freebuff)
- Always map RFC 7807 `ProblemDetails` (`title` = `Error.Code`, `detail` = `Error.Description`, `status` = HTTP code): Never show generic `"Something went wrong"` when the server returns a structured domain error like `Numbering.OfficialNumberTaken` or `Department.CyclicHierarchy`.
- Lookup IDs are integers: Payloads send integer lookup IDs (`documentTypeId`, `directionId`, `departmentTypeId`, `scopeTypeId`, `accessLevelId`), never magic strings.
- Golden Rule: Update this `README.md` and the touched feature slice's `README.md` in the same turn whenever files change.
## Implementation status
| Feature Slice | State |
| --- | --- |
| `auth/`, `document-create/`, `document-access-grant/`, `department-manage/`, `user-manage/`, `role-manage/` | ❌ Not implemented (Backend APIs live) |
| `movement-routing/`, `attachment-upload/` | ❌ Not implemented (Awaiting backend endpoints) |
## Change Log
| Date | Author | Change |
| --- | --- | --- |
| 2026-10-22 | opencode | Converted this folder's `README.md` from the Word document it was authored in to markdown, so the `README.md`-per-folder rule in `AGENT.md` holds and the content is diffable and agent-editable. |
| 2026-10-19 | agent | Initialized `src/features` FSD layer folder and mapped slices to backend CQRS commands and error codes. |
