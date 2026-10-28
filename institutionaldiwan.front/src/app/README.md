# institutionaldiwan.front / src/app — Application Initialization & Global Providers Layer
> **Status:** Implemented — `index.tsx` composes `AppProviders` + `AppRouter`; theme, query, and session hydration are wired; `/login` is public, `/change-password` is protected but standalone, and every other protected page renders inside the `widgets/app-shell` layout (Identity admin complete, 2026-10-28).
## Purpose
`src/app/` is the top-most layer in Feature-Sliced Design (FSD). It initializes the Institutional Diwan React SPA, wires global providers (TanStack Query, React Router, Auth/Session hydration, RTL/Theme direction, Toast notifications, SignalR connection lifecycle), defines route trees and permission guards, and loads global stylesheets. It contains no domain-specific business forms or tables—only application-wide wiring.
## Layer & Import Rules (FSD Strict Hierarchy)
`app` sits at the top of the FSD hierarchy (app → pages → widgets → features → entities → shared):
- Allowed imports: `@/pages/*`, `@/widgets/*`, `@/features/*`, `@/entities/*`, `@/shared/*`.
- Forbidden imports: Nothing in `pages`, `widgets`, `features`, `entities`, or `shared` may ever import from `@/app/*`.
- Segment organization: `app` does not have business slices; it is organized directly into technical segments (`providers/`, `router/`, `styles/`).
## Required Packages
| Package | Version / Role | Used in `src/app/` for |
| --- | --- | --- |
| `react-router-dom` (or `react-router` v7) | `^7.x` | `AppRouter.tsx`, `ProtectedRoute.tsx`, `RequirePermission.tsx`, lazy route loading |
| `@tanstack/react-query` | `^5.x` | `QueryProvider.tsx` (`QueryClient` with default staleTime, retry, and 401/403 global error hooks) |
| `@tanstack/react-query-devtools` | `^5.x` (dev) | Mounted in `QueryProvider.tsx` during development |
| `tailwindcss` + `@tailwindcss/vite` | `^4.x` | Global CSS & RTL utility classes in `styles/index.css` |
| `sonner` | `^2.x` | Global RTL-aware toast notifications (`<Toaster dir="rtl" position="top-left" />`) |
## Contents
| File / Segment | Purpose |
| --- | --- |
| `index.tsx` | Root `<App />` component composing `AppProviders` and `AppRouter`; exported for `src/main.tsx`. |
| `providers/AppProviders.tsx` | Composes `ThemeProvider`, `QueryProvider`, `DirectionProvider` (`dir="rtl"` default for Arabic), `AuthSessionLoader`, and `Toaster`. |
| `providers/ThemeProvider.tsx` | Applies `data-theme` (`light` / `dark` / `system`), persists `diwan.theme`, listens to `prefers-color-scheme`, and exposes the theme toggle. |
| `providers/QueryProvider.tsx` | Configures singleton `QueryClient` and wraps children in `QueryClientProvider`. |
| `providers/AuthSessionLoader.tsx` | Hydrates current user session on startup via `GET /api/auth/me` when a JWT exists in storage. |
| `router/AppRouter.tsx` | Declares the public `/login` route, the standalone `/change-password` route, and nests `ProtectedRoute` → `AppShell` around the dashboard and admin pages (`/users`, `/roles`, `/employees`); remaining module pages land as children of the shell route. `/archive` stays out until Module 07 exists. |
| `router/ProtectedRoute.tsx` | Redirects unauthenticated users to `/login` and forces `/change-password` when the session flag or the `must_change_password` JWT claim is true (the claim survives a reload, since `/auth/me` carries no flag). |
| `router/RequirePermission.tsx` | Route/component guard using the full 16-code `Permissions` set (incl. `AUDIT_VIEW` / `AUDIT_MANAGE`). |
| `styles/tokens.css` | **Single source of color**: raw Forest / Golden Wheat / Charcoal palette, semantic light/dark tokens, and functional status colors. |
| `styles/index.css` | Tailwind CSS v4 entry, imports `tokens.css`, Arabic font declarations (`Cairo` / `Readex Pro`), base RTL/LTR rules, and print tokens. |
## Conventions / Vibe-Coding Notes (OpenCode & Freebuff)
- Always enforce `dir="rtl"` and `lang="ar"` on `<html>` by default (with optional toggle to `ltr`/`en` since lookups carry both `DescAr` and `Desc`).
- Every screen must work in light **and** dark mode. Colors come only from `styles/tokens.css`; never hardcode hex values in a component.
- Permission policy parity: Every route guard in `router/RequirePermission.tsx` must use the exact permission strings from `@/shared/config/permissions` matching `InstitutionalDiwan.Domain/Common/Permissions.cs`.
- Golden Rule: Update this `README.md` (and any subfolder `README.md`) in the same turn whenever files are added, renamed, or deleted.
## Implementation status
| Item | State |
| --- | --- |
| `index.tsx` | ✅ Implemented — composes `AppProviders` and `AppRouter`; mounted by `src/main.tsx` |
| `providers/` (`AppProviders`, `ThemeProvider`, `QueryProvider`, `AuthSessionLoader`) | ✅ Implemented |
| `router/AppRouter` | ✅ Implemented — `/login` public; `/change-password` standalone under `ProtectedRoute`; `/`, `/users`, `/roles`, `/employees` render inside `AppShell`; remaining routes land with their slices |
| `styles/` (Tailwind v4, `tokens.css`, RTL + light/dark theme tokens, density scale) | ✅ Implemented |
| Forced `/change-password` redirect when `mustChangePassword` | ✅ Implemented — session flag or JWT claim; verified after a reload |
## Change Log
| Date | Author | Change |
| --- | --- | --- |
| 2026-10-28 | agent | Added the standalone `/change-password` route and made `ProtectedRoute` force it from the JWT claim as well as the session flag, closing the reload bypass; reset logins now land there directly. |
| 2026-10-28 | agent | Registered the `/users`, `/roles`, and `/employees` admin routes (Phases 3–5) as shell children. |
| 2026-10-27 | agent | Nested protected routes under the implemented `widgets/app-shell` (Phase 1): the dashboard is now a shell child and receives sidebar/header/settings chrome; documented the shell's ownership of density and theme controls. |
| 2026-10-27 | agent | Implemented the layer for Identity Module 01: `index.tsx`, `providers` (theme/query/session loader + Toaster), `router` (login + protected dashboard, permission guard re-export), and `styles` (tokens + Tailwind v4 entry). The `mustChangePassword` redirect is deferred until the change-password page exists. |
| 2026-10-25 | Buffy | Added `ThemeProvider` and `styles/tokens.css` to the planned wiring; documented that every screen supports light/dark with colors only from the token file. |
| 2026-10-25 | Cursor | Wired route map to live modules (audit in, archive deferred); RequirePermission covers all 16 permission codes. |
| 2026-10-22 | opencode | Converted this folder's `README.md` from the Word document it was authored in to markdown, so the `README.md`-per-folder rule in `AGENT.md` holds and the content is diffable and agent-editable. |
| 2026-10-19 | agent | Initialized `src/app` FSD layer folder and documented architecture, import rules, and required packages. |
