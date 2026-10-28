# InstitutionalDiwan.Tests — xUnit test suite

> **Status:** Implemented — tests covering domain handlers, department register numbering, the
> receipt gate, the movement routing workflow (including the Central Diwan hub and rejection routing),
> the department inbox/outbox, attachments (atomic first upload, versions, SHA-256 streaming, magic
> bytes), movement participation, the tamper-evident audit chain (hashing, projections, checkpoints,
> integrity reports, redaction, composition-root wiring, real document create/update auditing,
> action-level recording and middleware), seeding, authorization, paging/search/filtering, the caller
> profile, deterministic time, and the generic lookups API.

## Purpose

Unit and integration tests for all layers. Handler tests use hand-written fakes (`Support/`) so a use
case can run without a database; anything that depends on EF behaviour (queries, foreign keys, unique
indexes) runs against a private SQLite in-memory database through `Support/TestContextFactory`.

## Layout

| Folder | Contents |
| --- | --- |
| `Application/` | Handler/validator tests per slice — Identity (users, roles, permissions, employees, access grants, login/register/change password, admin password reset, `GetUserProfileQueryHandlerTests`), Organization/Departments + `InstitutionSettingsHandlerTests`, Correspondence/`Documents` (`DocumentLifecycleHandlerTests`, `ListDepartmentDocumentsQueryHandlerTests`) + `ExternalOrganizationsHandlerTests`, Numbering (`OfficialNumberLifecycleHandlerTests`, `DepartmentRegisterNumberingTests`), Movements (`MovementWorkflowHandlerTests`), Attachments (`AttachmentHandlerTests`), Audit (`AuditDomainTests`, `AuditHandlerTests`), `UnitOfWorkClockTests` (handler-stamped times come from `IUnitOfWork.UtcNow`), and `LookupHandlerTests.cs`. |
| `Storage/` | `LocalFileStorageServiceTests` (layout, streamed checksum, read-back, root-escape guard) and `FileSignatureValidatorTests` (PDF/PNG/JPEG/TIFF/ZIP/OLE/SVG magic bytes, extension allow-list, size limit). |
| `Authorization/` | `PermissionAuthorizationHandlerTests` (policy handler + `Permissions.All` integrity), `DepartmentScopeEvaluatorTests` (primary department, institution/dept-tree scope, temporal assignments incl. `IncludeSubtree` and ended assignments), `DocumentAccessEvaluatorTests` (authorship, grants incl. revoked, scope, confidentiality, movement participation, anonymous). |
| `Persistence/` | `DocumentMovementRepositoryTests` — the inbox/outbox queries against a real SQLite database; `AuditLogRepositoryTests` — filtered search, range reads, checkpoint/integrity persistence; `TamperEvidentAuditInterceptorTests` — domain-event projection (including login), sensitive-field redaction, and request-only writes against SQLite; `AuditActionRecorderTests` — the isolated-scope recorder writes one valid chain row without flushing the caller's pending changes; `CorrespondenceAuditIntegrationTests` — real `CreateDocumentCommand`/`UpdateDraftMetadataCommand` handlers through repositories + DbContext + interceptor produce entity audit rows with old/new values. |
| `Server/` | `OpenApiDocumentationTests` — every controller endpoint must carry an `[EndpointSummary]`, an `[EndpointDescription]`, and a `<param>` description with an example. `Module09DependencyInjectionTests` — the audit repository, appender, action recorder, EF interceptor, hosted jobs, permission policies, and MediatR handlers resolve from the real `AddApplication` + `AddInfrastructure` + `AddInstitutionalAuthentication` composition root. `AuditActionMiddlewareTests` — action-level rows for authenticated calls (create/view), skipping anonymous and non-`/api` requests, and failure recording + rethrow. |
| `Seed/` | Seeder and pipeline tests — `PermissionSeederTests`, `RoleSeederTests`, `UserSeederTests`, `DepartmentTypeSeederTests`, `DepartmentSeederTests`, `EmployeeSeederTests`, `SeederRunnerTests`, `SeedingPipelineTests`. |
| `Support/` | Shared test doubles: `TestContextFactory` (SQLite in-memory `DiwanDbContext`), fake repositories/unit-of-work, fake hasher and JWT provider. |

### Module 01 regression files

| File | Purpose |
| --- | --- |
| `Application/UpdateDepartmentCommandHandlerTests.cs` | Tests department updates, manager/notes behavior, root-preserving edits of the designated hub, and nullable/empty/500/501-character notes validation. |
| `Application/MoveDepartmentCommandHandlerTests.cs` | Tests hierarchy guards and no-op root moves of the designated hub; supplies the settings repository dependency. |
| `Application/DeleteDepartmentCommandHandlerTests.cs` | Tests deactivation and active-child guards with the settings repository dependency. |
| `Application/ActivateDepartmentCommandHandlerTests.cs` | Tests missing/inactive parents without mutation or saving, parent-first activation, root activation, and already-active behavior. |
| `Application/InstitutionSettingsHandlerTests.cs` | Tests hub update/move/delete protection before and after a real settings transfer, inactive/null hub and invalid lookup/employee branches, and logo-length/required-hub validation. |

