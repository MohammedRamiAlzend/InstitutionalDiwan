# institutionaldiwan.front / src/app/router — Application Router & Permission Guards
> **Status:** Implemented — public `/login`, protected standalone `/change-password`, protected routes nested inside `<AppShell />`, permission-component guard, and catch-all redirect.
## Purpose
Defines the React Router v7 route tree, the authentication guard (`ProtectedRoute`), the authenticated shell layout (`AppShell` from `widgets/app-shell`), and the declarative RBAC permission guard (`RequirePermission`) aligned with `InstitutionalDiwan.Domain/Common/Permissions.cs`.
## Contents
| File | Purpose |
| --- | --- |
| `AppRouter.tsx` | Declares `/login` (public), `/change-password` (protected, standalone), and nests `ProtectedRoute` → `AppShell` → page routes (`/` dashboard, `/users`, `/roles`, `/employees`); further module pages mount beside them as their slices land. Includes a catch-all redirect. |
| `ProtectedRoute.tsx` | Redirects visitors without a token to `/login` (remembering the requested location), shows a spinner while `AuthSessionLoader` hydrates the session, and forces `/change-password` when the session flag or the `must_change_password` JWT claim is true. |
| `RequirePermission.tsx` | Re-exports the `features/auth` guard so route-level checks and component-level checks share one implementation (Arabic 403 state). |
| `index.ts` | Public barrel exporting `AppRouter`, `ProtectedRoute`, and `RequirePermission`. |
## Implementation status
| Item | State |
| --- | --- |
| `AppRouter.tsx`, `ProtectedRoute.tsx`, `RequirePermission.tsx` | ✅ Implemented (login, `/change-password`, shell-nested dashboard, `/users`, `/roles`, `/employees`) |
| Forced `/change-password` redirect | ✅ Implemented — session flag or JWT claim, so the redirect survives a reload |
| Full module route map (`/documents`, `/movements`, …) | ❌ Pending their page slices; the shell already exposes their navigation entries as «قريبًا» |
## Change Log
| Date | Author | Change |
| --- | --- | --- |
| 2026-10-28 | agent | Added the standalone `/change-password` route and forced it from the JWT claim (in addition to the session flag) inside `ProtectedRoute`; verified direct-URL access to `/users` redirects back after a reload. |
| 2026-10-28 | agent | Added the `/employees` route (Phase 5) inside the shell; the page performs its own `EMPLOYEE_MANAGE` guard. |
| 2026-10-28 | agent | Added the `/roles` route (Phase 4) inside the shell; the page performs its own `ROLE_MANAGE` guard. |
| 2026-10-28 | agent | Added the `/users` route (Phase 3) inside the shell; the page performs its own `USER_MANAGE` guard. |
| 2026-10-27 | agent | Nested protected page routes under `widgets/app-shell` (Phase 1) so every page gets the sidebar, header, and shell menus; dashboard mounts as the first child. |
| 2026-10-27 | agent | Implemented the router and guards for Identity Module 01; `RequirePermission` now re-exports the single `features/auth` implementation to avoid duplicate guard logic. |
| 2026-10-22 | opencode | Converted this folder's `README.md` from the Word document it was authored in to markdown, so the `README.md`-per-folder rule in `AGENT.md` holds and the content is diffable and agent-editable. |
| 2026-10-19 | agent | Initialized `src/app/router` segment. |
