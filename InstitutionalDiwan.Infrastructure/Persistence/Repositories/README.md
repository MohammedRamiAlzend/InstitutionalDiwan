# Infrastructure / Persistence / Repositories

> **Status:** Target design — **no code yet**.

## Purpose

Concrete EF Core implementations of the repository interfaces declared in
`InstitutionalDiwan.Domain/Repositories/IDomainRepositories.cs`. Repositories are responsible only for
**loading and attaching** aggregates; they never call `SaveChanges` (that is `IUnitOfWork`, i.e. the
`DbContext` itself). Read models that need speed over richness use the application's
`ISqlConnectionFactory` instead.

## Planned repositories

| Repository (`:` interface) | Key behaviors |
| --- | --- |
| `DepartmentRepository` (`: IDepartmentRepository`) | Queries `DEPARTMENTS`, implements `HasActiveChildrenAsync`, and loads department subtree paths (`DEPARTMENT_TREE_PATHS`) for cycle detection during moves. |
| `UserRepository` (`: IUserRepository`) & `RoleRepository` (`: IRoleRepository`) | Eagerly load `UserRoles`, `DepartmentAssignments`, and `RolePermissions` when hydrating `User` / `Role`. |
| `DocumentRepository` (`: IDocumentRepository`) | Eagerly loads `Participants` and `StatusHistory` for the `Document` aggregate root. |
| `NumberingRegistryRepository` (`: INumberingRegistryRepository`) & `DocumentNumberRepository` (`: IDocumentNumberRepository`) | Uses `UPDLOCK, HOLDLOCK` and/or optimistic `RowVersion` on `NUMBERING_COUNTERS` to guarantee **gap-free, thread-safe** official-number allocation. |
| `DocumentMovementRepository` (`: IDocumentMovementRepository`) | Eagerly loads `Steps` and `Events`; implements `HasActiveMovementsAsync(documentId)` used by document archive/cancel guards. |
| `DocumentFileRepository` (`: IDocumentFileRepository`) | Loads `DocumentFile` with its `Versions` collection. |
| `DocumentRelationRepository` (`: IDocumentRelationRepository`) | Queries bidirectional document links and supports cycle checks when the relation type has `PreventCycles = true`. |
| `AuditLogRepository` (`: IAuditLogRepository`) | **Append-only**; `GetLatestAsync()` returns the prior `EventHash` so the next tamper-evident entry can chain to it. |
| `NotificationRepository` (`: INotificationRepository`) | Persists `Notification`, `NotificationRecipient`, and `NotificationOutbox` records. |

## Conventions

- Use `AsNoTracking()` for read-only loads; only track aggregates that will be mutated.
- Include exactly the collections a use case needs — avoid a universal "load everything" graph.
- `ValueGeneratedNever` Guid keys mean new aggregates keep the id assigned by their `Create(...)` factory.
- Concurrency conflicts surface as `DbUpdateConcurrencyException` and are translated to the application
  `ConcurrencyException` (→ HTTP 409).

## Implementation status

| Repository | State |
| --- | --- |
| All 11 repositories | ❌ Not implemented |

The interfaces and the entity configurations they build on are in place; only the concrete EF Core code
is missing. Nothing compiles until the
[namespace/package issues](../README.md#known-issues--todo) are resolved.
