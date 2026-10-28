# InstitutionalDiwan.Domain — Pure Domain Layer

> **Status:** Core implemented and compiling. See [Known issues](#known-issues--todo) for remaining TODOs.

## Purpose

The Domain layer is the heart of the InstitutionalDiwan system. It encapsulates all business
invariants across the 10 modules using Domain-Driven Design (DDD) and the Result pattern, with
**zero external NuGet package dependencies** (no EF Core, no DataAnnotations schema attributes).
Everything here is plain C# so the domain can be unit-tested without a database or web host.

The project name and namespaces are `InstitutionalDiwan.Domain.*`. Earlier design notes used the
`NewDiwan` name; no `NewDiwan.*` namespace or `NewDiwanDbContext` remains in the code.

## Design rules

| Rule | Detail |
| --- | --- |
| Dependency direction | Domain depends on **nothing**. Application and Infrastructure depend on Domain. |
| No infrastructure types | No `DbContext`, no attributes, no serialization concerns, no `IActionResult`. |
| Mutation through behavior | All state changes happen through intention-revealing methods (`Rename`, `Archive`, `Send`, …), never public setters. |
| Expected failures are values | Business-rule violations return `Result` / `Result<T>`; they do **not** throw. |
| Invariants at construction | Factory methods (`Create`, `Issue`, `Grant`, …) validate and return `Result<T>`; private parameterless constructors exist only for persistence. |
| Domain events | Aggregate roots raise immutable `IDomainEvent` records into a private collection exposed read-only. |
| Deterministic time | Every method that stamps a timestamp takes a trailing optional `DateTime? utcNow` and falls back to `DateTime.UtcNow` only when it is omitted. Application handlers pass `IUnitOfWork.UtcNow`, so one transaction writes one instant and tests can pin it. |

## Folder contents

```
Common/        Base building blocks (Result, Error, Entity, AggregateRoot, ValueObject, IDomainEvent, DomainException, IUnitOfWork)
ValueObjects/  Immutable value objects (DepartmentCode, DocumentTitle, OfficialNumberValue, ParticipantTarget)
Errors/        Strongly-typed DomainErrors catalog organized by module
Events/        All immutable IDomainEvent records raised by aggregate roots
Repositories/  Pure repository contracts consumed by the Application layer
Entities/      All encapsulated domain entities grouped across the 10 module folders
GlobalUsing.cs Global usings for entities, common, and errors
```

### `Common/` — building blocks

| Type | Responsibility |
| --- | --- |
| `Result` / `Result<TValue>` | Outcome of an operation. `Result` guards that success carries `Error.None` and failure carries a real error (throws `InvalidOperationException` otherwise). `Result<T>` exposes `Value` only on success. `Result.Create<T>` maps `null` to `Error.NullValue`. |
| `Error` | Immutable `record(string Code, string Description, ErrorType Type = Failure)` with factory helpers `Validation`, `NotFound`, `Conflict`, `Failure`, `Unauthorized`, `Forbidden` and the sentinels `Error.None`, `Error.NullValue`. |
| `ErrorType` | Enum: `None, Failure, Validation, NotFound, Conflict, Unauthorized, Forbidden`. Used by the API layer to map to HTTP status codes. |
| `Entity<TId>` | Identity-based equality (type + `Id`), `==`/`!=` operators, `GetHashCode` from type + id. |
| `AggregateRoot<TId>` | Adds the domain-event collection: `DomainEvents`, `RaiseDomainEvent` (protected), `ClearDomainEvents`. A non-generic `AggregateRoot` variant also exists for roots without a typed id. |
| `ValueObject` | Structural equality via `GetEqualityComponents()`; combines components with `HashCode` for hashing. |
| `IDomainEvent` | `Guid EventId`, `DateTime OccurredOnUtc`. `DomainEventBase` is an abstract record supplying both with defaults (`Guid.NewGuid()`, `DateTime.UtcNow`). |
| `DomainException` | Sealed exception carrying an `Error.Code`; used for truly exceptional technical violations, not expected business failures. |
| `IUnitOfWork` | `Task<int> SaveChangesAsync(CancellationToken)` — the single abstraction over the persistence transaction. |
| `PageRequest` | Normalized paging/search/sort input (`Page`, `PageSize`, `Search`, `SortBy`, `SortDescending`) with `NormalizedPage`, `NormalizedPageSize` (1–100), `Skip`, and `Take`. |
| `PagedList<T>` | `PagedList<T>(IReadOnlyList<T> Items, int TotalCount)` — one page plus the total row count, returned by repository `ListPagedAsync` methods. |

### `ValueObjects/`

| Value object | Rule enforced |
| --- | --- |
| `DepartmentCode` | Non-empty, trimmed, upper-cased, max 50 chars. |
| `DocumentTitle` | Non-empty, trimmed, max 500 chars. |
| `OfficialNumberValue` | Non-empty, trimmed, max 100 chars. |
| `ParticipantTarget` | Exactly one of `External(orgId)`, `Internal(departmentId)`, or `Person(name)` via `ParticipantTargetKind`. |

### `Errors/DomainErrors.cs`

A single static catalog nested by module (`DomainErrors.Department`, `.Identity`, `.Correspondence`,
`.Numbering`, `.Movements`, `.Attachments`, `.Relations`, `.Archiving`, `.Audit`, `.Notifications`,
`.InstitutionSetting`, `.Lookup`). Each entry is a strongly-typed `Error` with a stable code such as
`Correspondence.OnlyDraftEditable` so the API can produce deterministic ProblemDetails.

The `.Lookup` group drives the generic lookups API: `InvalidId` / `LabelEmpty` (validation),
`UnknownCatalog` and `EntryNotFound` (NotFound), plus `IdAlreadyExists`, `EntryInUse` (Conflict)
and `CatalogReadOnly` (validation) — the last one marks catalogs that need more than a label before
an entry can be created.

The `.Movements` group carries the routing errors: `MovementNotFound` (NotFound),
`SameSourceAndDestination` / `ReasonRequired` (Validation), `InvalidStatusTransition` /
`ActiveMovementExists` / `CentralDiwanRoutingRequired` / `CentralDiwanNotConfigured` /
`StepNotCurrent` (Conflict), and `DepartmentOutOfScope` (Forbidden) — the last one answers a request
to read a department inbox/outbox that sits outside the caller's organizational scope. The Central
Diwan errors enforce the mandatory hub: ordinary → ordinary hops must go through the configured
Central Diwan, and the setting must be present. `StepNotCurrent` backs the planned step-scoped route
aliases. `Department.Inactive` and `Correspondence.OnlyRegisteredRoutable` complete the picture: an
inactive destination department cannot receive a document, and only a `REGISTERED` document can be
routed.

The `.Attachments` group covers Module 06: `FileNotFound` / `VersionNotFound` / `FileTypeNotFound`
and `StorageProviderNotFound` (NotFound), `FileTypeInactive` / `StorageProviderInactive` /
`FileArchived` / `FileAlreadyArchived` (Conflict), and the upload-validation errors `FileEmpty`,
`FileTooLarge`, `ExtensionNotAllowed`, and `SignatureMismatch` (Validation).

The `.Audit` group covers Module 09: `NoAuditLogs` / `PreviousLogNotFound` (NotFound), `HashMismatch` /
`ChainBroken` / `NothingToCheckpoint` (Conflict), and `RequiredFieldsMissing` /
`InvalidCheckpointRange` / `InvalidIntegrityRange` (Validation) so the checkpoint and integrity
handlers can report an empty chain, a broken link, a tampered row, or an unusable range
deterministically.

### `Events/DomainEvents.cs`

36 immutable `record` domain events (the older notes claimed 22, which is out of date). Grouped by module:

| Module | Events |
| --- | --- |
| Organization (3) | `DepartmentCreated`, `DepartmentMoved`, `DepartmentDeactivated` |
| Identity (8) | `UserCreated`, `UserLoggedIn`, `UserDeactivated`, `UserPasswordReset`, `UserRoleAssigned`, `UserRoleRevoked`, `DocumentAccessGranted`, `DocumentAccessRevoked` |
| Correspondence / Archiving (6) | `DocumentCreated`, `DocumentRegistered`, `DocumentArchived`, `DocumentUnarchived`, `DocumentCanceled`, `DocumentParticipantAdded` |
| Numbering (3) | `OfficialNumberIssued`, `OfficialNumberCancelled`, `OfficialNumberReplaced` |
| Movements (9) | `MovementCreated`, `MovementSent`, `MovementReceived`, `MovementCompleted`, `MovementRejected`, `MovementReturned`, `MovementRedirected`, `MovementClosed`, `MovementCancelled` |
| Attachments (3) | `DocumentFileCreated`, `DocumentFileVersionUploaded`, `DocumentFileArchived` |
| Relations (2) | `DocumentRelationCreated`, `DocumentRelationRemoved` |
| Notifications (2) | `NotificationCreated`, `NotificationRead` |

### `Repositories/IDomainRepositories.cs`

21 pure repository contracts: `IDepartmentRepository`, `IDepartmentTypeRepository`, `IUserRepository`,
`IRoleRepository`, `IEmployeeRepository`, `IDocumentAccessGrantRepository`, `IPermissionRepository`,
`IDocumentRepository`, `IDocumentTypeRepository`, `INumberingRegistryRepository`,
`IDocumentNumberRepository`, `IDocumentMovementRepository`, `IDocumentFileRepository`,
`IDocumentFileTypeRepository`, `IFileStorageProviderRepository`, `IDocumentRelationRepository`,
`IAuditLogRepository`, `INotificationRepository`, and `ILookupRepository`. They expose `GetByIdAsync`, a few targeted lookups (`HasActiveChildrenAsync`,
`GetActiveForScopeAsync`, `HasActiveMovementsAsync`, `GetLatestAsync` for audit hash chaining), and
`Add`/`Remove` — no `SaveChanges` (that is `IUnitOfWork`).

`ILookupRepository` is the one generic contract: it addresses any seeded reference catalog by its
kebab-case slug (`IsSupported`, `ListAsync`, `GetByIdAsync`, `AddAsync`, `RelabelAsync`,
`SetActiveStateAsync`, `IsInUseAsync`) so the lookups API needs no per-table repository.

`IDocumentMovementRepository` carries the routing reads: `GetByIdAsync` (with steps and events),
`HasActiveMovementsAsync`, `GetActiveByDocumentIdAsync` (the document currently held by a
non-terminal movement, used for movement participation), `ListByDocumentAsync` (a document's routing
history), and `ListInboxAsync`/`ListOutboxAsync` (a department's two queues).

`IDocumentFileRepository` loads a file with its immutable `Versions` chain (by id) and lists a
document's files; `IDocumentFileTypeRepository` supplies the allowed extensions/size limit for
validation, and `IFileStorageProviderRepository` resolves the active default (or a specific) storage
provider for Module 06.

`IAuditLogRepository` is append-only: `GetLatestAsync` provides the previous `EventHash`,
`GetByIdAsync` anchors a verification range, `ListRangeAsync` walks an ordered range for checkpoints
and integrity checks, and `SearchAsync` applies the audit filters and paging. Checkpoints and
integrity reports have their own add/list/latest methods; no update or delete is exposed.

### `Entities/` — the 10 modules (41 entities)

| # | Module | Entities | Notable aggregate roots |
| --- | --- | --- | --- |
| 01 | Organization | `Department`, `DepartmentType`, `InstitutionSetting` | `Department`, `InstitutionSetting` |
| 02 | Identity & Access | `User`, `Role`, `Permission`, `UserRole`, `RolePermission`, `Employee`, `UserDepartmentAssignment`, `DocumentAccessGrant` | `User`, `Role`, `Employee`, `DocumentAccessGrant` |
| 03 | Correspondence | `Document`, `DocumentParticipant`, `DocumentStatusHistory`, `DocumentType`, `DocumentStatusType`, `ExternalOrganization` | `Document`, `ExternalOrganization` |
| 04 | Official Numbering | `DocumentNumber`, `NumberingRegistry`, `NumberingCounter`, `DocumentNumberHistory`, `DocumentNumberScope` | `DocumentNumber`, `NumberingRegistry` |
| 05 | Movements & Routing | `DocumentMovement`, `DocumentMovementStep`, `DocumentMovementEvent` | `DocumentMovement` |
| 06 | Attachments | `DocumentFile`, `DocumentFileVersion`, `DocumentFileType`, `FileStorageProvider` | `DocumentFile` |
| 07 | Archiving | `DocumentArchiveEvent`, `SavedSearch` | `SavedSearch` |
| 08 | Relations | `DocumentRelation`, `DocumentRelationType`, `DocumentRelationEvent` | `DocumentRelation` |
| 09 | Audit & Integrity | `AuditLog`, `AuditCheckpoint`, `AuditIntegrityCheck` | (`AuditLog` is an `Entity<long>`) |
| 10 | Notifications & Outbox | `Notification`, `NotificationRecipient`, `NotificationOutbox`, `NotificationType` | `Notification` |

#### Selected invariants already encoded

- **`Department`** — code/name/type required; no self-parent; no cyclic re-parenting
  (`MoveToParent` receives descendant ids); inactive parent rejected; cannot deactivate with active children.
- **`Document`** — status machine `DRAFT → REGISTERED → IN_MOVEMENT → {ARCHIVED, CANCELED}`;
  only `DRAFT` is editable/registerable; archived cannot be canceled without unarchiving; cancel requires a reason;
  at most one `ExternalOrganization` participant; archive/cancel blocked while active movements exist;
  `MarkInMovement`/`MarkMovementEnded` move the document between `REGISTERED` and `IN_MOVEMENT` as its
  movement opens and closes; `RowVersion` exposed for optimistic concurrency.
- **`User`** — username required and lower-cased; duplicate role assignment rejected; role revoke when absent returns NotFound;
  department scope assignment validates the date range; password change clears `MustChangePassword`;
  an administrator's `ResetPassword` replaces the hash and sets `MustChangePassword`, recording who reset it;
  `UpdateProfile` edits the primary department, employee link, scope, and password-change flag; `Activate`
  reverses `Deactivate`.
- **`Role`** — code/name required; duplicate permission rejected; system roles are protected from deactivation.
- **`DocumentAccessGrant`** — access level must be one of `VIEW|DOWNLOAD|FORWARD|MANAGE`; expiry must be in the future;
  `IsCurrentlyValid(utcNow)` combines revoked + expiry.
- **`DocumentMovement`** — explicit movement status machine; send/receive/complete/reject/return/redirect/close/cancel
  all guard legal transitions (`InvalidStatusTransition`), require reasons where applicable, append an immutable
  `DocumentMovementEvent`, and raise the matching domain event. Redirect creates the next numbered step.
  A rejection may append a new sent step back to the configured Central Diwan, leaving the rejected step
  untouched as history; the movement becomes active again while the hub holds it.
- **`DocumentNumber` / `NumberingRegistry`** — year in `[1900,2200]`, positive sequence, gap-free allocation via
  `NumberingRegistry.AllocateNextSequence`; cancel/replace require reasons; registry must be active.
  A number is either **central** (`ScopeId = 1`, formatted by the registry, sequence allocated) or a
  **department register entry** (`ScopeId = 2`, clerk-typed text + `RegistryDate`, sequence `0`). The
  same department registering the document again does not create a second number: `ReRegisterInDepartment`
  keeps the text and appends a new dated history row (`EffectiveDate`). `Document.AddDepartmentNumber`
  enforces one entry per department and `Document.FindDepartmentNumber` is the lookup used by the
  receipt gate; `ReplaceWith` refuses department entries.
- **`DocumentMovement`** receipt — `ReceiveStep(actor, departmentRegistryNumberId)` rejects an empty
  number id (`Numbering.DepartmentRegistrationRequired`) and stores it on the step
  (`DocumentMovementStep.RegistryNumberId`), so custody always points at the register line that dated it.
- **`DocumentMovement`** queues — the movement header's `DestinationDepartmentId` always mirrors the
  current step's `ToDepartmentId`, but the movement's `SourceDepartmentId` stays at the origin, so
  the "who holds the document now" question is answered from the **current step** (`CurrentStepNo`),
  not from the header. `ListInboxAsync`/`ListOutboxAsync` in the Infrastructure layer rely on that.
- **`DocumentFile`** — version numbers strictly sequential; archived files reject new versions; superseded versions flagged.
- **`AuditLog`** — tamper-evident SHA-256 hash chaining: `Record(...)` normalizes `OccurredAtUtc` to
  UTC milliseconds, hashes every stored field except `AuditLogId`/`EventHash` (including actor
  department, related document, IP, and correlation id), and links it to `PreviousHash`;
  `VerifyIntegrity(expectedPreviousHash)` re-computes and detects a broken chain or hash mismatch,
  and `Reseal(previousHash)` re-chains a not-yet-persisted row onto the current predecessor after the
  persistence layer takes the chain lock. The first row chains to `AuditLog.GenesisHash` (64 zeroes).
  Checkpoints and integrity reports are separate immutable records.
- **`Notification` / `NotificationOutbox`** — duplicate recipients rejected; outbox transitions
  `PENDING → SENT` or `→ FAILED/DEAD_LETTER` with `RetryCount` and `NextRetryAt`.
- **`DocumentRelation`** — no self-relations; inactive relation types rejected; optional cycle prevention and
  symmetric endpoint normalization handled by `DocumentRelationType`.

## Reference (lookup) tables

Free-form "code" strings (`departmentTypeCode`, `documentStatusTypeCode`, `direction`, …) have been
replaced with dedicated **reference/lookup tables**. Each lookup is its own entity derived from
`LookupEntity` and referenced by an integer FK from the table that used to hold the string.

### Pattern

`Domain/Common/LookupEntity.cs` supplies the shared shape:

| Member | Purpose |
| --- | --- |
| `Id` (`int`) | Stable primary key. |
| `Desc` / `DescAr` | English / Arabic labels (both required). |
| `IsActive` | Soft on/off switch instead of deleting rows. |
| `CreatedAt` | Audit timestamp. |
| `Relabel(desc, descAr)` / `Deactivate()` / `Activate()` | Encapsulated mutations. |

Conventions applied to every lookup:

- **Deterministic integer ids** are declared as `public const int ...Id` on the entity (for example
  `DepartmentType.SectionId`, `DocumentStatusType.RegisteredId`). They are persisted with
  `ValueGeneratedNever()` rather than a database `IDENTITY`, so default-data seeding and the domain
  status machines can reference them safely. *This is the one deviation from the original plan,
  which proposed a database identity column — revisit if auto-generated ids are required.*
- Domain-specific columns that already existed are kept as extra properties (for example
  `DepartmentType.HierarchyLevelOrder`, `DocumentType.AllowedDirectionId`,
  `DocumentStatusType.IsTerminal`, `NotificationType.TitleTemplateAr`).
- Default rows are inserted by seeders deriving from `Seed/LookupSeederBase<TEntity>` (idempotent:
  existing rows are never overwritten), registered in `Infrastructure/DependencyInjection`.
- Business documents store only the integer id; clients resolve labels from the lookup endpoints.

### Converted

| Lookup table | Replaced string | Referenced by |
| --- | --- | --- |
| `DEPARTMENT_TYPES` | `Department.DepartmentTypeCode` | `DEPARTMENTS.DepartmentTypeId` |
| `NUMBERING_MODES` | `InstitutionSetting.NumberingMode` | `INSTITUTION_SETTINGS.NumberingModeId` |
| `USER_SCOPE_TYPES` | `User.ScopeType` | `USERS.ScopeTypeId` |
| `ASSIGNMENT_TYPES` | `UserDepartmentAssignment.AssignmentType` | `USER_DEPARTMENT_ASSIGNMENTS.AssignmentTypeId` |
| `ACCESS_LEVELS` | `DocumentAccessGrant.AccessLevel` | `DOCUMENT_ACCESS_GRANTS.AccessLevelId` |
| `DOCUMENT_DIRECTIONS` | `Document.Direction`, `NumberingRegistry.Direction` | `DOCUMENTS.DirectionId`, `NUMBERING_REGISTRIES.DirectionId` |
| `DOCUMENT_TYPES` | `Document.DocumentTypeCode`, `NumberingRegistry.DocumentTypeCode` | `DOCUMENTS.DocumentTypeId`, `NUMBERING_REGISTRIES.DocumentTypeId` |
| `DOCUMENT_STATUS_TYPES` | `Document.CurrentStatusCode`, `Document.StatusBeforeArchiveCode`, `DocumentStatusHistory.From/ToStatusCode`, `DocumentArchiveEvent.Previous/NewStatusCode` | `DOCUMENTS.CurrentStatusId`, `DOCUMENT_STATUS_HISTORY.FromStatusId/ToStatusId`, `DOCUMENT_ARCHIVE_EVENTS.PreviousStatusId/NewStatusId` |
| `DOCUMENT_CONFIDENTIALITY_LEVELS` | `Document.ConfidentialityLevel` | `DOCUMENTS.ConfidentialityLevelId` |
| `DOCUMENT_PRIORITY_LEVELS` | `Document.PriorityLevel` | `DOCUMENTS.PriorityLevelId` |
| `DOCUMENT_NUMBER_STATUSES` | `DocumentNumber.Status` | `DOCUMENT_NUMBERS.StatusId` |
| `DOCUMENT_NUMBER_EVENT_TYPES` | `DocumentNumberHistory.EventType` | `DOCUMENT_NUMBER_HISTORY.EventTypeId` |
| `DOCUMENT_NUMBER_SCOPES` | `DocumentNumber.Scope` (central vs department register) | `DOCUMENT_NUMBERS.ScopeId` |
| `NUMBERING_RESET_POLICIES` | `NumberingRegistry.ResetPolicy`, `InstitutionSetting.NumberingResetPolicy` | `NUMBERING_REGISTRIES.ResetPolicyId`, `INSTITUTION_SETTINGS.NumberingResetPolicyId` |
| `PARTICIPANT_ROLES` | `DocumentParticipant.RoleCode` | `DOCUMENT_PARTICIPANTS.RoleId` |
| `PARTICIPANT_TARGET_TYPES` | `DocumentParticipant.TargetType` | `DOCUMENT_PARTICIPANTS.TargetTypeId` |
| `ORGANIZATION_TYPES` | `ExternalOrganization.OrganizationType` | `EXTERNAL_ORGANIZATIONS.OrganizationTypeId` |
| `MOVEMENT_TYPES` | `DocumentMovement.MovementType` | `DOCUMENT_MOVEMENTS.MovementTypeId` |
| `MOVEMENT_STATUSES` | `DocumentMovement.StatusCode` | `DOCUMENT_MOVEMENTS.StatusId` |
| `MOVEMENT_STEP_STATUSES` | `DocumentMovementStep.StepStatus` | `DOCUMENT_MOVEMENT_STEPS.StepStatusId` |
| `MOVEMENT_EVENT_TYPES` | `DocumentMovementEvent.EventType` | `DOCUMENT_MOVEMENT_EVENTS.EventTypeId` |
| `STORAGE_PROVIDER_TYPES` | `FileStorageProvider.ProviderType` | `FILE_STORAGE_PROVIDERS.ProviderTypeId` |
| `DOCUMENT_FILE_TYPES` | `DocumentFile.FileTypeCode` | `DOCUMENT_FILES.FileTypeId` |
| `FILE_STORAGE_PROVIDERS` | `DocumentFileVersion.StorageProviderCode` | `DOCUMENT_FILE_VERSIONS.StorageProviderId` |
| `DOCUMENT_RELATION_TYPES` | `DocumentRelation.RelationTypeCode`, `DocumentRelationEvent.RelationTypeCode` | `DOCUMENT_RELATIONS.RelationTypeId`, `DOCUMENT_RELATION_EVENTS.RelationTypeId` |
| `RELATION_ACTION_TYPES` | `DocumentRelationEvent.ActionType` | `DOCUMENT_RELATION_EVENTS.ActionTypeId` |
| `ARCHIVE_EVENT_TYPES` | `DocumentArchiveEvent.EventType` | `DOCUMENT_ARCHIVE_EVENTS.EventTypeId` |
| `ARCHIVE_LOCATIONS` | `DocumentArchiveEvent.ArchiveLocationCode` | `DOCUMENT_ARCHIVE_EVENTS.ArchiveLocationId` |
| `AUDIT_ACTION_TYPES` | `AuditLog.ActionType` | `AUDIT_LOGS.ActionTypeId` |
| `AUDIT_ENTITY_TYPES` | `AuditLog.EntityType` | `AUDIT_LOGS.EntityTypeId` |
| `NOTIFICATION_TYPES` | `Notification.NotificationTypeCode` | `NOTIFICATIONS.NotificationTypeId` |
| `NOTIFICATION_PRIORITIES` | `Notification.Priority` | `NOTIFICATIONS.PriorityId` |
| `NOTIFICATION_CHANNELS` | `NotificationOutbox.Channel` | `NOTIFICATION_OUTBOX.ChannelId` |
| `NOTIFICATION_OUTBOX_STATUSES` | `NotificationOutbox.Status` | `NOTIFICATION_OUTBOX.StatusId` |

Every reference/"code" string in the domain has now been converted. The only code-like columns that
remain **intentionally** as strings are identity/business identifiers that are not lookups —
`Role.Code`, `Permission.Code`, `Department.Code`, `NumberingRegistry.Code`,
`ExternalOrganization.Code` — plus genuinely free-text multi-value fields such as
`NotificationType.DefaultChannelsCsv` and `AuditLog.ModuleName`.

### Lookup CRUD endpoints (implemented)

A single generic `LookupsController` exposes CRUD over every reference table, with the lookup
selected by a route segment resolved through a registry of allowed slugs (see the Infrastructure
`LookupRepository`):

| Method | Route | Behavior |
| --- | --- | --- |
| `GET` | `/api/lookups/{lookup}` | List entries (optionally `?includeInactive=true`). |
| `GET` | `/api/lookups/{lookup}/{id}` | Fetch one entry. |
| `POST` | `/api/lookups/{lookup}` | Create (`Desc`, `DescAr`, plus any domain extras). |
| `PUT` | `/api/lookups/{lookup}/{id}` | Relabel an existing entry. |
| `PATCH` | `/api/lookups/{lookup}/{id}/deactivate` | Deactivate. |
| `PATCH` | `/api/lookups/{lookup}/{id}/activate` | Reactivate. |

Every route — including reads — requires an authenticated token; writes additionally require the
`LOOKUP_MANAGE` permission (`Permissions.LookupManage`). Ids are immutable, an unknown slug is a 404
(`Lookup.UnknownCatalog`), a duplicate id is a 409 (`Lookup.IdAlreadyExists`), and deactivating a
lookup that is still referenced by a live business row is rejected with a Conflict
(`Lookup.EntryInUse`). `department-types`, `notification-types` and `file-storage-providers` cannot
be created through the generic route (they need extra fields: `HierarchyLevelOrder`, Arabic message
templates, and a storage endpoint respectively) — `POST` answers 400 `Lookup.CatalogReadOnly` for
them, while listing, relabelling and (de)activation keep working.

## Implementation status

| Folder | State |
| --- | --- |
| `Common/` | ✅ Implemented (`Result`, `Error`, `Entity`, `AggregateRoot`, `ValueObject`, `IDomainEvent`, `DomainException`, `IClock`, `IUnitOfWork`, `PageRequest`, `PagedList<T>`) |
| `ValueObjects/` | ✅ Implemented (4 value objects) |
| `Errors/` | ✅ Implemented (`DomainErrors` catalog) |
| `Events/` | ✅ Implemented (35 events) |
| `Repositories/` | ✅ Implemented (21 interfaces) |
| `Entities/` | ✅ Implemented (64 entities across 10 modules, including reference/lookup tables) |

### Known issues / TODO

1. ~~Build failure~~ **Resolved** — the entity usings now point at `InstitutionalDiwan.Domain.Events`
   and the Domain project builds cleanly.
2. ~~Naming drift~~ **Resolved** — code namespaces and the DbContext (`DiwanDbContext`) are
   consistently `InstitutionalDiwan.*`; no `NewDiwan.*` references remain.
3. ~~No test project yet~~ **Resolved** — `InstitutionalDiwan.Tests` covers the domain invariants,
   application handlers, seeding pipeline and authorization.
4. **Identity login errors added** — `DomainErrors.Identity.InvalidCredentials` (Unauthorized) and
   `DomainErrors.Identity.UserInactive` (Forbidden) support the login use case.

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-10-28 | agent | Added `User.ResetPassword` and `UserPasswordResetEvent`: an administrative reset replaces the hash, sets `MustChangePassword`, and carries the resetting administrator (35 → 36 events). |
| 2026-10-26 | Qoder | Added `UserLoggedInEvent` (raised by `User.RecordLogin`, 34 → 35 events) so logins become attributable audit rows, and the action-level audit lookup ids `AuditActionType.ExecuteId = 8` / `AuditEntityType.OperationId = 13`. |
| 2026-10-25 | Buffy | Added `AuditLog.Reseal(previousHash)` so an unpersisted audit row can be re-chained onto the current predecessor after the persistence layer serializes appends. |
| 2026-10-25 | opencode | Strengthened the Module 09 audit domain: `AuditLog.GenesisHash`, UTC/millisecond normalization, a hash payload covering every stored field, the expanded `IAuditLogRepository` (range/checkpoint/integrity queries), audit entity-type ids, and the `Audit.*` error catalog. |
| 2026-10-25 | opencode | Added the Central Diwan routing errors (`CentralDiwanRoutingRequired`, `CentralDiwanNotConfigured`, `StepNotCurrent`), `IDocumentMovementRepository.GetActiveByDocumentIdAsync`, and rejection routing back to the Central Diwan as a new sent step. |
| 2026-10-25 | opencode | Added the Module 06 attachment errors (`FileNotFound`, `VersionNotFound`, `FileTypeNotFound`, `StorageProviderNotFound`, `FileTypeInactive`, `StorageProviderInactive`, `FileAlreadyArchived`, `FileEmpty`, `FileTooLarge`, `ExtensionNotAllowed`, `SignatureMismatch`, `TitleTooLong`), `Correspondence.DocumentAccessDenied`, and the `IDocumentFileTypeRepository` / `IFileStorageProviderRepository` contracts plus `IDocumentFileRepository.ListByDocumentAsync`. |
| 2026-10-22 | Buffy | Threaded deterministic time through the whole layer: every aggregate factory/behavior that stamps a timestamp (`Document`, `User`, `Department`, `Employee`, `DocumentMovement` + steps/events, `DocumentNumber`, `NumberingRegistry`/`Counter`, `InstitutionSetting`, `ExternalOrganization`, `DocumentAccessGrant`, `Role`/`Permission`/`RolePermission`, `UserRole`/`UserDepartmentAssignment`, `DocumentFile`, `AuditCheckpoint`/`AuditIntegrityCheck`, `Notification*`, `SavedSearch`, relations, lookups, `DepartmentType`) now accepts an optional `utcNow` and uses it for nested rows too. The system clock remains only as the fallback (`IDomainEvent.OccurredOnUtc`, `AuditLog.Record`). Added `DomainErrors.Identity.NotAuthenticated` and `DomainErrors.Correspondence.DepartmentOutOfScope`. |
| 2026-10-22 | opencode | Added `Common/PageRequest` (normalized page/pageSize/search/sort with clamping) and `Common/PagedList<T>` (one page + total count), and the `ListPagedAsync` contracts on `IDocumentRepository`, `IUserRepository`, `IEmployeeRepository`, and `IExternalOrganizationRepository`. Replaced `IDocumentRepository.ListRecentAsync`. |
| 2026-10-21 | opencode | Added `Document.MarkMovementEnded` (a document leaves `IN_MOVEMENT` when its movement closes or is canceled), the `DomainErrors.Movements.ActiveMovementExists` / `DepartmentOutOfScope`, `DomainErrors.Department.Inactive`, and `DomainErrors.Correspondence.OnlyRegisteredRoutable` errors, and the `IDocumentMovementRepository.ListByDocumentAsync`/`ListInboxAsync`/`ListOutboxAsync` contracts. |
| 2026-10-20 | Buffy | Added department register numbering: `DocumentNumberScope` lookup, `ScopeId`/`DepartmentId`/`RegistryDate` and `CreateDepartmentEntry`/`ReRegisterInDepartment` on `DocumentNumber`, `EffectiveDate` on `DocumentNumberHistory`, the `RegisteredInDepartment`/`ReRegisteredInDepartment` events, `Document.Numbers` with `AddDepartmentNumber`/`FindDepartmentNumber`, `DocumentMovementStep.RegistryNumberId` with a register-gated `ReceiveStep`, the new `DomainErrors.Numbering.*`/`Movements.MovementNotFound` entries, and the repository contracts for scoped uniqueness, department entries, register books, and bulk document loads. |
| 2026-10-19 | Buffy | Added the Module 01–04 behavior the new use cases need: `InstitutionSetting.Update` (Central Diwan required + transfer-reason rule) and `UpdateNumberingPolicy`, `ExternalOrganization.Update`/`Activate`, `NumberingRegistry.Update(hasIssuedNumbers:)` (template lock), `Document.AttachReplacementNumber`, `User.AssignDepartmentScope` duplicate guard + `User.EndDepartmentScope`; plus the `NUMBERING_MANAGE` permission, `IInstitutionSettingRepository`/`IExternalOrganizationRepository` contracts, and the new `DomainErrors` entries (`InstitutionSetting.*`, `Lookup.EntryInactive`, `Identity.DepartmentAlreadyAssigned`/`DepartmentAssignmentNotFound`/`AssignmentTypeInvalid`). |
| 2026-10-19 | opencode | Added the `LOOKUP_MANAGE` permission (`Permissions.LookupManage`, now part of `Permissions.All`), the `DomainErrors.Lookup` API errors (`UnknownCatalog`, `EntryNotFound`, `IdAlreadyExists`, `EntryInUse`, `CatalogReadOnly`), and the generic `ILookupRepository` contract (17th repository interface) — the domain side of the generic `/api/lookups/{lookup}` API; the lookup CRUD section now describes the implemented routes instead of a plan. |
| 2026-10-18 | opencode | `User` gained `UpdateProfile` (primary department, employee link, scope, password-change flag) and `Activate` (reverses `Deactivate`). |
| 2026-10-18 | opencode | Added `IEmployeeRepository` and `IDocumentAccessGrantRepository` plus the `Identity.EmployeeNotFound`/`Identity.AccessGrantNotFound` errors; `Employee` gained `Update` (full-field edit) and `Activate` alongside the existing `TransferDepartment`/`Deactivate`; refreshed the repository count to 16. |
| 2026-10-18 | opencode | Converted the **remaining** reference "code" strings to seeded lookup tables and completed the lookup programme: Identity (`UserScopeType`, `AssignmentType`, `AccessLevel`), Correspondence (`ParticipantRole`, `ParticipantTargetType`, `OrganizationType`), Movements (`MovementType`, `MovementStatus`, `MovementStepStatus`, `MovementEventType`), Attachments (`StorageProviderType`; reshaped `DocumentFileType`, `FileStorageProvider`), Relations (`DocumentRelationType`, `RelationActionType`), Archiving (`ArchiveEventType`, `ArchiveLocation`), Audit (`AuditActionType`, `AuditEntityType`), Notifications (`NotificationType`, `NotificationPriority`, `NotificationChannel`, `NotificationOutboxStatus`), and Organization (`NumberingMode`). Every reference string is now an integer FK; only identity/business codes and multi-value/free-text fields remain strings. |
| 2026-10-18 | opencode | Introduced `LookupEntity` and replaced reference "code" strings with seeded lookup tables: Organization (`DepartmentType`), Correspondence (`DocumentType`, `DocumentStatusType`, `DocumentDirection`, confidentiality/priority levels), and Numbering (`DocumentNumberStatus`, `DocumentNumberEventType`, `NumberingResetPolicy`). Documented the pattern and the planned generic lookup CRUD endpoints. |
| 2026-10-18 | opencode | Added `DomainErrors.Numbering.OfficialNumberTaken` (Conflict) and `IDocumentNumberRepository.ExistsByNumberAndYearAsync` — the domain contract behind the official-number + year duplicate check performed when creating a document. |
