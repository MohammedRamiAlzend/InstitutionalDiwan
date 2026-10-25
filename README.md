# Institutional Diwan — نظام الديوان المؤسساتي الموحد

> **Status:** Backend implemented for Modules 01–06 and 09 (Organization, Identity, Correspondence,
> Numbering, Movements, Attachments, Audit) plus the cross-module reference catalogs. The React client
> is documented and scaffolded but has no application code yet.

The unified institutional correspondence system: a clean-architecture ASP.NET Core 10 API that
registers documents, allocates official numbers, routes them between departments, and keeps an
auditable trail — with an Arabic-first React 19 front end planned on top.

## Repository layout

| Folder | Layer | Responsibility |
| --- | --- | --- |
| `InstitutionalDiwan.Domain/` | Domain | Entities, value objects, domain errors, domain events, repository contracts. No external packages. |
| `InstitutionalDiwan.Application/` | Application | CQRS commands/queries, validators, pipeline behaviors, abstractions. No EF Core, no ASP.NET. |
| `InstitutionalDiwan.Infrastructure/` | Infrastructure | EF Core persistence and migrations, JWT auth, authorization evaluators, seeding, storage, outbox, background jobs, clock. |
| `InstitutionalDiwan.Server/` | Server | ASP.NET Core presentation — controllers, HTTP contracts, middleware, DI/OpenAPI extensions. |
| `InstitutionalDiwan.Tests/` | Tests | xUnit unit and integration tests for every layer. |
| `institutionaldiwan.client/` | Client | React 19 + TypeScript 6 + Vite 8 SPA on Feature-Sliced Design. |
| `plans/` | Docs | Implementation plans that are not yet executed. |

Dependency direction is one-way: **Domain → nothing**, **Application → Domain**,
**Infrastructure → Domain + Application**, **Server → Application + Infrastructure**.

Every layer has a `README.md` that lists the files it contains, the conventions that apply there,
and a change log. `AGENT.md` prescribes that structure for *every* folder that holds code; the
Server's `Controllers/`, `Contracts/`, `Middleware/`, and `Extensions/`, the Application's
`Features/` slices, and every client slice already follow it. The Domain and Infrastructure
layers are currently documented folder-by-folder inside their layer-level `README.md` rather than
in a `README.md` per subfolder — see the "Known gap" note below.

## Prerequisites

| Tool | Version | Needed for |
| --- | --- | --- |
| .NET SDK | 10.0 | Building and running the API and the tests |
| SQL Server | 2019+ / LocalDB | The `InstitutionalDiwan` database (`Server=.;Trusted_Connection=True`) |
| Node.js | 20+ (24 recommended) | The Vite dev server and the client build |

## Working with the code

```bash
# Build the whole solution (server + client project file)
dotnet build InstitutionalDiwan.slnx
# or, on Windows
build.cmd

# Run the tests
dotnet test InstitutionalDiwan.slnx

# Run the API (migrates + seeds the database in Development)
dotnet run --project InstitutionalDiwan.Server
```

The API listens on `http://localhost:5028` and `https://localhost:7126`. In Development it
exposes the OpenAPI document and the Scalar API reference (`/scalar/v1`); every route is
documented with a summary, a description, and a concrete example for each parameter, and
`OpenApiDocumentationTests` fails the build if one is missing.

The database is migrated and seeded on startup in the Development environment. Seeded demo
accounts: `admin` / `12345678` (system scope) and `user` / `12345678` (department scope).

```bash
# Client
cd institutionaldiwan.client
npm install
npm run dev     # Vite on https://localhost:61325, proxying /api and /hubs to the API
npm run build   # tsc -b && vite build
npm run lint
```

> The client currently contains only the Vite template's `App.tsx` and `main.tsx`. Its
> architecture, planned slices (incl. Attachments + Audit), 16 permissions, UI/design contract, and
> live endpoint catalog are documented in
> [`institutionaldiwan.client/README.md`](institutionaldiwan.client/README.md),
> [`AGENTS.md`](institutionaldiwan.client/AGENTS.md), and
> [`knowledge.md`](institutionaldiwan.client/knowledge.md) (current as of 2026-10-25).

## Module status

