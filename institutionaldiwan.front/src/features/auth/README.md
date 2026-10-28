# `src/features/auth` — Authentication & Session Management Feature Slice
## 1. Purpose
Implements user login, signed-in password change, session bootstrapping (`/api/auth/me` via `entities/user`), logout, and the permission guard for the Institutional Diwan client.
## 2. Strict FSD Rules
- Allowed Imports: `src/entities/user`, `src/entities/role`, and `src/shared/*` (`@/shared/api`, `@/shared/ui`, `@/shared/lib`, `@/shared/config`).
- Forbidden Imports: NEVER import from `src/app`, `src/pages`, `src/widgets`, or any sibling `src/features/*` slice.
- Public API: Export `LoginForm`, `ChangePasswordForm`, `LogoutButton`, `useLoginMutation`, `useChangePasswordMutation`, `useLogout`, `useCurrentUserQuery` (alias of `useMeQuery`), and `RequirePermission` only via `src/features/auth/index.ts`.
## 3. Contents & Segments
- `ui/LoginForm.tsx` — Validated Arabic RTL login form (`react-hook-form` + `zod`), password visibility toggle, inline Arabic error from the API; reports the `LoginResponse` so the page can route a forced password change.
- `ui/ChangePasswordForm.tsx` — Current/new/confirm password fields with per-field visibility toggles, Arabic Zod validation (8–200 chars, differing from the current password, matching confirmation), inline Arabic API error, and a success toast.
- `model/change-password.schema.ts` — `changePasswordSchema`, `ChangePasswordFormValues`, and the server-shaped `ChangePasswordRequest` (`oldPassword` / `newPassword`).
- `api/use-change-password.ts` — `POST /api/auth/change-password`; the server returns a fresh JWT, so the token and session are replaced on success.
- `ui/LogoutButton.tsx` — Thin button over `useLogout`; used by the login/dashboard surfaces.
- `model/use-logout.ts` — Single teardown path shared by `LogoutButton` and the shell account menu: clears the token and session store, removes cached user queries, and redirects to `/login` (replace).
- `ui/RequirePermission.tsx` — Renders children only when the session holds the permission; otherwise an Arabic 403 state (`ForbiddenState`).
- `model/login.schema.ts` — Zod schema plus `LoginRequest` / `LoginResponse` contracts.
- `api/use-login.ts` — TanStack Query `useMutation` calling `POST /api/auth/login`, persisting the token and hydrating the session store.
- `index.ts` — Public API barrel file.
## 4. Required Packages
- `@tanstack/react-query`, `react-hook-form`, `zod`, `@hookform/resolvers`, `zustand`, `lucide-react` — ✅ installed
## 5. Implementation Status
- ✅ `LoginForm`, `useLoginMutation`, `useLogout`, `LogoutButton`, `RequirePermission` implemented and verified against the live API; the app-shell account menu consumes `useLogout`.
- ✅ `ChangePasswordForm`, `useChangePasswordMutation`, and the forced/voluntary change-password flow are implemented and verified in-browser (client rules, wrong current password, success with token refresh, dark theme).
- ❌ The register form remains out of scope for the Identity admin module.
## 6. Change Log
- 2026-10-28: Implemented the signed-in password change (Zod schema, mutation, form) and made `LoginForm` hand the session payload to the page so reset logins route to `/change-password`.
- 2026-10-27: Extracted `use-logout.ts` so the shell and `LogoutButton` share one session teardown; exported `useLogout` from the slice barrel.
- 2026-10-27: Implemented the login slice (form, mutation, logout, permission guard) with Arabic error messages via the shared `toArabicErrorMessage` catalog.
- 2026-10-22: Converted this `README.md` from the Word document it was authored in to markdown, so the `README.md`-per-folder rule in `AGENT.md` holds and the content is diffable and agent-editable.
- 2026-10-19: Created `features/auth` slice folder and `README.md`.
