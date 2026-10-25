# institutionaldiwan.client / src/shared — Shared Infrastructure, UI Kit & Utilities Layer

> **Status:** Scaffolding & Rules — FSD shared segments, HTTP client contract, and UI kit rules
> defined; code pending. Packages listed below are **to-be** (not in `package.json` yet).

## Purpose

`src/shared/` is the FSD foundation: HTTP client + JWT + RFC 7807 parsing, RTL UI primitives,
date/timezone helpers (`Asia/Damascus`), canonical permissions, and generic TypeScript utilities.
No business logic; no imports from upper layers.

## Layer & Import Rules

- Allowed: npm packages and other `@/shared/*` segments.
- Forbidden: entities / features / widgets / pages / app.
- Segments: `api/`, `ui/`, `lib/`, `config/`, `types/` — each with `index.ts` + `README.md`.

## Required Packages (to-be)

| Package | Used for |
| --- | --- |
| `axios` | `shared/api/httpClient.ts` |
| `clsx` + `tailwind-merge` + `class-variance-authority` | `cn()` + UI variants |
| `@radix-ui/*` + `lucide-react` | RTL primitives |
| `@microsoft/signalr` | Hub helper (only after server hubs exist) |

## Planned Segments

| Segment | Purpose | Key files |
| --- | --- | --- |
| `api/` | HTTP client, token storage, `ApiError`, optional SignalR | `httpClient.ts`, `problemDetails.ts`, `tokenStorage.ts` |
| `ui/` | Atomic RTL primitives | `button`, `input`, `dialog`, `table`, `skeleton`, … |
| `lib/` | `cn()`, Damascus dates, Web Crypto SHA-256 for client-side checksum display | `cn.ts`, `date.ts`, `crypto.ts` |
| `config/` | `Permissions` (16), `ROUTES`, `ENV` | `permissions.ts`, `routes.ts`, `env.ts` |
| `types/` | `ProblemDetails`, `PagedResult<T>`, `LookupItem` | `api.ts`, `lookup.ts` |

## Conventions

- Normalize ProblemDetails → `ApiError` (`.code` / `.detail` / `.status`).
- Relative `/api/...` URLs; Vite must proxy `^/api` (and `^/hubs`) — still TODO in `vite.config.ts`.
- First-slice TypeScript contracts for permissions and `ApiError` live in root `AGENTS.md`.

## Implementation status

| Segment | State |
| --- | --- |
| `api/`, `ui/`, `lib/`, `config/`, `types/` | ❌ Not implemented |

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-10-25 | Cursor | Clarified as-is vs to-be packages; noted 16 permissions and PagedResult shared type; pointed at AGENTS.md contracts. |
| 2026-10-22 | opencode | Converted this folder's `README.md` from the Word document it was authored in to markdown. |
| 2026-10-19 | agent | Initialized `src/shared` FSD layer folder and documented segments, `ProblemDetails` handling, and required packages. |