| Module | Slice | State |
| --- | --- | --- |
| 01 — Organization | Departments, institution settings, reference catalogs | ✅ Implemented |
| 02 — Identity | Login, register, change password, users, roles, permissions, employees, document access grants, department scope | ✅ Implemented |
| 03 — Correspondence | Documents (create, draft, update, register, participants, cancel, details, timeline), external organizations | ✅ Implemented |
| 04 — Numbering | Registries, central official numbers, department register books | ✅ Implemented |
| 05 — Movements | Routing workflow (open, send, receive, complete, reject, return, redirect, close, cancel) + department inbox/outbox | ✅ Implemented |
| 06 — Attachments | File metadata, version chain, upload/download, checksum verification | ✅ Implemented |
| 07 — Archiving | Domain entities and lookups only | ❌ Not implemented |
| 08 — Relations | Domain entities and lookups only | ❌ Not implemented |
| 09 — Audit | Append-only SHA-256 chain, search, checkpoints, integrity checks/reports, scheduled maintenance | ✅ Implemented |
| 10 — Notifications | Domain entities and lookups only | ❌ Not implemented |

## Conventions

- **Code and docs change together.** When a folder's contents change, that folder's `README.md`
  is updated in the same turn — `Contents`, `Implementation status`, and a `Change Log` row. See
  [`AGENT.md`](AGENT.md) for the exact required structure; it is the contract for every agent and
  contributor working here.
- **Namespaces** are `InstitutionalDiwan.*`. The legacy `NewDiwan.*` names are being phased out —
  do not introduce new ones.
- **Permissions** have a single source of truth in
  `InstitutionalDiwan.Domain/Common/Permissions.cs`; the seeded catalog, the JWT claims, and the
  ASP.NET Core policies must all use the identical strings.
- **Errors** are RFC 7807 `ProblemDetails` whose `title` is the domain error code (for example
  `Numbering.OfficialNumberTaken`) and whose `detail` is the human-readable message.

## Contents

| File | Purpose |
| --- | --- |
| `AGENT.md` | The repository rules every agent and contributor must follow, including the mandatory README structure. |
| `InstitutionalDiwan.slnx` | The solution file: five .NET projects plus the client `.esproj`. |
| `build.cmd` | Windows convenience wrapper around `dotnet build`. |
| `add-endpoint-docs.mjs` | Node script that fills in the OpenAPI summary, description, and parameter examples of a new endpoint; idempotent, run it with Node. |
| `plans/` | Implementation plans that have not been executed yet. |
| `opencode.json` | OpenCode workspace configuration. |
| `.freebuff/` | Freebuff project identifier. |

## Known gap

`AGENT.md` requires a `README.md` in every folder that holds code. The following backend folders
have no `README.md` of their own yet — their content is documented in the enclosing layer's
`README.md` instead:

- `InstitutionalDiwan.Domain/` — `Common/`, `Entities/` (and its 10 module folders),
  `Errors/`, `Events/`, `Repositories/`, `ValueObjects/`
- `InstitutionalDiwan.Application/` — `Features/Correspondence/`, `Features/Identity/` (6 subfolders;
  `Login/` has its own README), `Features/Organization/` (2 subfolders)
- `InstitutionalDiwan.Infrastructure/` — `Persistence/Configurations/`
- `InstitutionalDiwan.Server/` — `Authorization/`, `Contracts/` (8 subfolders)
- `InstitutionalDiwan.Tests/` — `Application/`, `Authorization/`, `Persistence/`, `Seed/`,
  `Server/`, `Support/`

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-10-25 | Cursor | Noted that client docs now track Modules 01–06 + 09 (endpoints, permissions, UI contract) while SPA code remains the Vite template. |
| 2026-10-25 | Buffy | Serialized audit-chain appends so concurrent reads, writes, and scheduled audit jobs cannot collide on the chain head; the suite now has 375 tests. |
| 2026-10-25 | opencode | Marked Modules 06 (Attachments) and 09 (Audit) implemented; documented the audit chain, scheduled checkpoint/integrity jobs, and the current 374-test suite. |
| 2026-10-22 | Buffy | Documented `Features/Identity/Login/` with its own README (the caller-profile query now lives there) and updated the known-gap list; the backend now passes one deterministic instant per transaction through every domain write. |
| 2026-10-22 | opencode | Added the missing root `README.md`: the repository layout, prerequisites, build/run/test commands, the per-module status table, and the conventions that govern work in this repository. |
