# Infrastructure / Persistence

> **Status:** Partially implemented. `DiwanDbContext` and the ten configuration files exist, plus
> five repositories, two applied migrations, and the ordered seeders. Interceptors remain empty.

## Purpose

Implements the EF Core (SQL Server) persistence concern. The `DbContext` doubles as the
`IUnitOfWork` the Application layer depends on, so a command handler can call `SaveChangesAsync`
without knowing which provider is underneath.

## Contents

| Path | State | Notes |
| --- | --- | --- |
| `NewDiwanDbContext.cs` | ✅ Written | `DbContext, IUnitOfWork`; 40 `DbSet`s; applies configurations by assembly scan. |
| `Configurations/` | ✅ Written (10 files) | `IEntityTypeConfiguration<T>` for all 40 entities. |
| `Repositories/` | 🟡 Partial (8 files) | `UserRepository`, `RoleRepository`, `PermissionRepository`, `DepartmentRepository`, `DepartmentTypeRepository`, `DocumentRepository`, `NumberingRegistryRepository`, `DocumentNumberRepository`. |
| `Interceptors/` | ❌ Empty | Domain-event dispatch + tamper-evident audit. |
| `Migrations/` | ✅ Written (2) | `InitialCreate` + `AddUniqueOfficialNumber` (unique official-number + year index). |
| `Seed/` | ✅ Written | `ISeeder` + `SeederRunner` (ordering + environment gating); `PermissionSeeder` (100), `RoleSeeder` (200), `UserSeeder` (300). |

## `NewDiwanDbContext` design

- Implements `IUnitOfWork` (`Task<int> SaveChangesAsync(CancellationToken)`).
- Exposes `DbSet`s grouped by module with comments (Organization, Identity, Correspondence, Numbering,
  Movements, Attachments, Archiving, Relations, Audit, Notifications).
- `OnModelCreating` calls `modelBuilder.ApplyConfigurationsFromAssembly(typeof(NewDiwanDbContext).Assembly)`
  — so dropping a new configuration class into `Configurations/` is enough to register it.
- **Namespace caveat:** the type lives in `NewDiwan.Infrastructure.Persistence` and its `using`s point at
  `NewDiwan.Domain.*`, which does not exist. See [Known issues](#known-issues--todo).

## Configuration conventions

| Concern | Convention |
| --- | --- |
| Table naming | `SCREAMING_SNAKE_CASE`, plural (e.g. `DOCUMENT_MOVEMENT_STEPS`). |
| Key columns | Explicitly named (`DepartmentId`, `Guid` keys are `ValueGeneratedNever`; `AuditLog.AuditLogId` is an identity column). |
| Lookup tables | Natural string keys (`Code`) and value-generated-never. |
| Timestamps | `datetime2(3)`, `IsRequired` for creation times. |
| Concurrency | `RowVersion` via `.IsRowVersion()` on `DOCUMENTS`, `NUMBERING_COUNTERS`, `DOCUMENT_MOVEMENTS`. |
| Owned collections | Configured with `HasMany(...).WithOne().HasForeignKey(...)`, cascade delete, and `PropertyAccessMode.Field` (backing fields). |
| Indexes | Named with `UX_` (unique) / `IX_` (non-unique) prefixes. |
| `Id` alias + `DomainEvents` | Ignored so only the typed id and persisted fields map to columns. |

## Implementation status

| Item | State |
| --- | --- |
| `DbContext` | ✅ Written |
| All 40 entity configurations | ✅ Written |
| Repository implementations | 🟡 5 of 11 (incl. numbering repositories) |
| Interceptors | ❌ None |
| Migrations | ✅ 2 applied (`InitialCreate`, `AddUniqueOfficialNumber`) |
| Seed data | ✅ Permissions, system roles, demo users (`Seed/` — ordered + environment-gated) |

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-10-18 | opencode | Added `DocumentNumberRepository` + `NumberingRegistryRepository`, the unique `UX_DOC_NUMBERS_TEXT_YEAR` index (`NumberingConfigurations.cs`), and the `AddUniqueOfficialNumber` migration — official numbers are now uniqueness-checked at document creation. |

### Known issues / TODO

1. `NewDiwanDbContext.cs` and every configuration `using NewDiwan.Domain.*` — must be changed to
   `InstitutionalDiwan.Domain.*`.
2. No EF Core package reference exists in the `.csproj`, so the project cannot build even after the
   namespace fix.
3. No migrations exist yet, so no database can be created. First migration should also add the SQL
   Server hardening described in [`Migrations/README.md`](Migrations/README.md).
