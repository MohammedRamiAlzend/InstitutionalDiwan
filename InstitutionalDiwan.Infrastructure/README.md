# InstitutionalDiwan.Infrastructure — Infrastructure & Persistence Layer

> **Status:** Partially implemented — `Persistence/` (`DiwanDbContext` + 10 EF configuration files +
> 19 of 21 repositories + migrations + ordered seeders), `Authentication/` (PBKDF2 hasher, JWT provider,
> `UserContext`), `Authorization/` (department scope + document access evaluators), `Storage/` (local
> file storage + magic-byte validation), `Persistence/Interceptors/` (tamper-evident audit chain),
> `Time/` (`IDateTimeProvider`), and the two audit `BackgroundJobs/` are live; the transactional outbox
> and notification jobs are still to come.

## Purpose

Implements all external concerns defined by interfaces in the Domain and Application layers:
database persistence (EF Core SQL Server), authentication, authorization, outbox processing,
background jobs, and the clock.

All code uses the `InstitutionalDiwan.*` namespaces — the legacy `NewDiwan.*` prefix and the old
`NewDiwanDbContext` name are gone; the context is `InstitutionalDiwan.Infrastructure.Persistence.DiwanDbContext`.

## Subfolders

| Folder | Responsibility | Code status |
| --- | --- | --- |
| [`Persistence/`](Persistence/README.md) | `DiwanDbContext`, `Configurations/`, `Repositories/`, `Interceptors/`, `Migrations/`, `Seed/` | ⚠️ `DbContext` + 10 configuration files + 19 repositories + 8 migrations + ordered seeders |
| [`Authentication/`](Authentication/README.md) | JWT token generation, password hashing, HTTP `UserContext` | ✅ `PasswordHasher` + `JwtProvider` + `UserContext` implemented |
| [`Context/`](Context/README.md) | Ambient request metadata (`IRequestContext`) | ✅ `HttpRequestContext` implemented |
| [`Authorization/`](Authorization/README.md) | Permission-based RBAC + organizational scope and document-access evaluators | ✅ `DepartmentScopeEvaluator` + `DocumentAccessEvaluator` implemented |
| [`Storage/`](Storage/README.md) | Local binary file storage, streamed SHA-256, and magic-byte validation | ✅ `LocalFileStorageService` + `FileSignatureValidator` (S3/MinIO still planned) |
| [`Outbox/`](Outbox/README.md) | Transactional outbox processor and domain-event dispatcher | ❌ Empty |
| [`BackgroundJobs/`](BackgroundJobs/README.md) | Scheduled integrity checks, audit checkpoints, SLA escalations | 🟡 `AuditChainIntegrityVerificationJob` + `DailyAuditCheckpointJob` implemented; outbox/SLA jobs pending |
| [`Time/`](Time/README.md) | System clock implementation | ✅ `DateTimeProvider` (UTC storage + `Asia/Damascus` conversion) |

## Persistence at a glance

`DiwanDbContext : DbContext, IUnitOfWork` exposes **66 `DbSet`s** grouped by module (including the
reference/lookup tables) and applies all `IEntityTypeConfiguration<T>` classes via
`ApplyConfigurationsFromAssembly`. Each aggregate's `Id` alias and `DomainEvents` are ignored, tables
use `SCREAMING_SNAKE_CASE` names, and timestamps use `datetime2(3)`. `RowVersion`
optimistic-concurrency columns are configured on `DOCUMENTS`, `NUMBERING_COUNTERS`, and
`DOCUMENT_MOVEMENTS`.

The configuration files that currently exist:

