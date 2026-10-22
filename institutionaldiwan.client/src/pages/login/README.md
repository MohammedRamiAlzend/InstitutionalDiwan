# `src/pages/login` — Login Route Page Slice (`/login`)
## 1. Purpose
Renders the public authentication screen at `/login`, hosting the `LoginForm` from `src/features/auth` inside a clean institutional Arabic RTL layout.
## 2. Strict FSD Rules
- Allowed Imports: `src/features/auth`, `src/entities/*`, and `src/shared/*`.
- Forbidden Imports: NEVER import from `src/app` or sibling `src/pages/*` slices.
- Public API: Export `LoginPage` only via `src/pages/login/index.ts`.
## 3. Planned Contents & Segments
- `ui/LoginPage.tsx` — Centered institutional branding card and `LoginForm`.
- `index.ts` — Public API barrel file.
## 4. Required Packages
- `react-router-dom`, `lucide-react`
## 5. Implementation Status
- Slice folder and FSD rules established
- Implement `LoginPage`
## 6. Change Log
- 2026-10-22: Converted this `README.md` from the Word document it was authored in to markdown, so the `README.md`-per-folder rule in `AGENT.md` holds and the content is diffable and agent-editable.
- 2026-10-19: Created `pages/login` slice folder and `README.md`.
