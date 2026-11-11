# Infrastructure / Persistence / Repositories

> **Status:** Partially implemented — 24 of the 25 repository interfaces have implementations
> (`UserRepository`, `RoleRepository`, `PermissionRepository`, `EmployeeRepository`,
> `DepartmentRepository`, `DepartmentTypeRepository`, `DocumentRepository`, `DocumentTypeRepository`,
> `NumberingRegistryRepository`, `DocumentNumberRepository`, `DocumentAccessGrantRepository`,
> `LookupRepository`, `InstitutionSettingRepository`, `ExternalOrganizationRepository`,
> `DocumentMovementRepository`, `DocumentFileRepository`, `DocumentFileTypeRepository`,
> `FileStorageProviderRepository`, `AuditLogRepository`, `DocumentFieldRepository`,
> `DocumentTypeSchemaVersionRepository`, `DocumentArchiveRepository`, `SavedSearchRepository`, `DocumentRelationRepository`); the notification repository
> has none yet.

## Purpose

Concrete EF Core implementations of the repository interfaces declared in
`InstitutionalDiwan.Domain/Repositories/IDomainRepositories.cs`. Repositories are responsible only for
**loading and attaching** aggregates; they never call `SaveChanges` (that is `IUnitOfWork`, i.e. the
`DbContext` itself). Read models that need speed over richness use the application's
`ISqlConnectionFactory` instead.

## Contents

