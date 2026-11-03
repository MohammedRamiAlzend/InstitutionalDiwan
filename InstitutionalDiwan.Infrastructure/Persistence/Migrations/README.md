# Infrastructure / Persistence / Migrations

> **Status:** Implemented — nine migrations, including the Module 09 `Add_Audit_Chain_Uniqueness` guard and the additive `Add_Document_Dynamic_Fields`,
> are generated. Startup applies them through `Server/Extensions/DatabaseExtensions.cs`
> (`Database.MigrateAsync`).

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
| `20261025100040_Add_Audit_Chain_Uniqueness` | Adds the unique `UX_AUDIT_LOGS_PREVIOUS_HASH` index on `AUDIT_LOGS.PreviousHash`, so a chain row can have exactly one successor. Concurrent appends then surface as a duplicate-key conflict instead of forking the hash chain. |
| `20261102114522_Add_Document_Dynamic_Fields` | Purely additive: nullable `DOCUMENTS.SchemaVersionId` (FK, restrict) and the `DOCUMENT_FIELDS`, `DOCUMENT_TYPE_SCHEMA_VERSIONS`, `DOCUMENT_TYPE_SCHEMA_VERSION_FIELDS` and `DOCUMENT_FIELD_VALUES` tables, with the unique key per type, the one-draft-per-type filtered unique index, the version-number unique index and the sparse typed-value indexes. Existing rows and tables are untouched. |

| `20261103064712_Add_Archiving_Constraints` | Foreign keys from `DOCUMENT_ARCHIVE_EVENTS` to documents, users and status types and from `SAVED_SEARCHES` to users (all restrict); unique `UX_SAVED_SEARCHES_USER_NAME`, filtered unique `UX_SAVED_SEARCHES_USER_DEFAULT` (one default per user), and `IX_DOC_ARCHIVE_EVENTS_TYPE_DATE`, which replaces the single-column event-type index. Both tables were empty, so no data migration is needed. |
| `20261103090137_Add_Relations_Constraints` | Foreign keys from `DOCUMENT_RELATIONS` to documents (source and target), users and `DOCUMENT_RELATION_TYPES`, and from `DOCUMENT_RELATION_EVENTS` to documents, users, types and action types (all restrict); deliberately none from the events to `RelationId`, because the history outlives a removed link. Adds `CK_DOC_RELATIONS_NOT_SELF`, `IX_DOC_RELATIONS_TARGET` and the event indexes by document and date. Both tables were empty, so no data migration is needed. |

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
| Audit chain uniqueness (`Add_Audit_Chain_Uniqueness`) | ✅ Created + applied against LocalDB (2026-10-25) |
| Dynamic document fields (`Add_Document_Dynamic_Fields`) | ✅ Created (2026-11-02) + applied to the development database |
| Archiving constraints (`Add_Archiving_Constraints`) | ✅ Created (2026-11-03) and verified on a scratch SQL Server database; applied to the development database (verified with `dotnet ef migrations list`, 2026-11-03) |
| Relations constraints (`Add_Relations_Constraints`) | ✅ Created (2026-11-03) and applied to the development database (verified with `dotnet ef migrations list`, 2026-11-03) |
| Notifications constraints (`Add_Notifications_Constraints`) | ✅ Created (2026-11-03), verified on a scratch SQL Server database (all 12 migrations apply) and applied to the development database: foreign keys from recipients (user, department) and notifications (document, movement), inbox, retention and unique recipient indexes, the 4000-character outbox payload limit, and a data step that sets the registration notice channels to `IN_APP` |
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
| 2026-11-03 | Claude | Added `Add_Notifications_Constraints` (Module 10): FKs, inbox/retention/unique indexes, payload limit and the `IN_APP` channel fix; verified on a scratch database first. |
| 2026-11-03 | Claude | Confirmed all 11 migrations are applied to the development database (`dotnet ef migrations list` shows none pending; `database update` was a no-op). |
| 2026-11-03 | Claude | Added `Add_Relations_Constraints` for Module 08 (Phase 1). |
| 2026-11-03 | Claude | Added `Add_Archiving_Constraints` for Module 07 (Phase 1) and verified it by applying the full chain to a scratch SQL Server database, since dropped. Recorded that `Add_Document_Dynamic_Fields` is now applied to the development database. |
| 2026-11-02 | Claude | Added `Add_Document_Dynamic_Fields` for the dynamic document fields plan (Phase 1). Generated with `dotnet ef` against a build in a separate configuration, because the running API locks the default `bin`; the snapshot diff is additions only. |
| 2026-10-25 | opencode | Added `Add_Audit_Chain_Uniqueness` (unique `UX_AUDIT_LOGS_PREVIOUS_HASH`) so concurrent audit appends cannot fork the chain. |
| 2026-10-20 | Buffy | Added `Add_Department_Register_Numbers`: scope columns + lookup seed rows, history `EffectiveDate`, movement-step `RegistryNumberId`, `ScopeId`-filtered uniqueness replacing the global indexes, one-line-per-department index, and the `RegistryDate` backfill; verified by applying it to a throwaway LocalDB database and probing the indexes. |
| 2026-10-18 | opencode | Added `AddUniqueOfficialNumber` — drops `IX_DOC_NUMBERS_TEXT`, creates unique `UX_DOC_NUMBERS_TEXT_YEAR (OfficialNumberText, NumberingYear)` so a duplicate official number + year can never be saved (409 backstop for `CreateDocumentCommand`). |
