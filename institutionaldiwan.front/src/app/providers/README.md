# institutionaldiwan.front / src/app/providers — Global React Context & Query Providers
> **Status:** Scaffolding & Rules — Provider wiring defined; code pending.
## Purpose
Composes all application-level React context providers (`ThemeProvider`, `QueryClientProvider`, `DirectionProvider`, `AuthSessionLoader`, `Toaster`) so `src/app/index.tsx` remains clean and testable. `ThemeProvider` is the only place that writes the theme to the document; colors themselves live in [`../styles/tokens.css`](../styles/README.md).
## Contents
| File | Purpose |
| --- | --- |
| `AppProviders.tsx` | Wraps the application tree in all global providers in deterministic order. |
| `ThemeProvider.tsx` | Reads `diwan.theme` (`light` / `dark` / `system`), applies `data-theme` to `<html>`, follows `prefers-color-scheme` while in `system`, and exposes the toggle consumed by `widgets/app-shell`. |
| `QueryProvider.tsx` | Configures `@tanstack/react-query` `QueryClient` (default `staleTime: 30_000`, `retry: 1`, no retry on `401`/`403`/`404`/`409`). |
| `AuthSessionLoader.tsx` | Calls `GET /api/auth/me` on initial mount when a token exists in `tokenStorage`, hydrating the user store before protected routes render. |
| `index.ts` | Public barrel exporting `AppProviders`. |
## Implementation status
| Item | State |
| --- | --- |
| `AppProviders.tsx`, `ThemeProvider.tsx`, `QueryProvider.tsx`, `AuthSessionLoader.tsx` | ❌ Not implemented |
## Change Log
| Date | Author | Change |
| --- | --- | --- |
| 2026-10-25 | Buffy | Added the planned `ThemeProvider` for light/dark/system themes persisted in `diwan.theme`. |
| 2026-10-22 | opencode | Converted this folder's `README.md` from the Word document it was authored in to markdown, so the `README.md`-per-folder rule in `AGENT.md` holds and the content is diffable and agent-editable. |
| 2026-10-19 | agent | Initialized `src/app/providers` segment. |