| File | Purpose |
| --- | --- |
| `UserRepository.cs` | Loads `User` aggregates (by id, username, or listing) with their role/department scopes for Identity and document-creation use cases; `ListPagedAsync` orders by username and pages with a username search. |
| `RoleRepository.cs` | Loads `Role` aggregates by id or code (or all, ordered by code) for authorization and role management. |
| `PermissionRepository.cs` | Loads `Permission` catalog entries by code and listing, so `GrantRolePermissionCommand` can validate the code and `GET /api/permissions` can serve the catalog. |
| `DepartmentRepository.cs` | Loads `Department` aggregates (by id, code, listing), implements `HasActiveChildrenAsync` (deactivation guard) and `GetDescendantIdsAsync` (cycle detection when moving a department under a new parent). |
| `DepartmentTypeRepository.cs` | Loads `DepartmentType` by code so department create/update can validate the type exists and is active. |
| `DocumentRepository.cs` | Loads `Document` with `Participants`, `StatusHistory` and the dynamic `FieldValues` (split query, so the collections do not multiply rows); `GetWithNumbersAsync` adds the central + department numbers (with their history) for the record/receive flows, `GetByIdsAsync` batches the register-book join, and `ListPagedAsync` backs the listing with the caller's `VIEW` access, its status/department/direction/type filters, free-text search, sort keys, and `Skip`/`Take`. |
| `DocumentFieldFilterQuery.cs` | The dynamic-field filter predicate shared by the document registry and the archive search. |
| `DocumentViewAccessQuery.cs` | `WhereViewableBy`, the `VIEW` rule of `DocumentAccessEvaluator` as a query filter (authorship, a valid grant, or scope or current-movement participation for documents confidentiality does not restrict), shared by the document list and the archive search so there is one SQL copy of the rule; persistence tests hold it equal to the evaluator. |
| `DocumentFieldRepository.cs` | Dynamic document fields: `GetByIdAsync`, `GetByKeyAsync` (the unique key per document type), `ListByDocumentTypeAsync` (no-tracking) and `Add`. |
| `DocumentTypeSchemaVersionRepository.cs` | Field-schema versions of a document type, always loaded with their `Fields`: `GetByIdAsync`, the single editable `GetDraftAsync`, the current `GetPublishedAsync`, `GetLatestVersionNumberAsync` (0 when none) for numbering the next draft, `ListByDocumentTypeAsync` (newest first), `Add` and `Remove` (discarding a draft). |
| `NumberingRegistryRepository.cs` | Loads `NumberingRegistry` with its per-year `Counters`; `GetActiveForScopeAsync` picks the most specific active registry matching department/type/direction (null fields act as wildcards), while `GetActiveDepartmentRegistryAsync` returns only the registry owned by that department (no shared-default fallback). |
| `DocumentNumberRepository.cs` | Loads `DocumentNumber` rows (by id / active central number per document) and answers `ExistsByNumberAndYearAsync` and the scope-aware `ExistsInScopeAsync`; `GetDepartmentEntryAsync` and `ListByDepartmentAsync` serve the register book. Department rows are excluded from the central lifecycle and from `AnyByRegistryAsync`, which decides the registry template lock. |
| `EmployeeRepository.cs` | Loads `Employee` records by id and listing for the employee-management endpoints; `ListPagedAsync` orders by Arabic name and pages with a name/employee-number search and an optional department-id set (the handler supplies one id, or the id plus descendants, for the department roster). |
| `DocumentAccessGrantRepository.cs` | Loads per-document `DocumentAccessGrant` rows by id and by document, for grant/revoke/list. |
| `LookupRepository.cs` | The one generic repository: resolves a kebab-case lookup slug to its `DbSet<TLookup>` through `LookupCatalog`, then lists/fetches/creates/relabels/(de)activates entries and answers `IsInUseAsync` from EF foreign-key metadata. Backs `/api/lookups/{lookup}`. |
| `InstitutionSettingRepository.cs` | Loads the single `InstitutionSetting` row (table is a singleton by convention; the seeder owns the first insert) and attaches updates. |
| `ExternalOrganizationRepository.cs` | Loads `ExternalOrganization` by id or normalized (upper-cased) code and lists them by code; `ListPagedAsync` adds the active filter, a code/name search, and `Skip`/`Take` for the external-correspondent CRUD endpoints. |
| `PersonRepository.cs` | Persons and their read models: name suggestions, the access-filtered list, a person's documents with current location and last action, the timeline merged from five sources, and join candidates (other unarchived, uncanceled documents of the same type that share a person and that the caller may view). |
| `DecisionTypeRepository.cs` | Decision types and the document types each one may be issued on. |
| `DocumentDecisionRepository.cs` | A document's decisions with their appeals, the one in force, the open department visit that shows who holds the file, and the paged register filtered by `WhereViewableBy`. |
| `DocumentMovementRepository.cs` | Loads a `DocumentMovement` with `Steps`/`Events`, answers `HasActiveMovementsAsync(documentId)` (true while the status is not `CLOSED`/`CANCELED`; backs the document cancel/archive guard), and returns `GetActiveByDocumentIdAsync` for the access evaluator's movement participation. `ListByDocumentAsync` gives a document's routing history; `ListInboxAsync`/`ListOutboxAsync` give a department's two queues, both keyed on the **current step** so a rejection routed to the Central Diwan or a redirect moves the custody with the document. |
| `DocumentFileRepository.cs` | Loads a `DocumentFile` with its full `Versions` chain by id or per document (oldest first), so the Attachments handlers can append versions and project history. Also holds `DocumentFileTypeRepository` (file-type extension/size limits) and `FileStorageProviderRepository` (by id or active default) in the same file. |
| `DocumentArchiveRepository.cs` | Module 07 archive access: appends `DocumentArchiveEvent` rows, lists a document's events newest first, and `SearchAsync` pages archived documents with their latest archive event. The `VIEW` rule (`DocumentViewAccessQuery`) is part of the query, so paging counters never include a document the caller cannot see; a persistence test holds the two rules equal. |
| `SavedSearchRepository.cs` | A user's personal `SavedSearch` rows: by id, listed by name, the single default, name-uniqueness and per-user count checks, `Add` and `Remove`. |
| `DocumentRelationRepository.cs` | Module 08 relation access: by id, by document in both directions, relation types, exact-row existence, tracked `ListOutgoingAsync` (used to find and remove a `JoinedInto` link), `ListTouchingAsync` for the graph, and the paged relation history. `WouldCreateCycleAsync` walks the type's edges breadth first and, on SQL Server inside a transaction, first takes an `sp_getapplock` per relation type so two opposite links cannot both pass the check. |
| `AuditLogRepository.cs` | **Append-only** audit access: `GetLatestAsync` for the next hash link, `GetByIdAsync` for range anchoring, `ListRangeAsync` for ordered chain walks, `SearchAsync` for filtered paging, and `AddCheckpoint`/`AddIntegrityCheck` plus their latest/listing queries. No update or delete path exists. |
| `NotificationRepository.cs` | Module 10 inbox and outbox access: paged own-inbox with type and unread filters, unread count, an owner-only recipient lookup, a set-based mark-all-read, recipients and due outbox rows for the job, and the retention purges. |
| `NotificationRecipientDirectory.cs` | Who may be notified: active users of a department (primary department or current assignment) and the SECRET/TOP_SECRET filter (creator or valid grant only). |

