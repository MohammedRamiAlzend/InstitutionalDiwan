# institutionaldiwan.front / src/app/router — Application Router & Permission Guards
> **Status:** Implemented for Identity Module 01 — public `/login`, protected `/` (dashboard), permission-component guard, and catch-all redirect.
## Purpose
Defines the React Router v7 route tree, the authentication guard (`ProtectedRoute`), and the declarative RBAC permission guard (`RequirePermission`) aligned with `InstitutionalDiwan.Domain/Common/Permissions.cs`.
## Contents
| File | Purpose |
| --- | --- |
| `AppRouter.tsx` | Declares `/login` (public), the protected `/` dashboard route, and a catch-all redirect; additional module routes are added as their pages land. |
| `ProtectedRoute.tsx` | Redirects visitors without a token to `/login` (remembering the requested location) and shows a spinner while `AuthSessionLoader` hydrates the session. |
| `RequirePermission.tsx` | Re-exports the `features/auth` guard so route-level checks and component-level checks share one implementation (Arabic 403 state). |
| `index.ts` | Public barrel exporting `AppRouter`, `ProtectedRoute`, and `RequirePermission`. |
## Implementation status
| Item | State |
| --- | --- |
| `AppRouter.tsx`, `ProtectedRoute.tsx`, `RequirePermission.tsx` | ✅ Implemented (login + dashboard routes) |
| Forced `/change-password` redirect | ❌ Pending — no change-password page yet; `mustChangePassword` is captured in the session store |
| Full module route map (`/documents`, `/movements`, …) | ❌ Pending their page slices |
## Change Log
| Date | Author | Change |
| --- | --- | --- |
| 2026-10-27 | agent | Implemented the router and guards for Identity Module 01; `RequirePermission` now re-exports the single `features/auth` implementation to avoid duplicate guard logic. |
| 2026-10-22 | opencode | Converted this folder's `README.md` from the Word document it was authored in to markdown, so the `README.md`-per-folder rule in `AGENT.md` holds and the content is diffable and agent-editable. |
| 2026-10-19 | agent | Initialized `src/app/router` segment. |
