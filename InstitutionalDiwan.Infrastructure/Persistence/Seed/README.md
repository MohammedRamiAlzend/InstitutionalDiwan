# Seed/

Ordered, environment-aware database seeding.

> **Status:** Implemented — 33 reference-catalog seeders, the institution-settings singleton,
> 16 permissions, system roles with 19 grants, and demo users.

## How it works

| Piece | Role |
| --- | --- |
| `ISeeder` | Contract: `Order` (execution position, lower first), `Environments` (host environments it may run in), `SeedAsync` (idempotent work). |
| `SeedingEnvironments` | Well-known environment names: `All` (wildcard), `Development`, `Staging`, `Production`. |
| `SeederContext` | Shared services per run: `Db`, `PasswordHasher`, `EnvironmentName`. |
| `SeederRunner` | Sorts registered seeders by `Order` (ties broken by type name), filters by environment, executes, returns the count. |
| `LookupSeederBase<TEntity>` | Base for reference-catalog seeders: supplies the fixed `Catalog` of stable ids + English/Arabic labels and inserts only the missing ids, so seeding is idempotent and never overwrites an edited label. |

Seeders are registered in DI (`AddInfrastructure`) and executed once at startup by
`DatabaseExtensions.InitializeDatabaseAsync`, which passes the host's current environment name
(`app.Environment.EnvironmentName`), so each seeder decides on its own whether it runs in
Development, Production, etc.

## Registered seeders

| Order | Seeder | Environments | Data |
| --- | --- | --- | --- |
| 41–90 | 34 `LookupSeederBase<T>` subclasses (`IdentityLookupSeeders.cs`, `ParticipantLookupSeeders.cs`, `MovementsLookupSeeders.cs`, `AttachmentsLookupSeeders.cs`, `RelationsLookupSeeders.cs`, `ArchivingLookupSeeders.cs`, `CorrespondenceLookupSeeders.cs`, `AuditLookupSeeders.cs`, `NotificationsLookupSeeders.cs`, `NumberingLookupSeeders.cs`, `OrganizationLookupSeeders.cs`, `DepartmentTypeSeeder.cs`) | All | Every reference catalog: scope/assignment/access levels, participant roles + target types, movement types/statuses/step statuses/event types, storage provider types + file types + the default local provider, relation types + action types, archive event types + locations, document directions/types/statuses/confidentiality/priority, audit action + entity types, notification types/priorities/channels/outbox statuses, numbering modes/statuses/event types/scopes (central vs department register)/reset policies, department types. |
| 95 | `InstitutionSettingSeeder` | All | The singleton `InstitutionSetting` row (automatic numbering, yearly reset) plus the `CENTRAL_DIWAN` root department it designates; runs after the numbering lookups and department types it references. |
| 100 | `PermissionSeeder` | All | Permission catalog — the 16 codes in `Permissions.All`, including `LOOKUP_MANAGE`, `NUMBERING_MANAGE`, `AUDIT_VIEW`, and `AUDIT_MANAGE`. |
| 200 | `RoleSeeder` | All | `ADMIN` / `USER` roles plus their permission grants (ADMIN receives all 16, USER the three document permissions; 19 grants total). |
| 300 | `UserSeeder` | Development, Staging | Demo accounts `admin` / `user` (password `12345678`) with role assignments. |

Order matters: the lookup catalogs are seeded before permissions/roles so business tables can
reference them, roles grant permission codes that the catalog defines first, and demo users are
assigned roles that must already exist.

All seeders are idempotent — they only insert what is missing and are safe to run on every
startup. Demo credentials are deliberately gated to Development/Staging; seeding users in
Production requires opting in by adding `SeedingEnvironments.Production` to
`UserSeeder.Environments` and supplying credentials from configuration.

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-10-25 | opencode | Added `AUDIT_VIEW` and `AUDIT_MANAGE` to the catalog (14 → 16) and granted both to `ADMIN` (17 → 19 grants); extended the audit entity-type lookup to 12 types. |
| 2026-10-20 | Buffy | Added `DocumentNumberScopeSeeder` (Order 88, `DOCUMENT_NUMBER_SCOPES`: central register / department register) and two department event types (`RegisteredInDepartment`, `ReRegisteredInDepartment`) to `DocumentNumberEventTypeSeeder`; registered the new seeder in `Infrastructure/DependencyInjection.cs` (34 lookup seeders). |
| 2026-10-19 | Buffy | Added `InstitutionSettingSeeder` (Order 95) seeding the settings singleton and its `CENTRAL_DIWAN` root department; `PermissionSeeder` now seeds `NUMBERING_MANAGE` and `RoleSeeder` grants it to `ADMIN` (catalog 13 → 14 codes, ADMIN grants 16 → 17). |
| 2026-10-19 | opencode | `PermissionSeeder` now seeds `LOOKUP_MANAGE` and `RoleSeeder` grants it to `ADMIN` (catalog 12 → 13 codes, ADMIN grants 12 → 13); documented the 33 lookup seeders that this README previously omitted, including `LookupSeederBase<T>` and the 41–90 order range. |
| 2026-10-18 | opencode | Lookup seeders added for every reference catalog during the code-string → lookup-table refactor. |
