# institutionaldiwan.client / src/app — Application Initialization & Global Providers Layer
> **Status:** Scaffolding & Rules — FSD layer rules, segments, and required packages defined; code pending.
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
| `providers/AppProviders.tsx` | Composes `QueryProvider`, `DirectionProvider` (`dir="rtl"` default for Arabic), `AuthSessionLoader`, and `Toaster`. |
| `providers/QueryProvider.tsx` | Configures singleton `QueryClient` and wraps children in `QueryClientProvider`. |
| `providers/AuthSessionLoader.tsx` | Hydrates current user session on startup via `GET /api/auth/me` when a JWT exists in storage. |
| `router/AppRouter.tsx` | Declares public routes (`/login`) and protected shell routes (`/`, `/documents`, `/movements`, `/departments`, `/users`, `/roles`, `/employees`, `/numbering`, `/audit`). `/archive` stays out until Module 07 exists. |
| `router/ProtectedRoute.tsx` | Redirects unauthenticated users to `/login` and forces `/change-password` when `must_change_password === "true"` in JWT claims. |
| `router/RequirePermission.tsx` | Route/component guard using the full 16-code `Permissions` set (incl. `AUDIT_VIEW` / `AUDIT_MANAGE`). |
| `styles/index.css` | Tailwind CSS v4 entry, Arabic font declarations (`Cairo` / `Readex Pro`), and CSS custom properties for light/dark institutional theme. |
## Conventions / Vibe-Coding Notes (OpenCode & Freebuff)
- Always enforce `dir="rtl"` and `lang="ar"` on `<html>` by default (with optional toggle to `ltr`/`en` since lookups carry both `DescAr` and `Desc`).
- Permission policy parity: Every route guard in `router/RequirePermission.tsx` must use the exact permission strings from `@/shared/config/permissions` matching `InstitutionalDiwan.Domain/Common/Permissions.cs`.
- Golden Rule: Update this `README.md` (and any subfolder `README.md`) in the same turn whenever files are added, renamed, or deleted.
## Implementation status
| Item | State |
| --- | --- |
| `providers/` (`AppProviders`, `QueryProvider`, `AuthSessionLoader`) | ❌ Not implemented |
| `router/` (`AppRouter`, `ProtectedRoute`, `RequirePermission`) | ❌ Not implemented |
| `styles/` (Tailwind v4 + RTL theme tokens) | ❌ Not implemented |
## Change Log
| Date | Author | Change |
| --- | --- | --- |
| 2026-10-25 | Cursor | Wired route map to live modules (audit in, archive deferred); RequirePermission covers all 16 permission codes. |
| 2026-10-22 | opencode | Converted this folder's `README.md` from the Word document it was authored in to markdown, so the `README.md`-per-folder rule in `AGENT.md` holds and the content is diffable and agent-editable. |
| 2026-10-19 | agent | Initialized `src/app` FSD layer folder and documented architecture, import rules, and required packages. |