### Module 02 security regression files

| File | Purpose |
| --- | --- |
| `Server/JwtSecurityPipelineTests.cs` | Runs real JWT bearer authentication, MVC auth/lookup/identity-options endpoints, identity handlers, and scoped repositories over private SQLite + loopback Kestrel; covers stale-session revocation, forced-change recovery, inactive-role filtering, mandatory stamps, credential non-disclosure, concurrent-login compatibility, and the exact picker-permission matrix for department and user options. |
| `Authorization/DepartmentScopeEvaluatorTests.cs` | Verifies live account/scope checks before primary/global grants and half-open temporal assignment windows. |
| `Application/UserRoleCommandHandlerTests.cs` | Verifies inactive role assignment fails without mutation or save, alongside existing grant/revoke behavior. |
| `Application/IdentityAdminCorrectnessTests.cs` | Covers department-assignment overlap rejection (including shared inclusive endpoints and open-ended intervals), ended rows retained as history, admin history vs. effective-permission semantics (inclusive dates, active roles only), and scope-type validation/handler rejection before saving. |
| `Application/RevokeDocumentAccessCommandHandlerTests.cs` | Grant revocation, missing/revoked conflicts, and rejection without mutation when the route document does not own the grant. |
| `Application/IdentityOptionQueryHandlerTests.cs` | Username search/paging for the user picker, inactive rows retained for labelling, and minimal `userId`/`username`/`isActive` projection. |

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
- `AuditDomainTests` covers the SHA-256 chain: the first row links to genesis, a valid chain verifies,
  tampering with a stored field breaks the recomputed hash, a wrong previous hash breaks the link,
  UTC/millisecond normalization survives a round-trip, and `Reseal` re-chains onto a new predecessor.
- `AuditHandlerTests` covers the audit use cases: search appends a self-audit entry, a checkpoint
  verifies before sealing and refuses an empty range, integrity checks record pass or fail, and both
  commands no-op with `Audit.NoAuditLogs` when the chain is empty.
- `AuditLogRepositoryTests` covers SQL-side filtering and paging, ordered range reads, the unique
  previous-hash guard that prevents a chain fork, and checkpoint / integrity-report persistence
  against SQLite.
- `TamperEvidentAuditInterceptorTests` proves domain events become audit rows with request metadata,
  multiple entries chain in order, password/secret fields never reach `OldValuesJson`/`NewValuesJson`,
  writes outside an HTTP request produce no automatic audit row, audit rows are not re-audited, and a
  login produces a `Login` row attributed to the user.
- `CorrespondenceAuditIntegrationTests` is the end-to-end proof that ordinary writes are audited: the
  real create-document handler (document Created + Update + official-number Create rows) and the real
  update-draft handler (an Update row with parseable old/new Title) run through the actual
  repositories, `DiwanDbContext`, and `TamperEvidentAuditInterceptor`, stamped with actor, department,
  IP, and correlation id, over an intact chain.
- `AuditActionRecorderTests` builds the recorder over a real SQLite context and proves it commits from
  an isolated scope: a pending unsaved `Role` in the request scope is not persisted, and exactly one
  valid chain row is written with actor/department/IP/correlation.
- `AuditActionMiddlewareTests` covers action-level auditing: an authenticated POST maps to
  `CORRESPONDENCE`/`Create`/`Operation` with actor, department, and request metadata; a GET maps to
  `View`; anonymous `/api/auth/login` and non-`/api` paths are skipped; an exception records status 500
  plus the error type and is rethrown.
- `Module09DependencyInjectionTests` locks the Module 09 composition root: `IAuditLogRepository`,
  `AuditLogAppender`, `IAuditActionRecorder`, `IRequestContext`, `ITransactionManager`,
  `IDateTimeProvider`, and `TamperEvidentAuditInterceptor` resolve; the interceptor is attached to
  `DiwanDbContext`; both audit hosted services are registered; `AUDIT_VIEW` / `AUDIT_MANAGE` policies
  exist; and all four audit MediatR handlers resolve.
- `SeedingPipelineTests` runs the full ordered seeder set per environment and asserts the resulting
  permission/role/lookup counts, including idempotency on repeated runs.

## Implementation status

