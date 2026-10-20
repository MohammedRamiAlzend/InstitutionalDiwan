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
| `20261020105152_Add_Department_Register_Numbers` | Adds `DOCUMENT_NUMBERS.ScopeId`/`DepartmentId`/`RegistryDate`, `DOCUMENT_NUMBER_HISTORY.EffectiveDate`, `DOCUMENT_MOVEMENT_STEPS.RegistryNumberId`, and the `DOCUMENT_NUMBER_SCOPES` lookup (rows 1–2 inserted inside the migration so the new FK holds for existing rows). Replaces the two global unique indexes with `ScopeId`-filtered ones (`UX_DOC_NUMBERS_REG_YEAR_SEQ`, `UX_DOC_NUMBERS_TEXT_YEAR` for central numbers only), adds `UX_DOC_NUMBERS_DEPT_TEXT_YEAR` + `UX_DOC_NUMBERS_DOC_DEPT` for department registers, and backfills `RegistryDate = IssuedAt` for existing central numbers. |

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
| Department register numbering (`Add_Department_Register_Numbers`) | ✅ Created + applied against LocalDB (2026-10-20) |
| Audit immutability trigger / DENY | ❌ Not created |
| Closure table / indexed view | ❌ Not created |
| Full-text catalog & indexes | ❌ Not created |

## Notes

- Filtered unique indexes (`HasFilter`) require `SET QUOTED_IDENTIFIER ON` on the session that writes
  the table — EF Core migrations set it themselves, but ad-hoc `sqlcmd` sessions must too, otherwise
  inserts fail with error 1934 ("SET options have incorrect settings").

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-10-20 | Buffy | Added `Add_Department_Register_Numbers`: scope columns + lookup seed rows, history `EffectiveDate`, movement-step `RegistryNumberId`, `ScopeId`-filtered uniqueness replacing the global indexes, one-line-per-department index, and the `RegistryDate` backfill; verified by applying it to a throwaway LocalDB database and probing the indexes. |
| 2026-10-18 | opencode | Added `AddUniqueOfficialNumber` — drops `IX_DOC_NUMBERS_TEXT`, creates unique `UX_DOC_NUMBERS_TEXT_YEAR (OfficialNumberText, NumberingYear)` so a duplicate official number + year can never be saved (409 backstop for `CreateDocumentCommand`). |
