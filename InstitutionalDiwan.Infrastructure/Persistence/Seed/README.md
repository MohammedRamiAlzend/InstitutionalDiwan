# Seed/

Ordered, environment-aware database seeding.

## How it works

| Piece | Role |
| --- | --- |
| `ISeeder` | Contract: `Order` (execution position, lower first), `Environments` (host environments it may run in), `SeedAsync` (idempotent work). |
| `SeedingEnvironments` | Well-known environment names: `All` (wildcard), `Development`, `Staging`, `Production`. |
| `SeederContext` | Shared services per run: `Db`, `PasswordHasher`, `EnvironmentName`. |
| `SeederRunner` | Sorts registered seeders by `Order` (ties broken by type name), filters by environment, executes, returns the count. |

Seeders are registered in DI (`AddInfrastructure`) and executed once at startup by
`DatabaseExtensions.InitializeDatabaseAsync`, which passes the host's current environment name
(`app.Environment.EnvironmentName`), so each seeder decides on its own whether it runs in
Development, Production, etc.

## Registered seeders

| Order | Seeder | Environments | Data |
| --- | --- | --- | --- |
| 100 | `PermissionSeeder` | All | Permission catalog (document, department, role, user, report permissions). |
| 200 | `RoleSeeder` | All | `ADMIN` / `USER` roles plus their permission grants. |
| 300 | `UserSeeder` | Development, Staging | Demo accounts `admin` / `user` (password `12345678`) with role assignments. |

Order matters: roles grant permission codes that the catalog defines first, and demo users are
assigned roles that must already exist.

All seeders are idempotent — they only insert what is missing and are safe to run on every
startup. Demo credentials are deliberately gated to Development/Staging; seeding users in
Production requires opting in by adding `SeedingEnvironments.Production` to
`UserSeeder.Environments` and supplying credentials from configuration.
