# institutionaldiwan.client / src/shared — Shared Infrastructure, UI Kit & Utilities Layer
> **Status:** Scaffolding & Rules — FSD shared segments, HTTP client contract, and UI kit rules defined; code pending.
## Purpose
`src/shared/` is the foundation layer of Feature-Sliced Design (FSD). It holds reusable, domain-agnostic code: the HTTP client with JWT Bearer injection and RFC 7807 `ProblemDetails` parsing, RTL-ready design system primitives (`shadcn/ui` + Radix), date/timezone helpers (`Asia/Damascus`), permission string constants, and generic TypeScript utilities. Nothing in `shared/` may contain business logic or import from upper layers.
## Layer & Import Rules (FSD Strict Hierarchy)
`shared` is the lowest layer in FSD (app → pages → widgets → features → entities → shared):
- Allowed imports: External npm packages and other segments within `@/shared/*`.
- Forbidden imports: Must never import from `@/entities/*`, `@/features/*`, `@/widgets/*`, `@/pages/*`, or `@/app/*`.
- Segment structure: Organized by technical purpose (`api/`, `ui/`, `lib/`, `config/`, `types/`), each with its own `index.ts` and `README.md`.
## Required Packages
| Package | Version / Role | Used in `src/shared/` for |
| --- | --- | --- |
| `axios` | `^1.x` | `shared/api/httpClient.ts` with JWT Bearer request interceptor & RFC 7807 `ProblemDetails` response interceptor |
| `clsx` + `tailwind-merge` + `class-variance-authority` | latest | `shared/lib/cn.ts` and variant-driven UI primitives in `shared/ui/` |
| `@radix-ui/*` + `lucide-react` | latest | Accessible RTL-ready UI primitives (`Button`, `Input`, `Dialog`, `Select`, `Table`, `Badge`, `Tooltip`, `Tabs`) |
| `@microsoft/signalr` | `^9.x` / `^10.x` | Real-time hub connection client helper in `shared/api/signalrClient.ts` |
## Planned Segments & Contents
| Segment Folder | Purpose | Key Files Planned |
| --- | --- | --- |
| `api/` | Base HTTP client, auth token storage, RFC 7807 `ProblemDetails` error class (`ApiError`), and SignalR hub helper. | `httpClient.ts`, `problemDetails.ts`, `tokenStorage.ts`, `index.ts` |
| `ui/` | Atomic, accessible RTL-first UI primitives (shadcn/ui style) with zero domain coupling. | `button.tsx`, `input.tsx`, `dialog.tsx`, `select.tsx`, `badge.tsx`, `table.tsx`, `card.tsx`, `skeleton.tsx`, `index.ts` |
| `lib/` | Pure utility functions: `cn()`, `formatDamascusDate()`, Arabic text normalization, and SHA-256 Web Crypto file hashing. | `cn.ts`, `date.ts`, `crypto.ts`, `index.ts` |
| `config/` | Environment config, route path map, and canonical `Permissions` constants mirroring `InstitutionalDiwan.Domain.Common.Permissions`. | `env.ts`, `routes.ts`, `permissions.ts`, `index.ts` |
| `types/` | Shared generic TypeScript contracts (`ProblemDetails`, `LookupItem`, `ApiErrorResponse`). | `api.ts`, `lookup.ts`, `index.ts` |
## Conventions / Vibe-Coding Notes (OpenCode & Freebuff)
- RFC 7807 `ProblemDetails` normalization (`shared/api/problemDetails.ts`): `InstitutionalDiwan.Server` returns `{ type, title, status, detail }` on every error (`400`, `401`, `403`, `404`, `409`, `500`), where `title` is the domain error code (e.g., `Numbering.OfficialNumberTaken`) and `detail` is the message. `httpClient` must normalize Axios errors into a typed `ApiError` exposing `.code` (`title`), `.detail`, and `.status`.
- Vite Proxy Requirement (`vite.config.ts`): Ensure `vite.config.ts` proxies `^/api` (and `^/hubs`) to the ASP.NET Core backend (`https://localhost:7126` / `http://localhost:5028`) so all `shared/api` requests use relative `/api/...` URLs without CORS issues.
- Golden Rule: Update this `README.md` and the touched segment's `README.md` in the same turn whenever files change.
## Implementation status
| Segment | State |
| --- | --- |
| `api/` | ❌ Not implemented |
| `ui/` | ❌ Not implemented |
| `lib/` | ❌ Not implemented |
| `config/` | ❌ Not implemented |
| `types/` | ❌ Not implemented |
## Change Log
| Date | Author | Change |
| --- | --- | --- |
| 2026-10-22 | opencode | Converted this folder's `README.md` from the Word document it was authored in to markdown, so the `README.md`-per-folder rule in `AGENT.md` holds and the content is diffable and agent-editable. |
| 2026-10-19 | agent | Initialized `src/shared` FSD layer folder and documented segments, `ProblemDetails` handling, and required packages. |
