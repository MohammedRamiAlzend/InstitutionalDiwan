# Institutional Diwan — نظام الديوان المؤسساتي الموحد

> **Status:** Backend implemented for Modules 01–06 and 09 plus reference catalogs; `dotnet build`
> succeeds and 487 tests pass. Frontend Module 01 Organization includes department management,
> tree/Mermaid/chart/grid views, lazy employee branches, paged rosters, and `/institution-settings`.
> Module 02 Identity has auth and administration screens with live-server-enforced session/forced
> password-change rules. Module 03 Correspondence is complete (Phases 0–8: document reads and Arabic
> errors, `/documents` registry and creation, details/lifecycle, access grants, numbering surfaces,
> the `/movements` inbox/outbox, the external-correspondent tab, and a role-matrix pass). Module 04
> Numbering is complete (Phases 1–5: template model, `/numbering` tabs with URL-backed filters and
> duplicate-scope warnings, shared-grid department register and document numbers, template
> preview/lock, and manager plus read-only verification). Module 09 Audit has `/audit` logs and
> integrity reports, and Module 05 Movements has its inbox/outbox page. Attachments (06) remain
> planned. `npm run build` and `npm run lint` pass, and the SPA was exercised against the live API
> throughout (login, session invalidation, departments, settings, documents, movements, numbering, audit).

The unified institutional correspondence system: a clean-architecture ASP.NET Core 10 API that
registers documents, allocates official numbers, routes them between departments, and keeps an
auditable trail — with an Arabic-first React 19 front end developed separately on top.

## Repository layout

| Folder | Layer | Responsibility |
| --- | --- | --- |
| `InstitutionalDiwan.Domain/` | Domain | Entities, value objects, domain errors, domain events, repository contracts. No external packages. |
| `InstitutionalDiwan.Application/` | Application | CQRS commands/queries, validators, pipeline behaviors, abstractions. No EF Core, no ASP.NET. |
| `InstitutionalDiwan.Infrastructure/` | Infrastructure | EF Core persistence and migrations, JWT auth, authorization evaluators, seeding, storage, outbox, background jobs, clock. |
| `InstitutionalDiwan.Server/` | Server | ASP.NET Core presentation — controllers, HTTP contracts, middleware, DI/OpenAPI extensions. |
| `InstitutionalDiwan.Tests/` | Tests | xUnit unit and integration tests for every layer. |
| `institutionaldiwan.front/` | Front end | Standalone React 19 + TypeScript + Vite SPA on Feature-Sliced Design; not part of the solution. |
| `plans/` | Docs | Implementation plans that are not yet executed. |

Dependency direction is one-way: **Domain → nothing**, **Application → Domain**,
**Infrastructure → Domain + Application**, **Server → Application + Infrastructure**.

Every layer has a `README.md` that lists the files it contains, the conventions that apply there,
and a change log. `AGENT.md` prescribes that structure for *every* folder that holds code; the
Server's `Controllers/`, `Contracts/`, `Middleware/`, and `Extensions/`, the Application's
`Features/` slices, and every front-end slice already follow it. The Domain and Infrastructure
layers are currently documented folder-by-folder inside their layer-level `README.md` rather than
in a `README.md` per subfolder — see the "Known gap" note below.

## Prerequisites

| Tool | Version | Needed for |
| --- | --- | --- |
| .NET SDK | 10.0 | Building and running the API and the tests |
| SQL Server | 2019+ / LocalDB | The `InstitutionalDiwan` database (`Server=.;Trusted_Connection=True`) |
| Node.js | 20+ (24 recommended) | The standalone front end and the `add-endpoint-docs.mjs` script; not needed to build or run the API |

## Working with the code

```bash
# Build the solution (the five .NET projects)
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
# Front end — standalone Vite project, not part of the solution
cd institutionaldiwan.front
npm install
npm run dev     # Vite dev server on http://localhost:5173 (proxies /api via DEV_PROXY_TARGET)
npm run build   # tsc -b && vite build
npm run lint
```