```
Persistence/Configurations/
  OrganizationConfigurations.cs   DEPARTMENTS, DEPARTMENT_TYPES, INSTITUTION_SETTINGS, NUMBERING_MODES
  IdentityConfigurations.cs       USERS, ROLES, PERMISSIONS, USER_ROLES, ROLE_PERMISSIONS,
                                  EMPLOYEES, USER_DEPARTMENT_ASSIGNMENTS, DOCUMENT_ACCESS_GRANTS,
                                  USER_SCOPE_TYPES, ASSIGNMENT_TYPES, ACCESS_LEVELS
  CorrespondenceConfigurations.cs DOCUMENTS, DOCUMENT_TYPES, DOCUMENT_STATUS_TYPES, DOCUMENT_DIRECTIONS,
                                  DOCUMENT_CONFIDENTIALITY_LEVELS, DOCUMENT_PRIORITY_LEVELS,
                                  DOCUMENT_PARTICIPANTS, DOCUMENT_STATUS_HISTORY, EXTERNAL_ORGANIZATIONS,
                                  PARTICIPANT_ROLES, PARTICIPANT_TARGET_TYPES, ORGANIZATION_TYPES
  NumberingConfigurations.cs      DOCUMENT_NUMBERS (central + department rows), NUMBERING_REGISTRIES,
                                  NUMBERING_COUNTERS, DOCUMENT_NUMBER_HISTORY, DOCUMENT_NUMBER_STATUSES,
                                  DOCUMENT_NUMBER_EVENT_TYPES, DOCUMENT_NUMBER_SCOPES,
                                  NUMBERING_RESET_POLICIES
  MovementsConfigurations.cs      DOCUMENT_MOVEMENTS, DOCUMENT_MOVEMENT_STEPS, DOCUMENT_MOVEMENT_EVENTS,
                                  MOVEMENT_TYPES, MOVEMENT_STATUSES, MOVEMENT_STEP_STATUSES,
                                  MOVEMENT_EVENT_TYPES
  AttachmentsConfigurations.cs    DOCUMENT_FILES, DOCUMENT_FILE_VERSIONS, DOCUMENT_FILE_TYPES,
                                  FILE_STORAGE_PROVIDERS, STORAGE_PROVIDER_TYPES
  ArchivingConfigurations.cs      DOCUMENT_ARCHIVE_EVENTS, SAVED_SEARCHES, ARCHIVE_EVENT_TYPES,
                                  ARCHIVE_LOCATIONS
  RelationsConfigurations.cs      DOCUMENT_RELATIONS, DOCUMENT_RELATION_TYPES, DOCUMENT_RELATION_EVENTS,
                                  RELATION_ACTION_TYPES
  AuditConfigurations.cs          AUDIT_LOGS (identity + hash chain), AUDIT_CHECKPOINTS,
                                  AUDIT_INTEGRITY_CHECKS, AUDIT_ACTION_TYPES, AUDIT_ENTITY_TYPES
  NotificationsConfigurations.cs  NOTIFICATIONS, NOTIFICATION_RECIPIENTS, NOTIFICATION_OUTBOX,
                                  NOTIFICATION_TYPES, NOTIFICATION_PRIORITIES, NOTIFICATION_CHANNELS,
                                  NOTIFICATION_OUTBOX_STATUSES
```

## Implementation status

