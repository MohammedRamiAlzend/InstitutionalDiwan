# institutionaldiwan.front / src/shared — Shared Infrastructure, UI Kit & Utilities Layer

> **Status:** Core implemented for Identity Module 01 — `api` (axios client, ProblemDetails →
> `ApiError`, Arabic error catalog, token storage), `config` (16 permissions, routes, env), `lib`
> (`cn`, theme store), `types` (ProblemDetails, PagedResult), and the core `ui` primitives
> (`button`, `input`, `form-field`, `theme-toggle`). The data grid, context menu, and Mermaid
> renderer land with their slices.

## Purpose

`src/shared/` is the FSD foundation: HTTP client + JWT + RFC 7807 parsing, RTL UI primitives,
date/timezone helpers (`Asia/Damascus`), canonical permissions, and generic TypeScript utilities.
No business logic; no imports from upper layers.

## Layer & Import Rules

- Allowed: npm packages and other `@/shared/*` segments.
- Forbidden: entities / features / widgets / pages / app.
- Segments: `api/`, `ui/`, `lib/`, `config/`, `types/` — each with `index.ts` + `README.md`.

## Required Packages

| Package | Used for | State |
| --- | --- | --- |
| `axios` | `shared/api/httpClient.ts` | ✅ Installed |
| `clsx` + `tailwind-merge` + `class-variance-authority` | `cn()` + UI variants | ✅ Installed |
| `lucide-react` | Icons | ✅ Installed |
| `@radix-ui/*` + `@radix-ui/react-context-menu` | RTL primitives, right-click menus | ❌ To-be with the data-grid slice |
| `mermaid` | Lazy-loaded diagram / chart / tree rendering in `ui/mermaid-view` | ❌ To-be with the visualization slice |
| `@microsoft/signalr` | Hub helper (only after server hubs exist) | ❌ Deferred |

## Planned Segments

| Segment | Purpose | Key files |
| --- | --- | --- |
| `api/` | HTTP client, token storage, `ApiError`, optional SignalR | `httpClient.ts`, `problemDetails.ts`, `tokenStorage.ts` |
| `ui/` | Atomic RTL primitives, the shared data grid, the right-click menu primitive, and the Mermaid renderer | `button`, `input`, `dialog`, `data-table`, `context-menu`, `mermaid-view`, `skeleton`, … |
| `lib/` | `cn()`, Damascus dates, Web Crypto SHA-256, and per-table column preferences | `cn.ts`, `date.ts`, `crypto.ts`, `columnPreferences.ts` |
| `config/` | `Permissions` (16), `ROUTES`, `ENV` | `permissions.ts`, `routes.ts`, `env.ts` |
| `types/` | `ProblemDetails`, `PagedResult<T>`, `LookupItem` | `api.ts`, `lookup.ts` |

## Conventions

- Normalize ProblemDetails → `ApiError` (`.code` / `.detail` / `.status`).
- Relative `/api/...` URLs; Vite must proxy `^/api` (and `^/hubs`) — still TODO in `vite.config.ts`.
- First-slice TypeScript contracts for permissions and `ApiError` live in root `AGENTS.md`.
- `ui/data-table` is the only table implementation: filters/search/sort/pagination are URL-driven,
  and the column chooser persists visibility through `lib/columnPreferences` under
  `diwan.table.<tableId>.columns.v1`.
- `ui/context-menu` is the only right-click implementation; callers pass permission-filtered items
  and keep the same actions reachable from the keyboard.
- `ui/mermaid-view` lazy-loads `mermaid` with `securityLevel: 'strict'`, rejects HTML/script labels,
  caps input size, and renders an Arabic-first error state for invalid syntax.
- Entity-specific Mermaid/tree builders stay in the owning entity's `model/`; `shared` only renders.

## Implementation status

| Segment | State |
| --- | --- |
| `api/` | ✅ Core — `httpClient.ts`, `problemDetails.ts`, `tokenStorage.ts`; `signalrClient.ts` deferred |
| `config/` | ✅ `permissions.ts`, `routes.ts`, `env.ts` |
| `lib/` | ✅ `cn.ts`, `theme.ts`; `date`, `crypto`, `arabic`, `columnPreferences` pending their slices |
| `types/` | ✅ `api.ts` (ProblemDetails, PagedResult); `lookup.ts` pending the lookups slice |
| `ui/` | ✅ `button`, `input`, `form-field`, `theme-toggle`; `data-table`, `context-menu`, `mermaid-view` pending |

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-10-27 | agent | Implemented the shared foundation for Identity Module 01, including the Arabic `errorMessages` catalog (domain-code → Arabic, status fallbacks) so no English server detail reaches the UI. |
| 2026-10-25 | Buffy | Documented the shared interaction primitives: `ui/context-menu`, `ui/data-table` with `lib/columnPreferences` (localStorage), and lazy `ui/mermaid-view`; added `mermaid` and `@radix-ui/react-context-menu` to the package list. |
| 2026-10-25 | Cursor | Clarified as-is vs to-be packages; noted 16 permissions and PagedResult shared type; pointed at AGENTS.md contracts. |
| 2026-10-22 | opencode | Converted this folder's `README.md` from the Word document it was authored in to markdown. |
| 2026-10-19 | agent | Initialized `src/shared` FSD layer folder and documented segments, `ProblemDetails` handling, and required packages. |
