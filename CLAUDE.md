# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

Institutional correspondence system: a clean-architecture ASP.NET Core 10 API (`InstitutionalDiwan.*` projects, in `InstitutionalDiwan.slnx`) plus a **standalone** Arabic-first React 19 + Vite SPA in `institutionaldiwan.front/` (not in the solution; the API is an external service to it). Backend modules 01–07 and 09 are implemented; modules 08/10 have no API (never call their endpoints), and module 07 has no front end yet.

Read [`README.md`](README.md) (module status), [`AGENT.md`](AGENT.md) (backend rules) and [`institutionaldiwan.front/AGENTS.md`](institutionaldiwan.front/AGENTS.md) (front-end rules) before large changes; `institutionaldiwan.front/knowledge.md` is the live endpoint/permission catalog.

## Commands

Backend (repo root):

```bash
dotnet build InstitutionalDiwan.slnx          # or build.cmd
dotnet test InstitutionalDiwan.slnx
dotnet test InstitutionalDiwan.Tests --filter "FullyQualifiedName~OpenApiDocumentationTests"   # single class/test
dotnet run --project InstitutionalDiwan.Server  # http://localhost:5028, https://localhost:7126, Scalar at /scalar/v1
```

Development startup migrates and seeds SQL Server (`Server=.;Trusted_Connection=True`, db `InstitutionalDiwan`). Seeded accounts: `admin` / `12345678` (system scope), `user` / `12345678` (department scope).

Front end (`cd institutionaldiwan.front`):

```bash
npm run dev          # Vite on :5173, proxies /api and /hubs to DEV_PROXY_TARGET (copy .env.example to .env)
npm run build        # tsc -b && vite build — must pass with no errors
npm run lint         # eslint + scripts/check-guide-ids.mjs
npm run check:guides # only the tutorial-guide id check
```

There is no front-end test runner; `build`, `lint` and exercising the SPA against the live API are the gates.

## Backend architecture

One-way dependencies: Domain → nothing; Application → Domain; Infrastructure → Domain + Application; Server → Application + Infrastructure. Application has no EF Core/ASP.NET; Domain has no packages. CQRS slices live in `Application/Features/<Module>/...` with validators and pipeline behaviors.

- **Permissions** have one source of truth: `InstitutionalDiwan.Domain/Common/Permissions.cs`. The seeded catalog, JWT claims and ASP.NET policies must use identical strings, and the front end's `src/shared/config/permissions.ts` must mirror them exactly (17 codes).
- **Errors** are RFC 7807 `ProblemDetails`; `title` is the domain error code (e.g. `Numbering.OfficialNumberTaken`), `detail` is English text.
- **OpenAPI**: every route needs a summary, description and a concrete example per parameter; `OpenApiDocumentationTests` fails the build otherwise (`add-endpoint-docs.mjs` helps generate these).
- Use namespaces `InstitutionalDiwan.*`; legacy `NewDiwan.*` must not be introduced.

## Docs-with-code rule (both backend and front end)

Whenever a folder's contents change, update that folder's `README.md` in the same change: `Contents` table (every file, one purposeful sentence), `Implementation status`, and a new top row in `Change Log` (`| YYYY-MM-DD | author | summary |`, append-only). Update a parent README's status row if it lists the subfolder. Keep `knowledge.md` in sync when API modules/permissions change.

## Front-end architecture (Feature-Sliced Design, strict)

Layers `app → pages → widgets → features → entities → shared`; imports flow downward only, never across sibling slices, and only through a slice's `index.ts` barrel (no deep imports). Reads (`GET`, `useQuery`, query-key factory) live in `entities/`; writes (`POST/PUT/DELETE`, `useMutation`, Zod schema) live in `features/` and must invalidate via the entity's query-key factory. `pages/<slice>/` holds one route, ~120 lines max. No `any`.

Conventions that are easy to violate:
- **Arabic/RTL first, light + dark.** `<html lang="ar" dir="rtl">`; use logical CSS only (`ms-*`, `me-*`, `ps-*`, `start-*`, `text-start`) — never `ml/mr/left/right/text-left`. Latin-only values (codes, official numbers, hashes, ISO dates) go in `dir="ltr"` islands. No hex colors in components; use semantic CSS variables from `src/app/styles/tokens.css`.
- **UI text is Arabic only**: render `toArabicErrorMessage(error)` (`shared/api/errorMessages.ts`), branching on `error.code`, never the server's English `detail`. Extend that catalog per module. Exception: `POST /api/audit-logs/integrity-checks` returns 200 with `isValid: false` — read the body.
- **Filters, search, sort, pagination live in URL search params**; server data stays in React Query, not `useState`.
- **Desktop-grade contract on every data surface**: shared Radix context-menu primitive (permission-filtered items, keyboard reachable), every table is a full grid (filter bar, search, pager, sort, column chooser persisted in `localStorage` as `diwan.table.<tableId>.columns.v1` with a stable table id), and Mermaid via the lazy shared `mermaid-view`. Details in `institutionaldiwan.front/AGENTS.md`.
- **Tutorial mode** (Driver.js, opt-in): guide catalog in `src/shared/config/guides/*.ts`; JSX uses `GuideChip`/`GuideRegion`/`FormField guideId`/`GuideProvider pageId`/`PageHeader pageGuide` with literal ids. `npm run lint` fails if a used id is missing from the catalog or a catalog entry is unused. Plan: `plans/tutorial-mode-frontend.md`.
