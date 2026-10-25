# InstitutionalDiwan.Tests — xUnit test suite

> **Status:** Implemented — 348 tests covering domain handlers, department register numbering, the
> receipt gate, the movement routing workflow (including the Central Diwan hub and rejection routing),
> the department inbox/outbox, attachments (atomic first upload, versions, SHA-256 streaming, magic
> bytes), movement participation, seeding, authorization, paging/search/filtering, the caller
> profile, deterministic time, and the generic lookups API.

## Purpose

Unit and integration tests for all layers. Handler tests use hand-written fakes (`Support/`) so a use
case can run without a database; anything that depends on EF behaviour (queries, foreign keys, unique
indexes) runs against a private SQLite in-memory database through `Support/TestContextFactory`.

## Layout

| Folder | Contents |
| --- | --- |
| `Application/` | Handler/validator tests per slice — Identity (users, roles, permissions, employees, access grants, login/register/change password, `GetUserProfileQueryHandlerTests`), Organization/Departments + `InstitutionSettingsHandlerTests`, Correspondence/`Documents` (`DocumentLifecycleHandlerTests`, `ListDepartmentDocumentsQueryHandlerTests`) + `ExternalOrganizationsHandlerTests`, Numbering (`OfficialNumberLifecycleHandlerTests`, `DepartmentRegisterNumberingTests`), Movements (`MovementWorkflowHandlerTests`), Attachments (`AttachmentHandlerTests`), `UnitOfWorkClockTests` (handler-stamped times come from `IUnitOfWork.UtcNow`), and `LookupHandlerTests.cs`. |
| `Storage/` | `LocalFileStorageServiceTests` (layout, streamed checksum, read-back, root-escape guard) and `FileSignatureValidatorTests` (PDF/PNG/JPEG/TIFF/ZIP/OLE/SVG magic bytes, extension allow-list, size limit). |
| `Authorization/` | `PermissionAuthorizationHandlerTests` (policy handler + `Permissions.All` integrity), `DepartmentScopeEvaluatorTests` (primary department, institution/dept-tree scope, temporal assignments incl. `IncludeSubtree` and ended assignments), `DocumentAccessEvaluatorTests` (authorship, grants incl. revoked, scope, confidentiality, movement participation, anonymous). |
| `Persistence/` | `DocumentMovementRepositoryTests` — the inbox/outbox queries against a real SQLite database, so the SQL EF Core generates for the correlated current-step filters is verified and not only the in-memory fake. |
| `Server/` | `OpenApiDocumentationTests` — every controller endpoint must carry an `[EndpointSummary]`, an `[EndpointDescription]`, and a `<param>` description with an example. |
| `Seed/` | Seeder and pipeline tests — `PermissionSeederTests`, `RoleSeederTests`, `UserSeederTests`, `DepartmentTypeSeederTests`, `SeederRunnerTests`, `SeedingPipelineTests`. |
| `Support/` | Shared test doubles: `TestContextFactory` (SQLite in-memory `DiwanDbContext`), fake repositories/unit-of-work, fake hasher and JWT provider. |

## Conventions

- One test class per use case (handler + validator), named `...Tests`; assertions use xUnit's built-in
  `Assert` with `Assert.True(result.IsSuccess)` / `result.Error.Code` checks.
- Handler tests construct the handler directly (`internal` handlers are visible via
  `InternalsVisibleTo("InstitutionalDiwan.Tests")` on the Application and Infrastructure projects).
- Database-backed tests use `TestContextFactory.Create()`, which returns a context over an open SQLite
  `:memory:` connection that is disposed with the test.
- Seeding assertions count rows (lookup totals, `Permissions.All` length, role grants) so a new
  permission or lookup must be reflected in the tests — keeping the seed catalog and the tests in sync.

## Notable coverage

- `LookupHandlerTests` exercises the slug registry (all 33 catalogs), active/inactive listing, unknown
  slug → 404, missing entry → 404, duplicate id → 409, read-only catalog → 400, relabel, and the
  reference-in-use guard → 409 (including a reference held through a nullable FK), all against SQLite.
- `DepartmentRegisterNumberingTests` covers the per-department register: typed number + date stored
  without touching the central number, a second arrival that keeps the number and appends a date, a
  conflicting second number → 409, the same number allowed in two departments, a missing department
  registry → 409, a draft without a central number → 409, a taken number → 409, `Cancel` still finding
  the central row when department entries exist, and the receipt gate (blocked, then allowed and
  stamped with the register number).
