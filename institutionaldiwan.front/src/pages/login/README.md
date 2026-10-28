# `src/pages/login` — Login Route Page Slice (`/login`)
## 1. Purpose
Renders the public authentication screen at `/login`: an Arabic RTL two-panel layout (login card + institutional brand panel) hosting the `LoginForm` from `src/features/auth`, with a light/dark theme toggle in the header.
## 2. Strict FSD Rules
- Allowed Imports: `src/features/auth`, `src/entities/*`, and `src/shared/*`.
- Forbidden Imports: NEVER import from `src/app` or sibling `src/pages/*` slices.
- Public API: Export `LoginPage` only via `src/pages/login/index.ts`.
## 3. Contents & Segments
- `ui/LoginPage.tsx` — Brand header + theme toggle, login card (title, form, audit notice) at the inline-start side, and the institutional panel (title, feature list, audit notice) at the inline-end side; redirects after login to `/change-password` when the account must reset its password, otherwise to the requested location or `/`.
- `index.ts` — Public API barrel file.
## 4. Required Packages
- `react-router-dom`, `lucide-react` — ✅ installed
## 5. Implementation Status
- ✅ Implemented and verified in RTL, light and dark themes, with the live API.
## 6. Change Log
- 2026-10-28: Routed reset logins (`mustChangePassword`) to `/change-password` instead of the requested page.
- 2026-10-27: Implemented the page to match the approved light/dark designs; brand panel mirrors automatically in LTR.
- 2026-10-22: Converted this `README.md` from the Word document it was authored in to markdown, so the `README.md`-per-folder rule in `AGENT.md` holds and the content is diffable and agent-editable.
- 2026-10-19: Created `pages/login` slice folder and `README.md`.
