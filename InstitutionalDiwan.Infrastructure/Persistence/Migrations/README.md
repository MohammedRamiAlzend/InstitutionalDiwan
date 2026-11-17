# Infrastructure / Persistence / Migrations

> **Status:** Implemented — the consolidated `InitialCreate` migration holds the full SQL Server
> schema for the .NET 11 target, followed by incremental migrations (currently
> `AddUiLabelOverrides`). Startup applies them in Development through
> `Server/Extensions/DatabaseExtensions.cs` (`Database.MigrateAsync`).

## Purpose

Holds the EF Core SQL Server migrations for the InstitutionalDiwan schema. The incremental
migration history was squashed into a single `InitialCreate` during the .NET 11 upgrade, so a
fresh database is created by applying exactly one migration.

## Migration

| Migration | What it does |
| --- | --- |
| `20261115073415_InitialCreate` | Creates the full schema: 84 tables, `UX_*`/`IX_*` indexes, foreign keys and constraints, as declared by the entity configurations in `Configurations/`. |
| `20261117093850_AddUiLabelOverrides` | Creates `UI_LABEL_OVERRIDES` (LabelId PK, unique `UX_UI_LABEL_OVERRIDES_KEY` on `Key`, Value ≤ 200, UpdatedAt, UpdatedByUserId) for the editable UI terminology (plan `plans/ui-labels-edit-mode.md`). |

> Existing databases that predate the squash have an `__EFMigrationsHistory` that no longer
> matches the code. Drop them (or the history) before the next startup, otherwise
> `MigrateAsync` tries to apply `InitialCreate` on top of existing objects and fails.

## Generating a migration

The repo pins `dotnet-ef` 11.0.0 in [`.config/dotnet-tools.json`](../../../../.config/dotnet-tools.json) —
use it through the local tool manifest:

```bash
dotnet tool run dotnet-ef migrations add <MigrationName> \
  --project InstitutionalDiwan.Infrastructure \
  --startup-project InstitutionalDiwan.Server \
  --output-dir Persistence/Migrations
```

Always build first (or omit `--no-build`): scaffolding from a stale assembly silently produces an
empty migration when the model changed.

## Custom (hand-written) migration steps

| Step | Purpose |
| --- | --- |
| `AUDIT_LOGS` immutability | SQL Server `TRIGGER` (or `DENY UPDATE, DELETE` grants) so audit records cannot be altered even by the standard application connection. Complements the domain-level SHA-256 hash chain. |
| Full-Text Search | `CONTAINS` / `FREETEXT` catalog and indexes on `DOCUMENTS.Title` and `DOCUMENTS.SubjectSummary` (Module 07). |
| Unique/referential guards | The unique indexes (`UX_*`) and restricted/cascading foreign keys are already declared in `Configurations/`, so they arrive with the generated migration; verify deletes still respect a `Restrict` on self-referencing `DEPARTMENTS.ParentDepartmentId` and `DEPARTMENT_TYPES`. |

## Notes

- Filtered unique indexes (`HasFilter`) require `SET QUOTED_IDENTIFIER ON` on the session that writes
  the table — EF Core migrations set it themselves, but ad-hoc `sqlcmd` sessions must too, otherwise
  inserts fail with error 1934 ("SET options have incorrect settings").

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-11-17 | Qoder | Added `AddUiLabelOverrides` (the `UI_LABEL_OVERRIDES` table with a unique key index) — the first incremental migration after the squash. |
| 2026-11-15 | Qoder | Squashed the incremental migration history into a single consolidated `InitialCreate` (84 tables) regenerated with the local `dotnet-ef` 11.0.0 tool; existing development databases must be dropped or recreated. |