| Item | State |
| --- | --- |
| Module 01 correctness regressions | 24 cases added; build/test execution deferred while the API is running. |

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-10-28 | Qoder | Module 03 Phase 2: the picker matrix now expects `DOCUMENT_VIEW` to read department options (users stay restricted), matching the server change that lets read-only registry users resolve owning-department labels; 487 tests pass. |
| 2026-10-28 | Qoder | Module 03 Phase 0: added `IdentityOptionQueryHandlerTests` and a real-pipeline picker-permission matrix (departments open to document permissions, users limited to `USER_MANAGE`/`ACCESS_GRANT_MANAGE`/`DOCUMENT_EDIT`); 480 → 487 tests. |
| 2026-10-28 | Qoder | Add designated-hub protection/transfer regressions for update, move, and delete; preserve root edits and no-op moves; cover parent-first activation, logo/notes boundaries, and missing settings-validation branches. Update handler constructor call sites; defer build/test until the running API is stopped. |
| 2026-10-28 | Qoder | Added `Seed/EmployeeSeederTests` (28 demo employees attached to every department, idempotency, existing numbers kept, missing-tree failure, environment gating), extended `ListEmployeesQueryHandlerTests` with the department filter (exact, descendants, unknown department), and wired `EmployeeSeeder` into `SeedingPipelineTests`. 398 → 406 tests. |
| 2026-10-28 | Qoder | Added `Seed/DepartmentSeederTests` (four-level demo tree under the Central Diwan root, depth assertion, environment gating, idempotency, existing rows untouched, missing-root failure) and wired `DepartmentSeeder` into `SeedingPipelineTests`. 393 → 398 tests. |
| 2026-10-28 | agent | Added `Application/ResetUserPasswordCommandHandlerTests` (hash replaced, old password no longer verifies, `MustChangePassword` forced, single save, missing user → `Identity.UserNotFound`) and `ResetUserPasswordCommandValidatorTests` (empty id, blank password, too-short password). 386 → 393 tests. |
| 2026-10-26 | Qoder | Added `Persistence/CorrespondenceAuditIntegrationTests` (real document create/update commands write entity audit rows), `Persistence/AuditActionRecorderTests` (isolated scope), and `Server/AuditActionMiddlewareTests` (create/view/skip/failure rows); extended the interceptor login case, the composition-root test, lookup seeding (8 action / 13 entity types), and the seeding counts. 378 → 386 tests. |
| 2026-10-26 | Qoder | Added `Server/Module09DependencyInjectionTests` to prove Module 09 is injected through the production composition root (audit repository/appender, EF interceptor, hosted jobs, permission policies, MediatR handlers). 375 → 378 tests. |
| 2026-10-25 | Buffy | Added the `AuditDomainTests.Reseal` case after audit appends were serialized under a chain lock. 374 → 375 tests. |
| 2026-10-25 | opencode | Added Module 09 coverage: `AuditDomainTests`, `AuditHandlerTests`, `AuditLogRepositoryTests`, and `TamperEvidentAuditInterceptorTests`, plus audit fakes and lookup seeding. 348 → 374 tests. |
| 2026-10-25 | opencode | Added coverage for the missing Module 02/05/06 gaps: Central Diwan routing, rejection routing to the hub, stale-step refusal, movement participation, and the atomic first upload. 338 → 348 tests. |
| 2026-10-25 | opencode | Added Module 06 coverage: `Application/AttachmentHandlerTests` (create/upload/archive/list/download/verify), `Storage/LocalFileStorageServiceTests`, and `Storage/FileSignatureValidatorTests`, with shared attachment fakes. 312 → 338 tests. |
| 2026-10-22 | Buffy | Added `GetUserProfileQueryHandlerTests` (authenticated projection + unauthenticated 401), `ListDepartmentDocumentsQueryHandlerTests` (department filter, paging, 404, out-of-scope 403), and `UnitOfWorkClockTests` (document, numbering, and login writes all carry the unit of work's instant). 303 → 312 tests. |
| 2026-10-22 | opencode | Extended the list-handler tests for the paged contracts (documents, users, employees, external organizations) with cases for paging, clamping, search, sorting, and filtering; the fakes now implement `ListPagedAsync`. 294 → 303 tests. |
| 2026-10-21 | opencode | Added `Application/MovementWorkflowHandlerTests` (routing workflow + inbox/outbox) and `Persistence/DocumentMovementRepositoryTests` (the queue queries against SQLite); extended `FakeDocumentMovementRepository` with the list/queue members. |
| 2026-10-20 | Buffy | Added `DepartmentRegisterNumberingTests` (11 cases) for department register entries and the register-gated receipt; updated the seeding pipeline expectations for the new `DOCUMENT_NUMBER_SCOPES` lookup and the two department event types, and extended the fakes with the new repository members. |
| 2026-10-19 | Buffy | Added `InstitutionSettingsHandlerTests`, `ExternalOrganizationHandlerTests`, `DocumentLifecycleHandlerTests`, `OfficialNumberLifecycleHandlerTests`, `DepartmentScopeEvaluatorTests`, and `DocumentAccessEvaluatorTests` (60+ new tests) with shared fakes in `Support/FakeServiceRepositories.cs`; updated the permission (13 → 14), role-grant (16 → 17), department-type (4 → 5), and seeder-count expectations for `NUMBERING_MANAGE` and the new `InstitutionSettingSeeder`. |
| 2026-10-19 | opencode | Added `Application/LookupHandlerTests.cs` (13 tests over the generic lookups API) and updated the permission/role/seeding counts for the new `LOOKUP_MANAGE` permission; documented the project layout in this README. |
