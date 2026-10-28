# institutionaldiwan.front / src/shared — Shared Infrastructure, UI Kit & Utilities Layer

> **Status:** Implemented for Identity Module 01, the Module 02 admin foundation, and the Module 01
> hierarchy slice — `api` (axios client, ProblemDetails → `ApiError`, expanded Arabic error catalog,
> token storage), `config` (16 permissions, routes, env), `lib` (`cn`, theme store, URL table state,
> column preferences, date and JWT helpers), `types` (ProblemDetails, PagedResult), and the `ui` kit
> including dialogs, menus, select/checkbox, the shared data grid, and the lazy Mermaid renderer.

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
| `@radix-ui/*` + `@radix-ui/react-context-menu` | Dialog, dropdown, context menu, select, checkbox primitives | ✅ Installed (`dialog`, `dropdown-menu`, `context-menu`, `select`, `checkbox`) |
| `mermaid` | Lazy-loaded diagram rendering in `ui/mermaid-view` | ✅ Installed |
| `@microsoft/signalr` | Hub helper (only after server hubs exist) | ❌ Deferred |

## Planned Segments

| Segment | Purpose | Key files |
| --- | --- | --- |
| `api/` | HTTP client, token storage, `ApiError`, optional SignalR | `httpClient.ts`, `problemDetails.ts`, `tokenStorage.ts` |
| `ui/` | Atomic RTL primitives, the shared data grid, the right-click menu primitive, and the Mermaid renderer | `button`, `input`, `dialog`, `data-table`, `context-menu`, `mermaid-view`, `skeleton`, … |
| `lib/` | `cn()`, theme store, URL table state, Damascus dates, column preferences, JWT hints | `cn.ts`, `theme.ts`, `useTableState.ts`, `date.ts`, `columnPreferences.ts`, `jwt.ts` |
| `config/` | `Permissions` (16), `ROUTES`, `ENV` | `permissions.ts`, `routes.ts`, `env.ts` |
| `types/` | `ProblemDetails`, `PagedResult<T>`, `LookupItem` | `api.ts`, `lookup.ts` |

## Conventions

- Normalize ProblemDetails → `ApiError` (`.code` / `.detail` / `.status`).
- Relative `/api/...` URLs; Vite proxies `^/api` and `^/hubs` to the API target (`vite.config.ts`).
- First-slice TypeScript contracts for permissions and `ApiError` live in root `AGENTS.md`.
- `ui/data-table` is the only table implementation: filters/search/sort/pagination are URL-driven,
  and the column chooser persists visibility through `lib/columnPreferences` under
  `diwan.table.<tableId>.columns.v1`.
- `ui/context-menu` is the only right-click implementation; callers pass permission-filtered items
  and keep the same actions reachable from the keyboard.
- `ui/mermaid-view` lazy-loads `mermaid` with `securityLevel: 'strict'`, rejects HTML/script labels,
  caps input size, disables external links, derives `themeVariables` from the theme tokens, and
  renders an Arabic-first error state for invalid syntax.
- Entity-specific Mermaid/tree builders stay in the owning entity's `model/`; `shared` only renders.

## Implementation status

| Segment | State |
| --- | --- |
| `api/` | ✅ Core — `httpClient.ts`, `problemDetails.ts`, `tokenStorage.ts`, `errorMessages.ts` (Identity + Department/Lookup); `signalrClient.ts` deferred |
| `config/` | ✅ `permissions.ts`, `routes.ts`, `env.ts` |
| `lib/` | ✅ `cn.ts`, `theme.ts`, `useTableState.ts`, `useDebouncedValue.ts`, `columnPreferences.ts`, `date.ts`, `format.ts`, `jwt.ts`; `crypto`/`arabic` pending |
| `types/` | ✅ `api.ts` (ProblemDetails, PagedResult); `lookup.ts` pending the lookups slice |
| `ui/` | ✅ Form primitives, overlays, menus, `data-table`, and the lazy `mermaid-view` |

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-10-28 | agent | Added `ui/mermaid-view` and installed `mermaid` for the Module 01 hierarchy slice (lazy load, strict security, token-derived theme variables, Arabic error state); corrected the proxy note now that `vite.config.ts` forwards `/api` and `/hubs`. |
| 2026-10-27 | agent | Lifted the shared foundation to the Module 02 admin phase: installed Radix primitives, added table-state/column-preference/date/JWT utilities, built dialogs/menus/select/checkbox/badges/skeletons and the shared `data-table`, and expanded the Arabic error catalog. |
| 2026-10-27 | agent | Implemented the shared foundation for Identity Module 01, including the Arabic `errorMessages` catalog (domain-code → Arabic, status fallbacks) so no English server detail reaches the UI. |
| 2026-10-25 | Buffy | Documented the shared interaction primitives: `ui/context-menu`, `ui/data-table` with `lib/columnPreferences` (localStorage), and lazy `ui/mermaid-view`; added `mermaid` and `@radix-ui/react-context-menu` to the package list. |
| 2026-10-25 | Cursor | Clarified as-is vs to-be packages; noted 16 permissions and PagedResult shared type; pointed at AGENTS.md contracts. |
| 2026-10-22 | opencode | Converted this folder's `README.md` from the Word document it was authored in to markdown. |
| 2026-10-19 | agent | Initialized `src/shared` FSD layer folder and documented segments, `ProblemDetails` handling, and required packages. |
