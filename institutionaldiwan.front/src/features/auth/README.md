# `src/features/auth` — Authentication & Session Management Feature Slice
## 1. Purpose
Implements user login, session bootstrapping (`/api/auth/me`), logout, and route-level permission guard actions for the Institutional Diwan client.
## 2. Strict FSD Rules
- Allowed Imports: `src/entities/user`, `src/entities/role`, and `src/shared/*` (`@/shared/api`, `@/shared/ui`, `@/shared/lib`, `@/shared/config`).
- Forbidden Imports: NEVER import from `src/app`, `src/pages`, `src/widgets`, or any sibling `src/features/*` slice.
- Public API: Export `LoginForm`, `LogoutButton`, `useLoginMutation`, `useCurrentUserQuery`, and `RequirePermission` only via `src/features/auth/index.ts`.
## 3. Planned Contents & Segments
- `ui/LoginForm.tsx` — Validated Arabic RTL login form (`react-hook-form` + `zod`).
- `ui/LogoutButton.tsx` — Clears JWT token and resets TanStack Query cache.
- `ui/RequirePermission.tsx` — Wrapper/guard checking user permissions against `PERMISSIONS`.
- `model/login.schema.ts` — Zod validation schema for `LoginRequestDto`.
- `api/use-login.ts` — TanStack Query `useMutation` calling `POST /api/auth/login`.
- `index.ts` — Public API barrel file.
## 4. Required Packages
- `@tanstack/react-query`, `react-hook-form`, `zod`, `@hookform/resolvers`, `zustand`, `lucide-react`
## 5. Implementation Status
- Slice folder and FSD rules established
- Implement `LoginForm` and `useLoginMutation`
## 6. Change Log
- 2026-10-22: Converted this `README.md` from the Word document it was authored in to markdown, so the `README.md`-per-folder rule in `AGENT.md` holds and the content is diffable and agent-editable.
- 2026-10-19: Created `features/auth` slice folder and `README.md`.