## Planned repositories not yet implemented

| Repository (`:` interface) | Key behaviors |
| --- | --- |
| `DocumentRelationRepository` (`: IDocumentRelationRepository`) | Queries bidirectional document links and supports cycle checks when the relation type has `PreventCycles = true`. |
| `NotificationRepository` (`: INotificationRepository`) | Persists `Notification`, `NotificationRecipient`, and `NotificationOutbox` records. |

## Conventions

- Use `AsNoTracking()` for read-only loads; only track aggregates that will be mutated.
- Paged reads use `ListPagedAsync(PageRequest page, …)`: apply the filter to the `IQueryable`, then
  `CountAsync`, then `Skip(page.Skip).Take(page.Take)`, and return `PagedList<T>(items, totalCount)`.
  Never materialize a table and page it in memory.
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
| `DocumentMovementRepository` | ✅ Implemented (2026-10-19; inbox/outbox added 2026-10-21; active-by-document added 2026-10-25) |
| `DocumentFileRepository`, `DocumentFileTypeRepository`, `FileStorageProviderRepository` | ✅ Implemented (2026-10-25) |
| `AuditLogRepository` | ✅ Implemented (2026-10-25) |
| `DocumentArchiveRepository`, `SavedSearchRepository` | ✅ Implemented (2026-11-03) |
| `DocumentRelationRepository` | ✅ Implemented (2026-11-03) |
| `DecisionTypeRepository`, `DocumentDecisionRepository` | ✅ Implemented (2026-11-11) |
| `NotificationRepository` | ❌ Not implemented |

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-11-11 | Claude | `ListOutgoingAsync` returns tracked relations of one type leaving a document. `ListJoinCandidatesAsync` lists other viewable files of the same type that share a person. No schema change. |
| 2026-11-11 | Claude | `DocumentDecisionRepository` loads a decision's appeals with the decision, including the untracked document list. |
| 2026-11-11 | Claude | Added `DecisionTypeRepository` and `DocumentDecisionRepository`. The decision register joins documents through `WhereViewableBy`, so a caller never sees a decision on a document they cannot view. |
| 2026-11-11 | Claude | `DocumentViewAccessQuery` adds the archive diwan's reading access (the document's latest movement that was not canceled ended at the archive diwan and was received), equal to the new rule in `DocumentAccessEvaluator`. |
| 2026-11-10 | Claude | Field and schema-version repositories read one form (creation form by default, or a department record form); visits load their record values; `FindDocumentIdByVisitAsync`. |
| 2026-11-09 | Claude | `DocumentRepository.ListPagedAsync` filters by a number in any department register. |
| 2026-11-09 | Claude | Document search and the `title` sort use `DisplayName`; search no longer reads the hidden title or summary. |
| 2026-11-09 | Claude | Added `PersonRepository`; `DocumentRepository.GetByIdAsync` includes `Persons`; `LookupCatalog` gained `document-person-roles`, `genders`, `nationalities`. |
| 2026-11-08 | Claude | Moved the field-filter predicate to `DocumentFieldFilterQuery` (registry and archive, multi-select match); `DocumentRepository` sorts by a field and adds `ListFieldValuesAsync`. |
| 2026-11-08 | Claude | Document and number reads now include register visits. |
| 2026-11-04 | Claude | `DocumentViewAccessQuery` grants movement-based reading to the sending department of a returned or rejected current step, in line with the inbox and `DocumentAccessEvaluator`. |
| 2026-11-04 | Claude | Registry filtering by dynamic field values (plan `plans/document-registry-field-filters.md`): `DocumentRepository.ListPagedAsync` takes `DocumentFieldFilter`s and adds one `EXISTS` over `DocumentFieldValues` per filter after the visibility predicate (LIKE escaping for text, calendar-day comparison for dates). |
| 2026-11-03 | Claude | Added `NotificationRepository` and `NotificationRecipientDirectory` for Module 10. |
| 2026-11-03 | Claude | Module 08 Relations, Phase 1: added `DocumentRelationRepository`, registered in `DependencyInjection`. |
| 2026-11-03 | Claude | Added `DocumentRepository.ListViewableIdsAsync` on the shared `VIEW` rule, for the movement queues and the department register. |
| 2026-11-03 | Claude | Moved the SQL form of the `VIEW` rule out of `DocumentArchiveRepository` into `DocumentViewAccessQuery`, added the movement-participation path the archive search could leave out, and applied it to `DocumentRepository.ListPagedAsync`, which previously returned every document to any `DOCUMENT_VIEW` holder. |
| 2026-11-03 | Claude | Module 07 Archiving, Phase 1: added `DocumentArchiveRepository` (events and access-filtered search) and `SavedSearchRepository`, registered in `DependencyInjection`. Also corrected the status count, which had missed the two dynamic-field repositories. |
| 2026-11-02 | Claude | `DocumentTypeSchemaVersionRepository.Remove` added for discarding a schema draft (dynamic fields, Phase 2). |
| 2026-11-02 | Claude | Dynamic document fields, Phase 1: added `DocumentFieldRepository` and `DocumentTypeSchemaVersionRepository`; `DocumentRepository.GetByIdAsync` now also loads `FieldValues` with a split query so a draft's values can be replaced. Registered in `DependencyInjection`. |
| 2026-10-28 | agent | `EmployeeRepository.ListPagedAsync` gained an optional `departmentIds` set; an empty set returns an empty page, a null set keeps the old behavior, so `/api/employees?departmentId=…` can list one unit's roster. |

