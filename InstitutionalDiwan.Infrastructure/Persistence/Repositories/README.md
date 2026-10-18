# Infrastructure / Persistence / Repositories

> **Status:** Partially implemented — `UserRepository`, `RoleRepository`, `DepartmentRepository`,
> `DepartmentTypeRepository`, `DocumentRepository`, `NumberingRegistryRepository`, and
> `DocumentNumberRepository` exist; the remaining interfaces have no implementation yet.

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
| `RoleRepository.cs` | Loads `Role` aggregates by id or code for authorization and role management. |
| `DepartmentRepository.cs` | Loads `Department` aggregates (by id, code, listing), implements `HasActiveChildrenAsync` (deactivation guard) and `GetDescendantIdsAsync` (cycle detection when moving a department under a new parent). |
| `DepartmentTypeRepository.cs` | Loads `DepartmentType` by code so department create/update can validate the type exists and is active. |
| `DocumentRepository.cs` | Loads `Document` with `Participants` and `StatusHistory`, supports the recent-documents listing, and attaches new documents. |
| `NumberingRegistryRepository.cs` | Loads `NumberingRegistry` with its per-year `Counters`; `GetActiveForScopeAsync` picks the most specific active registry matching department/type/direction (null fields act as wildcards). |
| `DocumentNumberRepository.cs` | Loads issued `DocumentNumber` rows (by id / active per document) and answers `ExistsByNumberAndYearAsync`, the duplicate-official-number check used by `CreateDocumentCommandHandler`. |

## Planned repositories not yet implemented

| Repository (`:` interface) | Key behaviors |
| --- | --- |
| `DocumentMovementRepository` (`: IDocumentMovementRepository`) | Eagerly loads `Steps` and `Events`; implements `HasActiveMovementsAsync(documentId)` used by document archive/cancel guards. |
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
  guarantees it with the `UX_DOC_NUMBERS_TEXT_YEAR` unique index; a concurrent race that slips past
  the check surfaces as a SQL unique violation, which `GlobalExceptionHandlingMiddleware` maps to 409.

## Implementation status

| Repository | State |
| --- | --- |
| `UserRepository`, `RoleRepository`, `DocumentRepository` | ✅ Implemented |
| `NumberingRegistryRepository`, `DocumentNumberRepository` | ✅ Implemented (2026-10-18) |
| `DepartmentRepository`, `DepartmentTypeRepository` | ✅ Implemented (2026-10-18) |
| `DocumentMovementRepository`, `DocumentFileRepository`, `DocumentRelationRepository`, `AuditLogRepository`, `NotificationRepository` | ❌ Not implemented |

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-10-18 | opencode | Added `DepartmentRepository` (listing, `HasActiveChildrenAsync`, `GetDescendantIdsAsync`) and `DepartmentTypeRepository` (`GetByCodeAsync`); both registered in `Infrastructure/DependencyInjection.cs` to back the department CRUD + tree endpoints. |
| 2026-10-18 | opencode | Added `DocumentNumberRepository` (incl. `ExistsByNumberAndYearAsync` duplicate-number check) and `NumberingRegistryRepository` (scope matching with most-specific-first ordering); both registered in `Infrastructure/DependencyInjection.cs` to support official-number assignment at document creation. |
