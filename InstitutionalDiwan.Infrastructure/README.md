# InstitutionalDiwan.Infrastructure — Infrastructure & Persistence Layer

> **Status:** Partially implemented — `Persistence/` (DbContext + EF configurations + two repositories + seeder) and parts of `Authentication/` (PBKDF2 hasher, JWT provider).

## Purpose

Implements all external concerns defined by interfaces in the Domain and Application layers:
database persistence (EF Core SQL Server), file storage, authentication, authorization, outbox
processing, background jobs, and the clock.

The Domain namespaces are `InstitutionalDiwan.Domain.*`, but the code in this project still uses a
legacy `NewDiwan.*` prefix (`NewDiwan.Infrastructure.Persistence.*`, and `using NewDiwan.Domain.Entities.*`).
Those references do not resolve today — see [Known issues](#known-issues--todo).

## Subfolders

| Folder | Responsibility | Code status |
| --- | --- | --- |
| [`Persistence/`](Persistence/README.md) | `NewDiwanDbContext`, `Configurations/`, `Repositories/`, `Interceptors/`, `Migrations/`, `Seed/` | ⚠️ `DbContext` + 10 configurations + `User`/`Role` repositories + ordered seeders |
| [`Authentication/`](Authentication/README.md) | JWT token generation, password hashing, HTTP `UserContext` | ⚠️ `PasswordHasher` + `JwtProvider` implemented |
| [`Authorization/`](Authorization/README.md) | Permission-based RBAC + organizational scope and document-access evaluators | ❌ Empty |
| [`Storage/`](Storage/README.md) | Local/S3/MinIO binary file storage and SHA-256 stream verification | ❌ Empty |
| [`Outbox/`](Outbox/README.md) | Transactional outbox processor and domain-event dispatcher | ❌ Empty |
| [`BackgroundJobs/`](BackgroundJobs/README.md) | Scheduled integrity checks, audit checkpoints, SLA escalations | ❌ Empty |
| [`Time/`](Time/README.md) | System clock implementation | ❌ Empty |
| `Class1.cs` | Template placeholder | 🗑️ To be removed |

## Persistence at a glance

`NewDiwanDbContext : DbContext, IUnitOfWork` exposes **40 `DbSet`s** grouped by module and applies all
`IEntityTypeConfiguration<T>` classes via `ApplyConfigurationsFromAssembly`. Each aggregate's `Id` alias
and `DomainEvents` are ignored, tables use `SCREAMING_SNAKE_CASE` names, and timestamps use
`datetime2(3)`. `RowVersion` optimistic-concurrency columns are configured on `DOCUMENTS`,
`NUMBERING_COUNTERS`, and `DOCUMENT_MOVEMENTS`.

The configuration files that currently exist:

```
Persistence/Configurations/
  OrganizationConfigurations.cs   DEPARTMENTS, DEPARTMENT_TYPES, INSTITUTION_SETTINGS
  IdentityConfigurations.cs       USERS, ROLES, PERMISSIONS, USER_ROLES, ROLE_PERMISSIONS,
                                  EMPLOYEES, USER_DEPARTMENT_ASSIGNMENTS, DOCUMENT_ACCESS_GRANTS
  CorrespondenceConfigurations.cs DOCUMENTS, DOCUMENT_TYPES, DOCUMENT_STATUS_TYPES,
                                  DOCUMENT_PARTICIPANTS, DOCUMENT_STATUS_HISTORY, EXTERNAL_ORGANIZATIONS
  NumberingConfigurations.cs      DOCUMENT_NUMBERS, NUMBERING_REGISTRIES, NUMBERING_COUNTERS,
                                  DOCUMENT_NUMBER_HISTORY
  MovementsConfigurations.cs      DOCUMENT_MOVEMENTS, DOCUMENT_MOVEMENT_STEPS, DOCUMENT_MOVEMENT_EVENTS
  AttachmentsConfigurations.cs    DOCUMENT_FILES, DOCUMENT_FILE_VERSIONS, DOCUMENT_FILE_TYPES,
                                  FILE_STORAGE_PROVIDERS
  ArchivingConfigurations.cs      DOCUMENT_ARCHIVE_EVENTS, SAVED_SEARCHES
  RelationsConfigurations.cs      DOCUMENT_RELATIONS, DOCUMENT_RELATION_TYPES, DOCUMENT_RELATION_EVENTS
  AuditConfigurations.cs          AUDIT_LOGS (identity + hash chain), AUDIT_CHECKPOINTS,
                                  AUDIT_INTEGRITY_CHECKS
  NotificationsConfigurations.cs  NOTIFICATIONS, NOTIFICATION_RECIPIENTS, NOTIFICATION_OUTBOX,
                                  NOTIFICATION_TYPES
```

## Implementation status

| Area | State |
| --- | --- |
| EF Core provider package (`SqlServer`) | ✅ Referenced |
| `NewDiwanDbContext` | ✅ Written and compiling |
| Entity configurations | ✅ Written for all 40 entities |
| Repositories | ⚠️ `UserRepository`, `RoleRepository` implemented; the rest pending |
| Seed | ✅ Ordered, environment-aware seeders (`PermissionSeeder`, `RoleSeeder`, `UserSeeder`) |
| Authentication | ⚠️ `PasswordHasher` (PBKDF2) + `JwtProvider` implemented; `UserContext` pending |
| Interceptors / Migrations | ❌ Empty |
| Authorization / Storage / Outbox / BackgroundJobs / Time | ❌ Empty |

### Known issues / TODO

1. **Naming drift.** The `DbContext` class is still named `NewDiwanDbContext` even though it now lives
   in `InstitutionalDiwan.Infrastructure.Persistence`; rename it for consistency.
2. **The remaining repositories, interceptors, migrations and jobs** are still to be implemented — see
   each folder's README.