- `MovementWorkflowHandlerTests` covers the routing workflow end to end: opening a movement (only a
  REGISTERED document, one active movement per document, the source department taken from the caller),
  the Central Diwan hub (ordinary → ordinary refused, ordinary → hub allowed, redirect re-checked),
  send, receive, complete, reject (routed back to the hub as a new sent step; a hub rejection stays
  rejected), return, redirect, close, and cancel, plus a stale step id → 409 — plus the movement
  queries and the department inbox/outbox (the document waiting for the destination, the document
  still out, and a department outside the caller's scope → 403).
- `DocumentMovementRepositoryTests` runs the inbox/outbox queries against SQLite: a sent movement lands
  in the destination's inbox and the sender's outbox, a draft lands in neither, a closed movement lands
  in neither, a rejected document comes back to the sender's inbox, and a redirect moves the custody —
  the new destination gets the inbox, the redirecting department keeps the outbox.
- `AttachmentHandlerTests` covers Module 06: creating a file (MANAGE required), the atomic first upload
  (file + version 1 in one save), uploading a version (checksum recorded, current version flipped,
  invalid signature and archived file refused), downloading (DOWNLOAD required), verifying checksums,
  and listing a document's files.
- `DocumentAccessEvaluatorTests` covers movement participation: the current step's sender/receiver
  gets VIEW/DOWNLOAD outside the owning department, a draft step grants nothing, and a confidential
  document still requires an explicit grant.
- `LocalFileStorageServiceTests` writes real temp files to prove the `{Year}/{Month}/{DocumentId}`
  layout, one-pass SHA-256, read-back, missing-file behaviour, and refusal of a `../` path escape.
- `SeedingPipelineTests` runs the full ordered seeder set per environment and asserts the resulting
  permission/role/lookup counts, including idempotency on repeated runs.

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-10-25 | opencode | Added coverage for the missing Module 02/05/06 gaps: Central Diwan routing, rejection routing to the hub, stale-step refusal, movement participation, and the atomic first upload. 338 → 348 tests. |
| 2026-10-25 | opencode | Added Module 06 coverage: `Application/AttachmentHandlerTests` (create/upload/archive/list/download/verify), `Storage/LocalFileStorageServiceTests`, and `Storage/FileSignatureValidatorTests`, with shared attachment fakes. 312 → 338 tests. |
| 2026-10-22 | Buffy | Added `GetUserProfileQueryHandlerTests` (authenticated projection + unauthenticated 401), `ListDepartmentDocumentsQueryHandlerTests` (department filter, paging, 404, out-of-scope 403), and `UnitOfWorkClockTests` (document, numbering, and login writes all carry the unit of work's instant). 303 → 312 tests. |
| 2026-10-22 | opencode | Extended the list-handler tests for the paged contracts (documents, users, employees, external organizations) with cases for paging, clamping, search, sorting, and filtering; the fakes now implement `ListPagedAsync`. 294 → 303 tests. |
| 2026-10-21 | opencode | Added `Application/MovementWorkflowHandlerTests` (routing workflow + inbox/outbox) and `Persistence/DocumentMovementRepositoryTests` (the queue queries against SQLite); extended `FakeDocumentMovementRepository` with the list/queue members. |
| 2026-10-20 | Buffy | Added `DepartmentRegisterNumberingTests` (11 cases) for department register entries and the register-gated receipt; updated the seeding pipeline expectations for the new `DOCUMENT_NUMBER_SCOPES` lookup and the two department event types, and extended the fakes with the new repository members. |
| 2026-10-19 | Buffy | Added `InstitutionSettingsHandlerTests`, `ExternalOrganizationHandlerTests`, `DocumentLifecycleHandlerTests`, `OfficialNumberLifecycleHandlerTests`, `DepartmentScopeEvaluatorTests`, and `DocumentAccessEvaluatorTests` (60+ new tests) with shared fakes in `Support/FakeServiceRepositories.cs`; updated the permission (13 → 14), role-grant (16 → 17), department-type (4 → 5), and seeder-count expectations for `NUMBERING_MANAGE` and the new `InstitutionSettingSeeder`. |
| 2026-10-19 | opencode | Added `Application/LookupHandlerTests.cs` (13 tests over the generic lookups API) and updated the permission/role/seeding counts for the new `LOOKUP_MANAGE` permission; documented the project layout in this README. |
