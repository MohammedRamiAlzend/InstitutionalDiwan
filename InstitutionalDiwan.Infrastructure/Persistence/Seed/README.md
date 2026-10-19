# Seed/

Ordered, environment-aware database seeding.

> **Status:** Implemented — 33 reference-catalog seeders plus permissions, system roles, and demo users.

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
| 41–90 | 33 `LookupSeederBase<T>` subclasses (`IdentityLookupSeeders.cs`, `ParticipantLookupSeeders.cs`, `MovementsLookupSeeders.cs`, `AttachmentsLookupSeeders.cs`, `RelationsLookupSeeders.cs`, `ArchivingLookupSeeders.cs`, `CorrespondenceLookupSeeders.cs`, `AuditLookupSeeders.cs`, `NotificationsLookupSeeders.cs`, `NumberingLookupSeeders.cs`, `OrganizationLookupSeeders.cs`, `DepartmentTypeSeeder.cs`) | All | Every reference catalog: scope/assignment/access levels, participant roles + target types, movement types/statuses/step statuses/event types, storage provider types + file types + the default local provider, relation types + action types, archive event types + locations, document directions/types/statuses/confidentiality/priority, audit action + entity types, notification types/priorities/channels/outbox statuses, numbering modes/statuses/event types/reset policies, department types. |
| 100 | `PermissionSeeder` | All | Permission catalog — the 13 codes in `Permissions.All`, including `LOOKUP_MANAGE`. |
| 200 | `RoleSeeder` | All | `ADMIN` / `USER` roles plus their permission grants (ADMIN receives all 13, USER the three document permissions). |
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
| 2026-10-19 | opencode | `PermissionSeeder` now seeds `LOOKUP_MANAGE` and `RoleSeeder` grants it to `ADMIN` (catalog 12 → 13 codes, ADMIN grants 12 → 13); documented the 33 lookup seeders that this README previously omitted, including `LookupSeederBase<T>` and the 41–90 order range. |
| 2026-10-18 | opencode | Lookup seeders added for every reference catalog during the code-string → lookup-table refactor. |
