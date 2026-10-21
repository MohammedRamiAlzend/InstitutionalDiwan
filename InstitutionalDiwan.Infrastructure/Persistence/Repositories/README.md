# Infrastructure / Persistence / Repositories

> **Status:** Partially implemented — 14 of the 17 repository interfaces have implementations
> (`UserRepository`, `RoleRepository`, `PermissionRepository`, `EmployeeRepository`,
> `DepartmentRepository`, `DepartmentTypeRepository`, `DocumentRepository`, `DocumentTypeRepository`,
> `NumberingRegistryRepository`, `DocumentNumberRepository`, `DocumentAccessGrantRepository`,
> `LookupRepository`, `InstitutionSettingRepository`, `ExternalOrganizationRepository`,
> `DocumentMovementRepository`); three have none yet.

## Purpose

Concrete EF Core implementations of the repository interfaces declared in
`InstitutionalDiwan.Domain/Repositories/IDomainRepositories.cs`. Repositories are responsible only for
**loading and attaching** aggregates; they never call `SaveChanges` (that is `IUnitOfWork`, i.e. the
`DbContext` itself). Read models that need speed over richness use the application's
`ISqlConnectionFactory` instead.

## Contents

| File | Purpose |
| --- | --- |
| `UserRepository.cs` | Loads `User` aggregates (by id, username, or listing) with their role/department scopes for Identity and document-creation use cases. |
| `RoleRepository.cs` | Loads `Role` aggregates by id or code (or all, ordered by code) for authorization and role management. |
| `PermissionRepository.cs` | Loads `Permission` catalog entries by code and listing, so `GrantRolePermissionCommand` can validate the code and `GET /api/permissions` can serve the catalog. |
| `DepartmentRepository.cs` | Loads `Department` aggregates (by id, code, listing), implements `HasActiveChildrenAsync` (deactivation guard) and `GetDescendantIdsAsync` (cycle detection when moving a department under a new parent). |
| `DepartmentTypeRepository.cs` | Loads `DepartmentType` by code so department create/update can validate the type exists and is active. |
| `DocumentRepository.cs` | Loads `Document` with `Participants` and `StatusHistory`; `GetWithNumbersAsync` adds the central + department numbers (with their history) for the record/receive flows, `GetByIdsAsync` batches the register-book join, and `ListRecentAsync` backs the listing. |
| `NumberingRegistryRepository.cs` | Loads `NumberingRegistry` with its per-year `Counters`; `GetActiveForScopeAsync` picks the most specific active registry matching department/type/direction (null fields act as wildcards), while `GetActiveDepartmentRegistryAsync` returns only the registry owned by that department (no shared-default fallback). |
| `DocumentNumberRepository.cs` | Loads `DocumentNumber` rows (by id / active central number per document) and answers `ExistsByNumberAndYearAsync` and the scope-aware `ExistsInScopeAsync`; `GetDepartmentEntryAsync` and `ListByDepartmentAsync` serve the register book. Department rows are excluded from the central lifecycle and from `AnyByRegistryAsync`, which decides the registry template lock. |
| `EmployeeRepository.cs` | Loads `Employee` records by id and listing for the employee-management endpoints. |
| `DocumentAccessGrantRepository.cs` | Loads per-document `DocumentAccessGrant` rows by id and by document, for grant/revoke/list. |
| `LookupRepository.cs` | The one generic repository: resolves a kebab-case lookup slug to its `DbSet<TLookup>` through `LookupCatalog`, then lists/fetches/creates/relabels/(de)activates entries and answers `IsInUseAsync` from EF foreign-key metadata. Backs `/api/lookups/{lookup}`. |
| `InstitutionSettingRepository.cs` | Loads the single `InstitutionSetting` row (table is a singleton by convention; the seeder owns the first insert) and attaches updates. |
| `ExternalOrganizationRepository.cs` | Loads `ExternalOrganization` by id or normalized (upper-cased) code and lists them by code, for the external-correspondent CRUD endpoints. |
| `DocumentMovementRepository.cs` | Loads a `DocumentMovement` with `Steps`/`Events` and answers `HasActiveMovementsAsync(documentId)` — true while the status is not `CLOSED`/`CANCELED`; backs the document cancel/archive guard. `ListByDocumentAsync` gives a document's routing history; `ListInboxAsync`/`ListOutboxAsync` give a department's two queues, both keyed on the **current step** so a redirect moves the custody with the document. |

## Planned repositories not yet implemented

| Repository (`:` interface) | Key behaviors |
| --- | --- |
| `DocumentFileRepository` (`: IDocumentFileRepository`) | Loads `DocumentFile` with its `Versions` collection. |
| `DocumentRelationRepository` (`: IDocumentRelationRepository`) | Queries bidirectional document links and supports cycle checks when the relation type has `PreventCycles = true`. |
| `AuditLogRepository` (`: IAuditLogRepository`) | **Append-only**; `GetLatestAsync()` returns the prior `EventHash` so the next tamper-evident entry can chain to it. |
| `NotificationRepository` (`: INotificationRepository`) | Persists `Notification`, `NotificationRecipient`, and `NotificationOutbox` records. |

