# institutionaldiwan.front / src/app/providers — Global React Context & Query Providers
> **Status:** Implemented for Identity Module 01.
## Purpose
Composes all application-level React context providers (`ThemeProvider`, `QueryProvider`, `AuthSessionLoader`, `Toaster`) so the `app` root component remains clean and testable. `ThemeProvider` is the only place in the app that writes the theme to the document; colors themselves live in [`../styles/tokens.css`](../styles/README.md).
## Contents
| File | Purpose |
| --- | --- |
| `AppProviders.tsx` | Wraps the application tree in `ThemeProvider` → `QueryProvider` → `AuthSessionLoader` + RTL `Toaster`. |
| `ThemeProvider.tsx` | Re-applies the stored `diwan.theme` choice (`light` / `dark` / `system`) to `<html data-theme>` after mount; the toggle lives in `shared/ui/theme-toggle`. |
| `QueryProvider.tsx` | Configures `@tanstack/react-query` `QueryClient` (default `staleTime: 30_000`, retry 0 on `401`/`403`/`404`/`409`, at most one retry otherwise) and lazy-loads the devtools in development only. |
| `AuthSessionLoader.tsx` | Calls `GET /api/auth/me` on initial mount when a token exists, hydrates the session store, and clears a stale token when hydration fails. |
| `index.ts` | Public barrel exporting `AppProviders` and the individual providers. |
## Implementation status
| Item | State |
| --- | --- |
| `AppProviders.tsx`, `ThemeProvider.tsx`, `QueryProvider.tsx`, `AuthSessionLoader.tsx`, `index.ts` | ✅ Implemented |
## Change Log
| Date | Author | Change |
| --- | --- | --- |
| 2026-10-27 | agent | Implemented the providers for Identity Module 01; devtools are dev-only lazy chunks so they stay out of the production bundle. `mustChangePassword` is not re-derived from `/api/auth/me` (that response carries no flag) — it is set at login only. |
| 2026-10-25 | Buffy | Added the planned `ThemeProvider` for light/dark/system themes persisted in `diwan.theme`. |
| 2026-10-22 | opencode | Converted this folder's `README.md` from the Word document it was authored in to markdown, so the `README.md`-per-folder rule in `AGENT.md` holds and the content is diffable and agent-editable. |
| 2026-10-19 | agent | Initialized `src/app/providers` segment. |
