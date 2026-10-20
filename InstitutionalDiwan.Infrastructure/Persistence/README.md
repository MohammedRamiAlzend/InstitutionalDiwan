# Infrastructure / Persistence

> **Status:** Implemented (partially) — `DiwanDbContext` and ten configuration files exist, plus 14
> of 17 repositories, five applied migrations, and the ordered seeder pipeline. Interceptors remain empty.

## Purpose

Implements the EF Core (SQL Server) persistence concern. The `DbContext` doubles as the
`IUnitOfWork` the Application layer depends on, so a command handler can call `SaveChangesAsync`
without knowing which provider is underneath.

## Contents

| Path | State | Notes |
| --- | --- | --- |
| `DiwanDbContext.cs` | ✅ Written | `DbContext, IUnitOfWork`; 67 `DbSet`s; applies configurations by assembly scan. |
| `Configurations/` | ✅ Written (10 files) | `IEntityTypeConfiguration<T>` for all 67 entities. |
| `Repositories/` | 🟡 Partial (14 of 17) | `UserRepository`, `RoleRepository`, `PermissionRepository`, `EmployeeRepository`, `DepartmentRepository`, `DepartmentTypeRepository`, `DocumentRepository`, `DocumentTypeRepository`, `NumberingRegistryRepository`, `DocumentNumberRepository`, `DocumentAccessGrantRepository`, `LookupRepository`, `InstitutionSettingRepository`, `ExternalOrganizationRepository`, `DocumentMovementRepository`. |
| `Interceptors/` | ❌ Empty | Domain-event dispatch + tamper-evident audit. |
| `Migrations/` | ✅ Written (7) | `InitialCreate`, `AddUniqueOfficialNumber` (unique official-number + year index), three lookup refactors (`Refactor_DepartmentTypes_ToLookup`, `Refactor_CorrespondenceNumbering_ToLookups`, `Refactor_AllRemaining_ToLookups`), `InitialCreate2`, and `Add_Department_Register_Numbers` (department registers + `ScopeId`-filtered uniqueness). |
| `Seed/` | ✅ Written | `ISeeder` + `SeederRunner` (ordering + environment gating); 34 `LookupSeederBase<T>` seeders, `PermissionSeeder` (100), `RoleSeeder` (200), `UserSeeder` (300). |

## `DiwanDbContext` design

- Implements `IUnitOfWork` (`Task<int> SaveChangesAsync(CancellationToken)`).
- Exposes `DbSet`s grouped by module with comments (Organization, Identity, Correspondence, Numbering,
  Movements, Attachments, Archiving, Relations, Audit, Notifications), including the reference/lookup
  tables introduced by the lookup refactor.
- `OnModelCreating` calls `modelBuilder.ApplyConfigurationsFromAssembly(typeof(DiwanDbContext).Assembly)`
  — so dropping a new configuration class into `Configurations/` is enough to register it.
- `OnConfiguring` supplies a LocalDB SQL Server fallback only when no options were configured (design
  time); runtime DI and the SQLite test factory override it.

## Configuration conventions

| Concern | Convention |
| --- | --- |
| Table naming | `SCREAMING_SNAKE_CASE`, plural (e.g. `DOCUMENT_MOVEMENT_STEPS`). |
| Key columns | Explicitly named (`DepartmentId`, `Guid` keys are `ValueGeneratedNever`; `AuditLog.AuditLogId` is an identity column). |
| Lookup tables | Integer keys (`Id`) with `ValueGeneratedNever`, stable ids declared as `const` on the entity, and `Desc`/`DescAr`/`IsActive`/`CreatedAt` columns. |
| Business codes | Non-lookup identifiers (`Role.Code`, `Department.Code`, `NumberingRegistry.Code`, `ExternalOrganization.Code`) remain strings and keep their `UX_` unique indexes. |
| Timestamps | `datetime2(3)`, `IsRequired` for creation times. |
| Concurrency | `RowVersion` via `.IsRowVersion()` on `DOCUMENTS`, `NUMBERING_COUNTERS`, `DOCUMENT_MOVEMENTS`. |
| Owned collections | Configured with `HasMany(...).WithOne().HasForeignKey(...)`, cascade delete, and `PropertyAccessMode.Field` (backing fields). |
| Indexes | Named with `UX_` (unique) / `IX_` (non-unique) prefixes. |
| Lookup FKs | `HasOne<TLookup>().WithMany().HasForeignKey(x => x.XId).OnDelete(DeleteBehavior.Restrict)` — deactivation is the soft delete, the FK keeps history intact. |
| `Id` alias + `DomainEvents` | Ignored so only the typed id and persisted fields map to columns. |

## Implementation status

| Item | State |
| --- | --- |
| `DbContext` | ✅ Written |
| All 66 entity configurations | ✅ Written |
| Repository implementations | 🟡 14 of 17 (lookups, institution settings, external organizations, movements included; files/relations/audit/notifications pending) |
| Interceptors | ❌ None |
| Migrations | ✅ 5 applied (`InitialCreate`, `AddUniqueOfficialNumber`, three lookup refactors) |
| Seed data | ✅ 33 reference catalogs + permissions + system roles + demo users (`Seed/` — ordered + environment-gated) |

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-10-20 | Buffy | Added `DOCUMENT_NUMBER_SCOPES` (`DiwanDbContext`, configuration, migration) and the department-register columns/indexes; `DocumentNumberRepository` queries are now scope-aware and the repository gained `ExistsInScopeAsync`, `GetDepartmentEntryAsync`, and `ListByDepartmentAsync`, while `DocumentRepository` gained `GetWithNumbersAsync` + `GetByIdsAsync`. |
| --- | --- | --- |
| 2026-10-19 | Buffy | Added `Repositories/InstitutionSettingRepository.cs`, `Repositories/ExternalOrganizationRepository.cs`, `Repositories/DocumentMovementRepository.cs`, and `Seed/InstitutionSettingSeeder.cs` (singleton settings + its Central Diwan department); repository count 12 → 14 of 17. |
| 2026-10-19 | opencode | Added `Repositories/LookupRepository.cs` (generic access to every reference catalog via one slug registry) to `Contents`/status; corrected the stale README (it still called the context `NewDiwanDbContext`, counted 5 repositories and 2 migrations, and claimed the project could not build). |
| 2026-10-18 | opencode | Added `DocumentNumberRepository` + `NumberingRegistryRepository`, the unique `UX_DOC_NUMBERS_TEXT_YEAR` index (`NumberingConfigurations.cs`), and the `AddUniqueOfficialNumber` migration — official numbers are now uniqueness-checked at document creation. |