| 2026-10-25 | opencode | Added `AuditLogRepository` (latest/range reads, filtered paging, checkpoints and integrity reports; append-only) for Module 09. |
| 2026-10-25 | opencode | Added `DocumentMovementRepository.GetActiveByDocumentIdAsync` for the access evaluator's movement participation, and documented that the queues follow the current step after a rejection routed to the Central Diwan. |
| 2026-10-25 | opencode | Added `DocumentFileRepository` (file + versions by id and per document), `DocumentFileTypeRepository`, and `FileStorageProviderRepository` (by id / active default) for the Module 06 Attachments slice. |
| 2026-10-22 | opencode | Added `ListPagedAsync` to `DocumentRepository`, `UserRepository`, `EmployeeRepository`, and `ExternalOrganizationRepository` so the four unbounded lists filter, sort, count, and page in SQL. |
| 2026-10-21 | opencode | Added the movement queue queries to `DocumentMovementRepository`: `ListByDocumentAsync` (a document's routing history), `ListInboxAsync`, and `ListOutboxAsync`. The queue filters run against the movement's **current step** — a correlated `Steps.Any(step => step.StepNo == movement.CurrentStepNo && …)` — so a redirect hands the document to the new destination's inbox and to the redirecting department's outbox. |
| 2026-10-20 | Buffy | Made the numbering queries scope-aware (`GetActiveByDocumentIdAsync` and `AnyByRegistryAsync` now only see central numbers), added `ExistsInScopeAsync`/`GetDepartmentEntryAsync`/`ListByDepartmentAsync`, and added `DocumentRepository.GetWithNumbersAsync`/`GetByIdsAsync` for department registration and the register book. |
| 2026-10-19 | Buffy | Added `InstitutionSettingRepository` (`GetSingletonAsync`/`Add`), `ExternalOrganizationRepository` (by id / normalized code / listing), and `DocumentMovementRepository` (`HasActiveMovementsAsync` = status not CLOSED/CANCELED); all registered in `Infrastructure/DependencyInjection.cs`. |
| 2026-10-19 | opencode | Added `LookupRepository` — one generic implementation covering all 33 seeded reference catalogs through a deterministic slug registry (`LookupCatalog`), with Creates/Relabel/(De)activate plus an `IsInUseAsync` guard derived from the EF model's foreign keys. Registered in `Infrastructure/DependencyInjection.cs`; also recorded the previously undocumented `EmployeeRepository` and `DocumentAccessGrantRepository`. |
| 2026-10-18 | opencode | Added `PermissionRepository` (`GetByCodeAsync`, `ListAllAsync`) and `RoleRepository.ListAllAsync`, registered in `Infrastructure/DependencyInjection.cs` to back the role-management endpoints. |
| 2026-10-18 | opencode | Added `DepartmentRepository` (listing, `HasActiveChildrenAsync`, `GetDescendantIdsAsync`) and `DepartmentTypeRepository` (`GetByCodeAsync`); both registered in `Infrastructure/DependencyInjection.cs` to back the department CRUD + tree endpoints. |
| 2026-10-18 | opencode | Added `DocumentNumberRepository` (incl. `ExistsByNumberAndYearAsync` duplicate-number check) and `NumberingRegistryRepository` (scope matching with most-specific-first ordering); both registered in `Infrastructure/DependencyInjection.cs` to support official-number assignment at document creation. |