## Conventions

- Use `AsNoTracking()` for read-only loads; only track aggregates that will be mutated.
- Include exactly the collections a use case needs — avoid a universal "load everything" graph.
- `ValueGeneratedNever` Guid keys mean new aggregates keep the id assigned by their `Create(...)` factory.
- Concurrency conflicts surface as `DbUpdateConcurrencyException` and are translated to the application
  `ConcurrencyException` (→ HTTP 409).
- Official-number uniqueness is enforced twice: the handler pre-checks
  `ExistsByNumberAndYearAsync(text, year)` (→ `Numbering.OfficialNumberTaken`, 409) and the database
  guarantees it with the filtered `UX_DOC_NUMBERS_TEXT_YEAR` (central rows) / `UX_DOC_NUMBERS_DEPT_TEXT_YEAR`
  (one department register) indexes; `UX_DOC_NUMBERS_DOC_DEPT` is the backstop for "one line per
  document per department". A concurrent race that slips past the check surfaces as a SQL unique
  violation, which `GlobalExceptionHandlingMiddleware` maps to 409.

## Implementation status

| Repository | State |
| --- | --- |
| `UserRepository`, `RoleRepository`, `DocumentRepository` | ✅ Implemented |
| `PermissionRepository` | ✅ Implemented (2026-10-18) |
| `NumberingRegistryRepository`, `DocumentNumberRepository` | ✅ Implemented (2026-10-18) |
| `DepartmentRepository`, `DepartmentTypeRepository` | ✅ Implemented (2026-10-18) |
| `EmployeeRepository`, `DocumentAccessGrantRepository` | ✅ Implemented |
| `LookupRepository` (generic reference catalogs) | ✅ Implemented (2026-10-19) |
| `InstitutionSettingRepository`, `ExternalOrganizationRepository` | ✅ Implemented (2026-10-19) |
| `DocumentMovementRepository` | ✅ Implemented (2026-10-19; inbox/outbox queries added 2026-10-21) |
| `DocumentFileRepository`, `DocumentRelationRepository`, `AuditLogRepository`, `NotificationRepository` | ❌ Not implemented |

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-10-21 | opencode | Added the movement queue queries to `DocumentMovementRepository`: `ListByDocumentAsync` (a document's routing history), `ListInboxAsync`, and `ListOutboxAsync`. The queue filters run against the movement's **current step** — a correlated `Steps.Any(step => step.StepNo == movement.CurrentStepNo && …)` — so a redirect hands the document to the new destination's inbox and to the redirecting department's outbox. |
| 2026-10-20 | Buffy | Made the numbering queries scope-aware (`GetActiveByDocumentIdAsync` and `AnyByRegistryAsync` now only see central numbers), added `ExistsInScopeAsync`/`GetDepartmentEntryAsync`/`ListByDepartmentAsync`, and added `DocumentRepository.GetWithNumbersAsync`/`GetByIdsAsync` for department registration and the register book. |
| 2026-10-19 | Buffy | Added `InstitutionSettingRepository` (`GetSingletonAsync`/`Add`), `ExternalOrganizationRepository` (by id / normalized code / listing), and `DocumentMovementRepository` (`HasActiveMovementsAsync` = status not CLOSED/CANCELED); all registered in `Infrastructure/DependencyInjection.cs`. |
| 2026-10-19 | opencode | Added `LookupRepository` — one generic implementation covering all 33 seeded reference catalogs through a deterministic slug registry (`LookupCatalog`), with Creates/Relabel/(De)activate plus an `IsInUseAsync` guard derived from the EF model's foreign keys. Registered in `Infrastructure/DependencyInjection.cs`; also recorded the previously undocumented `EmployeeRepository` and `DocumentAccessGrantRepository`. |
| 2026-10-18 | opencode | Added `PermissionRepository` (`GetByCodeAsync`, `ListAllAsync`) and `RoleRepository.ListAllAsync`, registered in `Infrastructure/DependencyInjection.cs` to back the role-management endpoints. |
| 2026-10-18 | opencode | Added `DepartmentRepository` (listing, `HasActiveChildrenAsync`, `GetDescendantIdsAsync`) and `DepartmentTypeRepository` (`GetByCodeAsync`); both registered in `Infrastructure/DependencyInjection.cs` to back the department CRUD + tree endpoints. |
| 2026-10-18 | opencode | Added `DocumentNumberRepository` (incl. `ExistsByNumberAndYearAsync` duplicate-number check) and `NumberingRegistryRepository` (scope matching with most-specific-first ordering); both registered in `Infrastructure/DependencyInjection.cs` to support official-number assignment at document creation. |
