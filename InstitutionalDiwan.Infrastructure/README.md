# InstitutionalDiwan.Infrastructure — Infrastructure & Persistence Layer

> **Status:** Partially implemented — `Persistence/` (`DiwanDbContext` + 10 EF configurations + 14
> of 17 repositories + 5 migrations + ordered seeders), `Authentication/` (PBKDF2 hasher, JWT
> provider, `UserContext`), and `Authorization/` (department scope + document access evaluators) are
> live; outbox, background jobs, and the clock are still to come.

## Purpose

Implements all external concerns defined by interfaces in the Domain and Application layers:
database persistence (EF Core SQL Server), authentication, authorization, outbox processing,
background jobs, and the clock.

All code uses the `InstitutionalDiwan.*` namespaces — the legacy `NewDiwan.*` prefix and the old
`NewDiwanDbContext` name are gone; the context is `InstitutionalDiwan.Infrastructure.Persistence.DiwanDbContext`.

## Subfolders

| Folder | Responsibility | Code status |
| --- | --- | --- |
| [`Persistence/`](Persistence/README.md) | `DiwanDbContext`, `Configurations/`, `Repositories/`, `Interceptors/`, `Migrations/`, `Seed/` | ⚠️ `DbContext` + 10 configurations + 14 repositories + 5 migrations + ordered seeders |
| [`Authentication/`](Authentication/README.md) | JWT token generation, password hashing, HTTP `UserContext` | ✅ `PasswordHasher` + `JwtProvider` + `UserContext` implemented |
| [`Authorization/`](Authorization/README.md) | Permission-based RBAC + organizational scope and document-access evaluators | ✅ `DepartmentScopeEvaluator` + `DocumentAccessEvaluator` implemented |
| [`Storage/`](Storage/README.md) | Local/S3/MinIO binary file storage and SHA-256 stream verification | ❌ Empty |
| [`Outbox/`](Outbox/README.md) | Transactional outbox processor and domain-event dispatcher | ❌ Empty |
| [`BackgroundJobs/`](BackgroundJobs/README.md) | Scheduled integrity checks, audit checkpoints, SLA escalations | ❌ Empty |
| [`Time/`](Time/README.md) | System clock implementation | ❌ Empty |

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
  NumberingConfigurations.cs      DOCUMENT_NUMBERS, NUMBERING_REGISTRIES, NUMBERING_COUNTERS,
                                  DOCUMENT_NUMBER_HISTORY, DOCUMENT_NUMBER_STATUSES,
                                  DOCUMENT_NUMBER_EVENT_TYPES, NUMBERING_RESET_POLICIES
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
| Entity configurations | ✅ Written for all 66 entities (10 configuration files) |
| Repositories | 🟡 14 of 17 implemented — `UserRepository`, `RoleRepository`, `PermissionRepository`, `DepartmentRepository`, `DepartmentTypeRepository`, `DocumentRepository`, `DocumentTypeRepository`, `NumberingRegistryRepository`, `DocumentNumberRepository`, `EmployeeRepository`, `DocumentAccessGrantRepository`, `LookupRepository`, `InstitutionSettingRepository`, `ExternalOrganizationRepository`, `DocumentMovementRepository`; the rest pending |
| Seed | ✅ Ordered, environment-aware seeders (33 lookup seeders, `InstitutionSettingSeeder`, `PermissionSeeder`, `RoleSeeder`, `UserSeeder`) |
| Authentication | ✅ `PasswordHasher` (PBKDF2) + `JwtProvider` + `UserContext` (reads `sub`/`dept_id`/`scope_type`/`permission` claims) |
| Migrations | ✅ 5 applied (`InitialCreate`, `AddUniqueOfficialNumber`, `Refactor_DepartmentTypes_ToLookup`, `Refactor_CorrespondenceNumbering_ToLookups`, `Refactor_AllRemaining_ToLookups`) |
| Interceptors | ❌ Empty |
| Authorization | ✅ `DepartmentScopeEvaluator` + `DocumentAccessEvaluator` implementing the Application contracts |
| Storage / Outbox / BackgroundJobs / Time | ❌ Empty |

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-10-19 | Buffy | Added `Authentication/UserContext`, `Authorization/DepartmentScopeEvaluator`, `Authorization/DocumentAccessEvaluator`, the `InstitutionSettingRepository`/`ExternalOrganizationRepository`/`DocumentMovementRepository` implementations, and `Seed/InstitutionSettingSeeder`; `DependencyInjection` now registers `AddHttpContextAccessor` + the evaluators, and the csproj references the ASP.NET Core shared framework for `IHttpContextAccessor`. |
| 2026-10-19 | opencode | Added `Persistence/Repositories/LookupRepository.cs` (generic slug-addressed access to all 33 reference catalogs, incl. metadata-driven reference-in-use checks) and registered it in `DependencyInjection.cs`; documented the actual `DiwanDbContext`/66-entity/12-of-17-repository state, removing the stale `NewDiwan.*` drift notes. |
| 2026-10-18 | opencode | Registered `IDocumentNumberRepository` + `INumberingRegistryRepository` in `DependencyInjection.cs`; added unique official-number migration — supports number+year conflict check at document creation. |
