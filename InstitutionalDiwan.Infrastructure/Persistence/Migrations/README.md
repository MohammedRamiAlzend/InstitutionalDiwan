# Infrastructure / Persistence / Migrations

> **Status:** Target design — **no migrations exist yet**.

## Purpose

Holds the EF Core SQL Server migration files that create and evolve the InstitutionalDiwan schema,
plus the hand-written steps that express database-level guarantees the ORM cannot.

## Generating a migration

Once the EF Core packages are referenced and the namespaces are fixed:

```bash
dotnet ef migrations add InitialCreate --project   InstitutionalDiwan.Infrastructure --startup-project InstitutionalDiwan.Server  --output-dir Persistence/Migrations
```

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
| Initial migration | ❌ Not created |
| Audit immutability trigger / DENY | ❌ Not created |
| Closure table / indexed view | ❌ Not created |
| Full-text catalog & indexes | ❌ Not created |

Blocked on the [namespace fix and EF Core package reference](../README.md#known-issues--todo) — the
model does not compile yet, so it cannot be scaffolded into a migration.