> The front end's architecture, implemented slices, 16 permissions, UI/design contract, and live
> endpoint catalog are documented in
> [`institutionaldiwan.front/README.md`](institutionaldiwan.front/README.md),
> [`AGENTS.md`](institutionaldiwan.front/AGENTS.md), and
> [`knowledge.md`](institutionaldiwan.front/knowledge.md) (current as of 2026-10-30). The API is an
> external service from that project's point of view.

## Module status

| Module | Slice | Backend state | Frontend state |
| --- | --- | --- | --- |
| 01 — Organization | Departments, institution settings, reference catalogs | Implemented, including configured central-hub protection and inactive-parent activation guard. | Department management, tree/Mermaid/chart/grid, lazy employee branches, paged rosters/details/menus, and institution settings implemented and verified. Generic lookup administration is not implemented. |
| 02 — Identity | Auth, users, roles, permissions, employees, access grants, department scope | Implemented, including live session-stamp validation, inactive-role guards, and server-enforced forced password change. | Auth and users/roles/employees administration implemented and verified, including temporal department assignments and permission-scoped pickers. The document access-grant UI is implemented with Module 03 Phase 4. |
| 03 — Correspondence | Documents (create/draft/update/register/participants/cancel/details/timeline), external organizations, access grants | Implemented. | Phases 0–8 done and browser-verified: picker access, document reads, Arabic errors, the `/documents` registry with document/draft creation, the details page (participants, status history, timeline, draft-edit/register/add-participant/cancel), the `grants` tab, the numbers tab (issue/cancel/replace/department registration/history), the `/movements` inbox/outbox page with the nine routing commands, the external-correspondent admin tab, and a role-matrix pass. Attachments tab pending. |
| 04 — Numbering | Registries, central official numbers, department register books | Implemented. | Phases 1–5 done and browser-verified: template preview/validation constants, `/numbering` tabs (`?tab`) with URL-backed status/policy/scope filters and duplicate-scope warnings, the shared-grid department register book that keeps inactive departments and their history, the document numbers grid, and manager plus read-only (403) verification. |
| 05 — Movements | Routing workflow and department inbox/outbox | Implemented. | Implemented and verified with Module 03 Phase 6: `/movements` inbox/outbox plus the document-details movements tab with the nine routing commands. |
| 06 — Attachments | Metadata, versions, upload/download, checksum verification | Implemented. | Planned. |
| 07 — Archiving | Domain entities and lookups | Application/API not implemented. | Blocked on backend. |
| 08 — Relations | Domain entities and lookups | Application/API not implemented. | Blocked on backend. |
| 09 — Audit | Entity/action audit chain, search, checkpoints, integrity reports, scheduled maintenance | Implemented. | Implemented and verified: `/audit` logs and integrity reports behind `AUDIT_VIEW`, checkpoint/integrity actions behind `AUDIT_MANAGE`. |
| 10 — Notifications | Domain entities and lookups | Application/API not implemented. | Blocked on backend/hubs. |

