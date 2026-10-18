# Infrastructure / Persistence / Migrations

> **Status:** Implemented — `InitialCreate` (full schema) and `AddUniqueOfficialNumber` are generated
> and applied at startup by `Server/Extensions/DatabaseExtensions.cs` (`Database.MigrateAsync`).

## Purpose

Holds the EF Core SQL Server migration files that create and evolve the InstitutionalDiwan schema,
plus the hand-written steps that express database-level guarantees the ORM cannot.

## Generating a migration

```bash
dotnet ef migrations add <MigrationName> --project InstitutionalDiwan.Infrastructure --startup-project InstitutionalDiwan.Server --output-dir Persistence/Migrations
```

Always build first (or omit `--no-build`): scaffolding from a stale assembly silently produces an
empty migration when the model changed.

## Existing migrations

| Migration | What it does |
| --- | --- |
| `20261014200146_InitialCreate` | Creates the full schema (40+ tables, `UX_*`/`IX_*` indexes, FKs). |
| `20261018071440_AddUniqueOfficialNumber` | Replaces the non-unique `IX_DOC_NUMBERS_TEXT` on `DOCUMENT_NUMBERS.OfficialNumberText` with the unique `UX_DOC_NUMBERS_TEXT_YEAR (OfficialNumberText, NumberingYear)` — the database-level backstop behind the official-number duplicate check. |

## Custom (hand-written) migration steps

| Step | Purpose |
| --- | --- |
| `AUDIT_LOGS` immutability | SQL Server `TRIGGER` (or `DENY UPDATE, DELETE` grants) so audit records cannot be altered even by the standard application connection. Complements the domain-level SHA-256 hash chain. |
| `DEPARTMENT_TREE_PATHS` | Closure-table maintenance (or indexed view) to make department subtree queries fast and cycle detection cheap (Module 01). |
| Full-Text Search | `CONTAINS` / `FREETEXT` catalog and indexes on `DOCUMENTS.Title` and `DOCUMENTS.SubjectSummary` (Module 07). |
| Unique/referential guards | The unique indexes (`UX_*`) and restricted/cascading foreign keys are already declared in `Configurations/`, so they arrive with the generated migration; verify deletes still respect a `Restrict` on self-referencing `DEPARTMENTS.ParentDepartmentId` and `DEPARTMENT_TYPES`. |

## Implementation status

| Item | State |
| --- | --- |
| Initial migration | ✅ Created + applied |
| Unique official-number index (`UX_DOC_NUMBERS_TEXT_YEAR`) | ✅ Created + applied (2026-10-18) |
| Audit immutability trigger / DENY | ❌ Not created |
| Closure table / indexed view | ❌ Not created |
| Full-text catalog & indexes | ❌ Not created |

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-10-18 | opencode | Added `AddUniqueOfficialNumber` — drops `IX_DOC_NUMBERS_TEXT`, creates unique `UX_DOC_NUMBERS_TEXT_YEAR (OfficialNumberText, NumberingYear)` so a duplicate official number + year can never be saved (409 backstop for `CreateDocumentCommand`). |
