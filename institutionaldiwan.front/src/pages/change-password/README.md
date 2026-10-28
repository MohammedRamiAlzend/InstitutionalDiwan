# `src/pages/change-password` — Change Password Page Slice (`/change-password`)

> **Status:** Implemented (Module 02 Phase 6, 2026-10-28) — authenticated password change, forced after a reset login and reachable voluntarily from the account menu.

## 1. Purpose

Renders the signed-in password-change screen. It is intentionally outside `widgets/app-shell` so a user who must replace a reset password sees only this task. The page owns the standalone brand header, the theme toggle, the optional forced-change notice, and the post-success redirect; it delegates the form to `features/auth`.

## 2. Strict FSD Rules

- Allowed Imports: `src/features/auth`, `src/entities/user`, and `src/shared/*`.
- Forbidden Imports: NEVER import from `src/app` or sibling `src/pages/*` slices (the login layout is duplicated deliberately, not imported).
- Public API: Export `ChangePasswordPage` only via `src/pages/change-password/index.ts`.

## 3. Contents & Segments

- `ui/ChangePasswordPage.tsx` — brand header + theme toggle; centered card with the page title, an Arabic warning when `mustChangePassword` is set, `<ChangePasswordForm />`, and (only for voluntary changes) a link back to the dashboard. Successful changes navigate to `/` with `replace`.
- `index.ts` — Public API barrel file.

## 4. Required Packages

- `react-router-dom`, `lucide-react` — ✅ installed

## 5. Implementation Status

- ✅ `/change-password` mounted under `ProtectedRoute` outside the shell, with a fresh token and session applied after success.
- ✅ Verified in-browser: empty/mismatch/same-as-old validation, wrong current password (`Identity.CurrentPasswordIncorrect`), successful change, forced redirect (including after a reload via the JWT claim), voluntary entry from the account menu, RTL and dark theme.

## 6. Change Log

- 2026-10-28: Implemented the page and documented the slice.