“Implemented and verified” means the code is present and was exercised against the live API;
`dotnet test` (487 tests), `npm run build`, and `npm run lint` pass. Module 01 settings loads the
existing singleton: read errors, including missing settings, show Arabic feedback and retry rather
than creating or overwriting a record. Frontend Modules 03, 04, and 09 are implemented and
browser-verified; Module 06 attachments and Modules 07/08/10 remain.

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
| `InstitutionalDiwan.slnx` | The solution file: the five .NET projects (the React front end is not part of it). |
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
| 2026-10-30 | agent | The front-end create-document dialog gained an optional recipient field: users with `DOCUMENT_EDIT` can pick an external organization, an internal department, or a person, and the dialog attaches them as a primary RECEIVER participant right after creation (a participant failure keeps the document and reports only the recipient error). Verified in-browser; front build and lint pass. |
| 2026-10-30 | agent | Closed Modules 03 and 04 in the front end: Module 03 Phases 6–8 (movements inbox/outbox plus the document movements tab, the external-correspondent admin tab, and a role-matrix pass) and Module 04 Phases 1–5 (template preview/validation, `/numbering` tabs with URL-backed filters and duplicate-scope warnings, the department register book and document numbers grids on the shared table, and manager plus read-only 403 verification). Fixed the registry form so a deactivated department stays visible, disabled, and labelled «(موقوف)» instead of blanking the scope select. Recorded Module 05 and Module 09 front-end status and refreshed the top status; 487 tests pass and the front build/lint pass. |
| 2026-10-29 | Qoder | Module 03 Phase 5 (numbering): added `entities/numbering-registry` (registries/counters/department register), document number history/registry reads with `NumberStatusBadge`, `features/numbering-manage` (registry CRUD/activate; issue/cancel/replace/department registration), `widgets/numbering-registry-table`, the `/numbering` page, and the document-details numbers tab; activated the «الترقيم» nav item. Verified in-browser (registry lifecycle/counters, department register, issue `2026/000005`, department registration `DEP-2026-001`, duplicate `2026/000007` rejected in Arabic, replace `2026/000008`→`2026/000009`, cancel with reason, `USER`-role denial with 403 on direct writes). 487 tests pass and the front build/lint pass. |
| 2026-10-28 | Qoder | Module 03 Phase 4: added the `identity-options/users` option read to `entities/user`, the `features/document-access-grant` slice (grant a user an access level with optional future expiry and reason; revoke while keeping the record), and the document-details `grants` tab, shown only to `ACCESS_GRANT_MANAGE` holders. Verified in-browser (required-field and past-expiry Arabic validation, immediate grant row with grantor/expiry, revoked grant kept as «ملغى» with timestamp + revoker, tab hidden and endpoint 403 for a `USER`-role caller). The API takes no revocation reason, so the revoke dialog confirms without one; 487 tests pass and the front build/lint pass. |
| 2026-10-28 | Qoder | Module 03 Phase 3: added `entities/external-organization` reads, `features/document-manage` (draft edit, register into the numbering registry, add participant by organization/department/person, cancel with a reason), and the `/documents/:documentId` details page (participants, status history, Arabic audit timeline) with every transition gated on status + permission; registry rows now open the details page. Verified in-browser (draft actions, participants, edit, registration `2026/000004`, cancel with reason, timeline, canceled-document gating); 487 tests pass and the front build/lint pass. |
| 2026-10-28 | Qoder | Module 03 Phase 2 verified in-browser against the live API: registry search/filter/sort/empty state, draft and registered (`999/2026`) creation with immediate refresh, and the Arabic `Numbering.OfficialNumberTaken` field error; suppressed the duplicate form-level alert for that code. Front build and lint pass. |
| 2026-10-28 | Qoder | Module 03 Phase 2 (registry): added `features/document-create` (registered vs draft, lookup-driven selects, Arabic validation, `Numbering.OfficialNumberTaken` field error) and `widgets/document-registry-table`, mounted `/documents` behind `DOCUMENT_VIEW`, registered the route, activated the sidebar item, and opened `identity-options/departments` to `DOCUMENT_VIEW`; 487 tests pass and the front build/lint pass. |
| 2026-10-28 | Qoder | Module 03 Phase 1 (foundation): implemented `entities/document` (typed DTOs, query keys, list/by-id/details/timeline/grants hooks, status/direction badges, LTR official-number tag) and added the full Arabic catalog for `Correspondence.*`, `Numbering.*`, and `Movements.*`. No page or route yet; nav items stay disabled until their phases. Front build and lint pass. |
| 2026-10-28 | Qoder | Module 03 Phase 0 (picker access): `GET /api/identity-options/users` returns a paged, username-searchable `{ userId, username, isActive }` list behind `USER_MANAGE`, `ACCESS_GRANT_MANAGE`, or `DOCUMENT_EDIT`, and department options now also accept `DOCUMENT_CREATE`/`DOCUMENT_EDIT`. No document frontend code yet; 487 tests pass. |
| 2026-10-28 | agent | Closed the Module 01/02 audit: live session-stamp validation (deactivation, password, role and permission changes invalidate issued tokens), server-enforced forced password change, inactive-role guard and scope-type validation, document-scoped grant revocation, temporal assignment overlap rules, permission-scoped `identity-options` picker reads, user/employee sorting, department update manager/notes, `/institution-settings` UI, and the full Arabic error catalog. `dotnet build`/480 tests, `npm run build`/`npm run lint` pass; Module 03 frontend remains planned. |
| 2026-10-28 | agent | Refresh Module 01 department/settings/hierarchy coverage and split backend implementation from frontend status; use canonical 01 Organization / 02 Identity numbering, keep Module 02 audit fixes open and Module 03 frontend planned, and defer final verification results/counts. |
| 2026-10-28 | agent | Clicking a department in the Mermaid hierarchy view now opens its details: `shared/ui/mermaid-view` accepts an optional `onNodeClick` that turns flowchart nodes into focusable, activatable targets, and `departmentTreeToMermaid` returns the node-id → department-id map. View-only change; front build and lint pass. |
| 2026-10-28 | agent | Clicking a department now shows its employees: `GET /api/employees` accepts `departmentId` and `includeSubDepartments` (resolved through `IDepartmentRepository.GetDescendantIdsAsync`), the hierarchy detail dialog renders the roster with a sub-departments toggle, and `EmployeeSeeder` (Order 97, Development/Staging) seeds 28 demo employees across the 15-department tree. 406 tests pass; front build and lint pass. |
| 2026-10-28 | agent | Added `DepartmentSeeder` (Order 96, Development/Staging) so the organization hierarchy is demonstrable: 15 departments in four levels (Central Diwan → 4 directorates → 7 departments → 3 sections) seeded idempotently by code under the existing root, with unit/pipeline tests. |
| 2026-10-28 | agent | Front-end Module 01 Phase 1 (read-only): `/departments` now renders the organizational hierarchy as a native tree, a Mermaid diagram, or a table with search and a detail dialog (`shared/ui/mermaid-view`, `entities/department/model/toMermaid`, `widgets/department-hierarchy-tree`). Department write actions and employee nodes inside the tree remain pending. |
| 2026-10-28 | agent | Added administrator password reset end to end: `User.ResetPassword` + `UserPasswordResetEvent`, the `ResetUserPasswordCommand` handler/validator, `POST /api/users/{userId}/reset-password` (USER_MANAGE; forces `MustChangePassword`, records the resetting admin in the audit chain), unit tests, and the front-end `ResetPasswordDialog` reachable from the users grid row menu. Docs and both builds updated. |
| 2026-10-28 | agent | Refreshed the root status: the SPA now implements Identity Module 01 plus the complete Module 02 Identity admin front end (Phases 0–6: shared admin foundation, app shell, supporting reads, users, roles + permission matrix, employees, and change-password), uses the configured `/api` dev proxy, and is no longer the Vite template. |
| 2026-10-26 | Qoder | Removed the old `institutionaldiwan.client` project from the solution and deleted it: the server csproj no longer carries `SpaRoot`/`SpaProxy` or a client project reference, launch profiles no longer start the SPA proxy, and the API no longer serves static files or the SPA fallback. The React front end now lives in the standalone `institutionaldiwan.front` project, which is documented but holds only the Vite template. |
| 2026-10-26 | Qoder | Extended Module 09 beyond entity changes: a middleware now records every authenticated `/api` action (reads and failures included) as an `Operation` row, and logins produce a `Login` row; documented the two audit layers and the new integration tests. |
| 2026-10-25 | Cursor | Noted that client docs now track Modules 01–06 + 09 (endpoints, permissions, UI contract) while SPA code remains the Vite template. |
| 2026-10-25 | Buffy | Serialized audit-chain appends so concurrent reads, writes, and scheduled audit jobs cannot collide on the chain head; the suite now has 375 tests. |
| 2026-10-25 | opencode | Marked Modules 06 (Attachments) and 09 (Audit) implemented; documented the audit chain, scheduled checkpoint/integrity jobs, and the current 374-test suite. |
| 2026-10-22 | Buffy | Documented `Features/Identity/Login/` with its own README (the caller-profile query now lives there) and updated the known-gap list; the backend now passes one deterministic instant per transaction through every domain write. |
| 2026-10-22 | opencode | Added the missing root `README.md`: the repository layout, prerequisites, build/run/test commands, the per-module status table, and the conventions that govern work in this repository. |
