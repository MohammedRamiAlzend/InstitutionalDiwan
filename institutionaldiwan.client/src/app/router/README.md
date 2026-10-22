# institutionaldiwan.client / src/app/router — Application Router & Permission Guards
> **Status:** Scaffolding & Rules — Router and permission guard rules defined; code pending.
## Purpose
Defines the React Router v7 route tree, authentication guard (`ProtectedRoute`), forced password-change redirect (`must_change_password`), and declarative RBAC permission guard (`RequirePermission`) aligned with `InstitutionalDiwan.Domain/Common/Permissions.cs`.
## Contents
| File | Purpose |
| --- | --- |
| `AppRouter.tsx` | Declares public routes (`/login`) and protected `AppShell` routes for all Institutional Diwan modules. |
| `ProtectedRoute.tsx` | Redirects unauthenticated visitors to `/login` and enforces `/change-password` when `mustChangePassword` is true. |
| `RequirePermission.tsx` | Checks whether the authenticated user holds the required `PermissionCode` (e.g. `USER_MANAGE`, `DEPARTMENT_MANAGE`, `DOCUMENT_VIEW`) and renders a 403 Forbidden view when missing. |
| `index.ts` | Public barrel exporting `AppRouter`, `ProtectedRoute`, and `RequirePermission`. |
## Implementation status
| Item | State |
| --- | --- |
| `AppRouter.tsx`, `ProtectedRoute.tsx`, `RequirePermission.tsx` | ❌ Not implemented |
## Change Log
| Date | Author | Change |
| --- | --- | --- |
| 2026-10-22 | opencode | Converted this folder's `README.md` from the Word document it was authored in to markdown, so the `README.md`-per-folder rule in `AGENT.md` holds and the content is diffable and agent-editable. |
| 2026-10-19 | agent | Initialized `src/app/router` segment. |
