# InstitutionalDiwan.Tests — xUnit test suite

> **Status:** Implemented — 261 tests covering domain handlers, seeding, authorization, and the
> generic lookups API.

## Purpose

Unit and integration tests for all layers. Handler tests use hand-written fakes (`Support/`) so a use
case can run without a database; anything that depends on EF behaviour (queries, foreign keys, unique
indexes) runs against a private SQLite in-memory database through `Support/TestContextFactory`.

## Layout

| Folder | Contents |
| --- | --- |
| `Application/` | Handler/validator tests per slice — Identity (users, roles, permissions, employees, access grants, login/register/change password), Organization/Departments + `InstitutionSettingsHandlerTests`, Correspondence/`Documents` (`DocumentLifecycleHandlerTests`) + `ExternalOrganizationsHandlerTests`, Numbering (`OfficialNumberLifecycleHandlerTests`), and `LookupHandlerTests.cs`. |
| `Authorization/` | `PermissionAuthorizationHandlerTests` (policy handler + `Permissions.All` integrity), `DepartmentScopeEvaluatorTests` (primary department, institution/dept-tree scope, temporal assignments incl. `IncludeSubtree` and ended assignments), `DocumentAccessEvaluatorTests` (authorship, grants incl. revoked, scope, confidentiality, anonymous). |
| `Seed/` | Seeder and pipeline tests — `PermissionSeederTests`, `RoleSeederTests`, `UserSeederTests`, `DepartmentTypeSeederTests`, `SeederRunnerTests`, `SeedingPipelineTests`. |
| `Support/` | Shared test doubles: `TestContextFactory` (SQLite in-memory `DiwanDbContext`), fake repositories/unit-of-work, fake hasher and JWT provider. |

## Conventions

- One test class per use case (handler + validator), named `...Tests`; assertions use xUnit's built-in
  `Assert` with `Assert.True(result.IsSuccess)` / `result.Error.Code` checks.
- Handler tests construct the handler directly (`internal` handlers are visible via
  `InternalsVisibleTo("InstitutionalDiwan.Tests")` on the Application and Infrastructure projects).
- Database-backed tests use `TestContextFactory.Create()`, which returns a context over an open SQLite
  `:memory:` connection that is disposed with the test.
- Seeding assertions count rows (lookup totals, `Permissions.All` length, role grants) so a new
  permission or lookup must be reflected in the tests — keeping the seed catalog and the tests in sync.

## Notable coverage

- `LookupHandlerTests` exercises the slug registry (all 33 catalogs), active/inactive listing, unknown
  slug → 404, missing entry → 404, duplicate id → 409, read-only catalog → 400, relabel, and the
  reference-in-use guard → 409 (including a reference held through a nullable FK), all against SQLite.
- `SeedingPipelineTests` runs the full ordered seeder set per environment and asserts the resulting
  permission/role/lookup counts, including idempotency on repeated runs.

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-10-19 | Buffy | Added `InstitutionSettingsHandlerTests`, `ExternalOrganizationHandlerTests`, `DocumentLifecycleHandlerTests`, `OfficialNumberLifecycleHandlerTests`, `DepartmentScopeEvaluatorTests`, and `DocumentAccessEvaluatorTests` (60+ new tests) with shared fakes in `Support/FakeServiceRepositories.cs`; updated the permission (13 → 14), role-grant (16 → 17), department-type (4 → 5), and seeder-count expectations for `NUMBERING_MANAGE` and the new `InstitutionSettingSeeder`. |
| 2026-10-19 | opencode | Added `Application/LookupHandlerTests.cs` (13 tests over the generic lookups API) and updated the permission/role/seeding counts for the new `LOOKUP_MANAGE` permission; documented the project layout in this README. |