| Area | State |
| --- | --- |
| EF Core provider package (`SqlServer`) | ✅ Referenced |
| `DiwanDbContext` | ✅ Written and compiling |
| Entity configurations | ✅ Written for all 67 entities (10 configuration files) |
| Repositories | 🟡 19 of 21 implemented — `UserRepository`, `RoleRepository`, `PermissionRepository`, `DepartmentRepository`, `DepartmentTypeRepository`, `DocumentRepository`, `DocumentTypeRepository`, `NumberingRegistryRepository`, `DocumentNumberRepository`, `EmployeeRepository`, `DocumentAccessGrantRepository`, `LookupRepository`, `InstitutionSettingRepository`, `ExternalOrganizationRepository`, `DocumentMovementRepository`, `DocumentFileRepository`, `DocumentFileTypeRepository`, `FileStorageProviderRepository`, `AuditLogRepository`; relation and notification repositories pending |
| Seed | ✅ Ordered, environment-aware seeders (33 lookup seeders, `InstitutionSettingSeeder`, `DepartmentSeeder` demo tree, `EmployeeSeeder` demo personnel, `PermissionSeeder`, `RoleSeeder`, `UserSeeder`) |
| Authentication | ✅ `PasswordHasher` (PBKDF2) + `JwtProvider` + `UserContext` (reads `sub`/`dept_id`/`scope_type`/`permission` claims) |
| Migrations | ✅ 8 written (`InitialCreate`, `AddUniqueOfficialNumber`, `Refactor_DepartmentTypes_ToLookup`, `Refactor_CorrespondenceNumbering_ToLookups`, `Refactor_AllRemaining_ToLookups`, `InitialCreate2`, `Add_Department_Register_Numbers`, `Add_Audit_Chain_Uniqueness`) |
| Interceptors | ✅ `TamperEvidentAuditInterceptor` (domain events incl. `UserLoggedIn`, generic old/new snapshots, SHA-256 chain, sensitive-field redaction, no writes outside an HTTP request) |
| Authorization | ✅ `DepartmentScopeEvaluator` + `DocumentAccessEvaluator` implementing the Application contracts |
| Storage | ✅ `LocalFileStorageService` + `FileSignatureValidator` |
| BackgroundJobs | 🟡 `AuditChainIntegrityVerificationJob` + `DailyAuditCheckpointJob` (configurable, idempotent) |
| Outbox | ❌ Empty |

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-10-28 | Qoder | Added `Seed/EmployeeSeeder` (Order 97, Development/Staging) seeding 28 demo employees across the department tree, and extended `EmployeeRepository.ListPagedAsync` with an optional department-id set so `/api/employees` can list one department's roster (optionally including its descendants). |
| 2026-10-28 | Qoder | Added `Seed/DepartmentSeeder` (Order 96, Development/Staging) that builds a 15-department demo tree under the seeded Central Diwan root — 4 directorates, 7 departments, 3 sections — inserting level by level because EF cannot infer the insert order of the self-referencing parent FK; registered it in `DependencyInjection` after `InstitutionSettingSeeder`. |
| 2026-10-26 | Qoder | Mapped the new `UserLoggedInEvent` to a `Login` audit row and seeded the `Execute` action type / `Operation` entity type used by the new action-level audit trail; the interceptor continues to own all entity-level rows. |
| 2026-10-25 | Buffy | Serialized audit-chain appends with a transaction-scoped SQL Server application lock and a locking head read, after concurrent audit reads and the two scheduled jobs collided on `UX_AUDIT_LOGS_PREVIOUS_HASH`; explicit job rows now share the same path. |
| 2026-10-25 | opencode | Implemented Module 09 infrastructure: `AuditLogRepository` (paged search, range reads, checkpoints, integrity reports), `TamperEvidentAuditInterceptor` (hash chain, domain-event projection, sensitive-field redaction), `DateTimeProvider`, `HttpRequestContext`, `EfTransactionManager`, the `Add_Audit_Chain_Uniqueness` migration, and the scheduled `AuditChainIntegrityVerificationJob` / `DailyAuditCheckpointJob` (configurable via the `AuditJobs` section, actor resolved from `ActorUserId` or the first active user). |
| 2026-10-25 | opencode | Implemented Module 06 persistence and storage: `DocumentFileRepository` (file + versions), `DocumentFileTypeRepository`, `FileStorageProviderRepository`, `LocalFileStorageService` (streamed SHA-256, safe path resolution), and `FileSignatureValidator`; all registered in `DependencyInjection`. |
| 2026-10-20 | Buffy | Added department register numbering to persistence: `DOCUMENT_NUMBER_SCOPES` (entity, configuration, seeder), `ScopeId`/`DepartmentId`/`RegistryDate` + scoped indexes, `DOCUMENT_NUMBER_HISTORY.EffectiveDate`, `DOCUMENT_MOVEMENT_STEPS.RegistryNumberId`, the `Add_Department_Register_Numbers` migration (applied to a throwaway LocalDB database as verification), and scope-aware number queries. |
| 2026-10-19 | Buffy | Added `Authentication/UserContext`, `Authorization/DepartmentScopeEvaluator`, `Authorization/DocumentAccessEvaluator`, the `InstitutionSettingRepository`/`ExternalOrganizationRepository`/`DocumentMovementRepository` implementations, and `Seed/InstitutionSettingSeeder`; `DependencyInjection` now registers `AddHttpContextAccessor` + the evaluators, and the csproj references the ASP.NET Core shared framework for `IHttpContextAccessor`. |
| 2026-10-19 | opencode | Added `Persistence/Repositories/LookupRepository.cs` (generic slug-addressed access to all 33 reference catalogs, incl. metadata-driven reference-in-use checks) and registered it in `DependencyInjection.cs`; documented the actual `DiwanDbContext`/66-entity/12-of-17-repository state, removing the stale `NewDiwan.*` drift notes. |
| 2026-10-18 | opencode | Registered `IDocumentNumberRepository` + `INumberingRegistryRepository` in `DependencyInjection.cs`; added unique official-number migration — supports number+year conflict check at document creation. |
