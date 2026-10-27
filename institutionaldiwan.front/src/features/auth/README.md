# `src/features/auth` — Authentication & Session Management Feature Slice
## 1. Purpose
Implements user login, session bootstrapping (`/api/auth/me` via `entities/user`), logout, and the permission guard for the Institutional Diwan client.
## 2. Strict FSD Rules
- Allowed Imports: `src/entities/user`, `src/entities/role`, and `src/shared/*` (`@/shared/api`, `@/shared/ui`, `@/shared/lib`, `@/shared/config`).
- Forbidden Imports: NEVER import from `src/app`, `src/pages`, `src/widgets`, or any sibling `src/features/*` slice.
- Public API: Export `LoginForm`, `LogoutButton`, `useLoginMutation`, `useCurrentUserQuery` (alias of `useMeQuery`), and `RequirePermission` only via `src/features/auth/index.ts`.
## 3. Contents & Segments
- `ui/LoginForm.tsx` — Validated Arabic RTL login form (`react-hook-form` + `zod`), password visibility toggle, inline Arabic error from the API.
- `ui/LogoutButton.tsx` — Clears the JWT token, session store, and TanStack Query cache, then returns to `/login`.
- `ui/RequirePermission.tsx` — Renders children only when the session holds the permission; otherwise an Arabic 403 state (`ForbiddenState`).
- `model/login.schema.ts` — Zod schema plus `LoginRequest` / `LoginResponse` contracts.
- `api/use-login.ts` — TanStack Query `useMutation` calling `POST /api/auth/login`, persisting the token and hydrating the session store.
- `index.ts` — Public API barrel file.
## 4. Required Packages
- `@tanstack/react-query`, `react-hook-form`, `zod`, `@hookform/resolvers`, `zustand`, `lucide-react` — ✅ installed
## 5. Implementation Status
- ✅ `LoginForm`, `useLoginMutation`, `LogoutButton`, `RequirePermission` implemented and verified against the live API.
- ❌ Change-password and register forms are not part of this slice yet.
## 6. Change Log
- 2026-10-27: Implemented the login slice (form, mutation, logout, permission guard) with Arabic error messages via the shared `toArabicErrorMessage` catalog.
- 2026-10-22: Converted this `README.md` from the Word document it was authored in to markdown, so the `README.md`-per-folder rule in `AGENT.md` holds and the content is diffable and agent-editable.
- 2026-10-19: Created `features/auth` slice folder and `README.md`.
