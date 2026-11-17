# institutionaldiwan.front / src/app/providers — Global React Context & Query Providers
> **Status:** Implemented for Identity Module 01.
## Purpose
Composes all application-level React context providers (`ThemeProvider`, `QueryProvider`, `AuthSessionLoader`, `Toaster`) so the `app` root component remains clean and testable. `ThemeProvider` is the only place in the app that writes the theme to the document; colors themselves live in [`../styles/tokens.css`](../styles/README.md).
## Contents
| File | Purpose |
| --- | --- |
| `AppProviders.tsx` | Wraps the application tree in `ThemeProvider` → `QueryProvider` → `BrandingEffects` (tab title and icon from the stored name and logo) + `AuthSessionLoader` → `LabelsBootGate` → `UiLabelEditProvider` (the inline label editor bridge from `features/ui-label-manage`) + RTL `Toaster`. |
| `ThemeProvider.tsx` | Re-applies the stored `diwan.theme` choice (`light` / `dark` / `system`) to `<html data-theme>` after mount; the toggle lives in `shared/ui/theme-toggle`. |
| `QueryProvider.tsx` | Configures `@tanstack/react-query` `QueryClient` (default `staleTime: 30_000`, retry 0 on `401`/`403`/`404`/`409`, at most one retry otherwise) and lazy-loads the devtools in development only. |
| `AuthSessionLoader.tsx` | Calls `GET /api/auth/me` on initial mount when a token exists, hydrates the session store, and clears a stale token when hydration fails. |
| `LabelsBootGate.tsx` | UI-labels boot gate (UI labels plan, Phases 1–2): reveals the router once the labels store is ready. A returning browser seeds from the `diwan.uiLabels.v1` cache (no screen) and revalidates `GET /api/ui-labels` in the background; a first visit shows a branded loading screen (logo + spinner + «جارٍ تجهيز الواجهة…») while the overrides load in parallel with `AuthSessionLoader`. No token → the sign-in page renders on the defaults; a failed fetch boots on the defaults with a non-blocking warning toast. Also forces inline label-edit mode off for any non-`superadmin` session, so a leftover per-browser preference never leaks editable labels to a normal user. |
| `index.ts` | Public barrel exporting `AppProviders` and the individual providers. |
## Implementation status
| Item | State |
| --- | --- |
| `AppProviders.tsx`, `ThemeProvider.tsx`, `QueryProvider.tsx`, `AuthSessionLoader.tsx`, `index.ts` | ✅ Implemented |
| `LabelsBootGate.tsx` | ✅ Implemented (Phases 1–2: cache-first reveal + superadmin edit-mode guard); browser verification pending manual login. |
## Change Log
| Date | Author | Change |
| --- | --- | --- |
| 2026-11-17 | Qoder | UI labels, Phase 2 (`plans/ui-labels-edit-mode.md`): `AppProviders` mounts `UiLabelEditProvider` (from `features/ui-label-manage`) inside `LabelsBootGate` so every `<Label>` can open the in-place editor; `LabelsBootGate` forces `diwan.labelEdit` off for non-`superadmin` sessions so edit mode never leaks to a normal user. |
| 2026-11-17 | Qoder | UI labels, Phase 1 (`plans/ui-labels-edit-mode.md`): added `LabelsBootGate` (cache-first reveal + branded first-load screen + background revalidation of `GET /api/ui-labels`, defaults + warning on failure) and mounted it inside `AuthSessionLoader` in `AppProviders` so the labels fetch runs in parallel with `/api/auth/me`. |
| 2026-11-12 | Claude | `AppProviders` mounts `BrandingEffects` so the browser tab shows the institution name and logo. |
| 2026-11-05 | Claude | `AuthSessionLoader` publishes the signed-in user id to `setFormDraftScope` so form drafts are stored per user. |
| 2026-10-28 | agent | Documented the forced-change interaction: `/api/auth/me` still omits `mustChangePassword`, so `ProtectedRoute` re-reads the `must_change_password` claim from the stored JWT when the in-memory session does not carry it. |
| 2026-10-27 | agent | Implemented the providers for Identity Module 01; devtools are dev-only lazy chunks so they stay out of the production bundle. `mustChangePassword` is not re-derived from `/api/auth/me` (that response carries no flag) — it is set at login only. |
| 2026-10-25 | Buffy | Added the planned `ThemeProvider` for light/dark/system themes persisted in `diwan.theme`. |
| 2026-10-22 | opencode | Converted this folder's `README.md` from the Word document it was authored in to markdown, so the `README.md`-per-folder rule in `AGENT.md` holds and the content is diffable and agent-editable. |
| 2026-10-19 | agent | Initialized `src/app/providers` segment. |
