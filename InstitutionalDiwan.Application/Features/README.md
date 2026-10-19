# Application / Features

> **Status:** Partially implemented — `Lookups` (generic reference-catalog CRUD), Identity (`Login`,
> `Register`, `ChangePassword`, `Users`, `Roles`, `Permissions`, `Employees`, `AccessGrants`),
> Correspondence/`Documents` (create, get by id, list), and Organization/`Departments` (create,
> update, soft delete, activate, get by id, list, tree) slices are live; the remaining modules are
> target design.

## Purpose

Organizes all use cases (Commands, Queries, Validators, DTOs, and Domain Event Handlers) by the 10
InstitutionalDiwan modules using **vertical feature slices**. Each module folder is self-contained:

```
Features/<Module>/
  Commands/      <Command> + <CommandHandler> + <CommandValidator>
  Queries/       <Query>  + <QueryHandler>  + optional <QueryValidator>
  Dtos/          Request/response DTOs specific to the slice
  EventHandlers/ IDomainEventHandler<TDomainEvent> implementations
```

One folder per use case (`Features/Correspondence/Commands/CreateDraftDocument/`) is preferred over
grouping by technical type, so a slice can be read top-to-bottom.

## Planned use cases per module

### Lookups (cross-module)
- **Implemented:** `ListLookupEntriesQuery` ✅, `GetLookupEntryByIdQuery` ✅, `CreateLookupEntryCommand` ✅,
  `RelabelLookupEntryCommand` ✅, `DeactivateLookupEntryCommand` ✅ (rejects an entry that is still
  referenced by another table with `Lookup.EntryInUse`), `ActivateLookupEntryCommand` ✅; see
  [`Lookups/README.md`](Lookups/README.md)

### Organization (Module 01)
- **Commands:** `CreateDepartmentCommand` ✅ (creates under an optional active parent; validates the department type exists and is active; rejects duplicate codes with `Department.CodeAlreadyExists`), `UpdateDepartmentCommand` ✅ (renames, moves parent with self/cycle/inactive guards, updates the external-correspondence policy), `MoveDepartmentCommand` ✅ (dedicated reparent with full-subtree cycle validation → `Department.CyclicHierarchy`), `DeleteDepartmentCommand` ✅ (soft delete via `Deactivate`, blocked by `Department.HasActiveChildren`), `ActivateDepartmentCommand` ✅; `UpdateInstitutionSettingsCommand` planned
- **Queries:** `GetDepartmentTreeQuery` ✅ (root nodes with nested children), `GetDepartmentByIdQuery` ✅, `ListDepartmentsQuery` ✅; `GetInstitutionSettingsQuery` planned

### Identity (Module 02)
- **Commands:** `LoginCommand` ✅, `RegisterCommand` ✅, `ChangePasswordCommand` ✅, `CreateRoleCommand` ✅ (rejects duplicate codes with `Identity.RoleCodeAlreadyExists`), `UpdateRoleCommand` ✅ (names + description; the code is immutable), `DeleteRoleCommand` ✅ (soft delete via `Deactivate`, system roles rejected with `Identity.SystemRoleProtected`), `ActivateRoleCommand` ✅, `GrantRolePermissionCommand` ✅ (validates the code against the permission catalog, 409 `Identity.PermissionAlreadyGranted`), `RevokeRolePermissionCommand` ✅ (idempotent), `CreateEmployeeCommand` ✅, `UpdateEmployeeCommand` ✅, `DeactivateEmployeeCommand` ✅, `ActivateEmployeeCommand` ✅, `GrantDocumentAccessCommand` ✅, `RevokeDocumentAccessCommand` ✅, `CreateUserCommand` ✅ (hashes the raw password, rejects duplicate usernames with `Identity.UsernameTaken`, validates the primary department and optional employee), `UpdateUserCommand` ✅, `DeactivateUserCommand` ✅, `ActivateUserCommand` ✅, `AssignUserRoleCommand` ✅ (records the grantor; 409 `Identity.RoleAlreadyAssigned`), `RevokeUserRoleCommand` ✅; `AssignUserDepartmentScopeCommand` planned
- **Queries:** `ListUsersQuery` ✅, `GetUserByIdQuery` ✅, `ListRolesQuery` ✅, `GetRoleByIdQuery` ✅, `ListPermissionsQuery` ✅ (permission catalog), `ListEmployeesQuery` ✅, `GetEmployeeByIdQuery` ✅, `ListDocumentAccessGrantsQuery` ✅; `GetUserProfileQuery`, `GetUserEffectivePermissionsQuery`, `GetRolesQuery` planned

### Correspondence (Module 03)
- **Commands (planned):** `CreateDraftDocumentCommand` (✅ implemented as `Correspondence/Documents/CreateDocumentCommand` — validates the supplied official number + numbering year, rejects with `Numbering.OfficialNumberTaken` (409) when the pair is taken, otherwise issues a `DocumentNumber`, registers the document, and persists it as `REGISTERED`), `UpdateDraftMetadataCommand`, `AddDocumentParticipantCommand`, `RegisterDocumentCommand`, `CancelDocumentCommand`
- **Queries (partial):** `GetDocumentByIdQuery` ✅, `ListDocumentsQuery` ✅; `GetDocumentDetailsQuery`, `GetDocumentTimelineQuery`, `ListDepartmentDocumentsQuery` planned

### Numbering (Module 04)
- **Commands:** `IssueOfficialNumberCommand`, `CancelOfficialNumberCommand`, `ReplaceOfficialNumberCommand`, `CreateNumberingRegistryCommand`
- **Queries:** `GetDocumentNumberHistoryQuery`, `GetNumberingRegistriesQuery`

### Movements (Module 05)
- **Commands:** `CreateMovementCommand`, `SendMovementCommand`, `ReceiveMovementStepCommand`, `CompleteMovementStepCommand`, `RejectMovementStepCommand`, `ReturnMovementToSenderCommand`, `RedirectMovementCommand`, `CloseMovementCommand`
- **Queries:** `GetDepartmentInboxQuery`, `GetDepartmentOutboxQuery`, `GetMovementTrackingChainQuery`

### Attachments (Module 06)
- **Commands:** `CreateDocumentFileCommand`, `UploadDocumentFileVersionCommand`, `ArchiveDocumentFileCommand`
- **Queries:** `DownloadDocumentFileVersionQuery`, `VerifyAttachmentChecksumQuery`, `ListDocumentFilesQuery`

### Archiving (Module 07)
- **Commands:** `ArchiveDocumentCommand`, `UnarchiveDocumentCommand`, `CreateSavedSearchCommand`
- **Queries:** `SearchDocumentsAdvancedQuery`, `GetSavedSearchesQuery`, `GetDocumentArchiveHistoryQuery`

### Relations (Module 08)
- **Commands:** `CreateDocumentRelationCommand`, `RemoveDocumentRelationCommand`
- **Queries:** `GetDocumentRelationsGraphQuery`

### Audit (Module 09)
- **Commands:** `CreateAuditCheckpointCommand`, `RunAuditIntegrityCheckCommand`
- **Queries:** `GetAuditLogsQuery`, `GetAuditIntegrityReportsQuery`

### Notifications (Module 10)
- **Commands:** `MarkNotificationReadCommand`, `DismissNotificationCommand`
- **Queries:** `GetUserNotificationsQuery`, `GetUnreadNotificationCountQuery`
- **Event handlers:** `SendNotificationOnMovementSentHandler`, `SendNotificationOnDocumentAccessGrantedHandler`

## Mapping to the domain

Each command/query maps to behavior that already exists on the aggregates — for example
`MoveDepartmentCommand → Department.MoveToParent(...)`, `RegisterDocumentCommand → Document.Register(...)`,
`RedirectMovementCommand → DocumentMovement.RedirectTo(...)`, `IssueOfficialNumberCommand →
NumberingRegistry.AllocateNextSequence(...) + DocumentNumber.Issue(...)`. The handlers add the
persistence and authorization concerns; the invariants live in the domain.

## Implementation status

| Module slice | State |
| --- | --- |
| Identity (`Login`, `Register`, `ChangePassword`) | ✅ Implemented |
| Identity/`Users` (list, get by id, create, update, deactivate/activate, assign/revoke role) | ✅ Implemented |
| Identity/`Roles` (create, update, soft delete, activate, get by id, list, grant/revoke permission) + Identity/`Permissions` catalog | ✅ Implemented |
| Identity/`Employees` (create, update, deactivate/activate, get by id, list) | ✅ Implemented |
| Identity/`AccessGrants` (grant, revoke, list by document) | ✅ Implemented |
| Correspondence/`Documents` (create with official number, get by id, list) | ✅ Implemented |
| `Lookups` (list, get by id, create, relabel, deactivate, activate over all 33 catalogs) | ✅ Implemented |
| Organization/`Departments` (create, update, soft delete, activate, get by id, list, tree) | ✅ Implemented |
| Numbering, Movements, Attachments, Archiving, Relations, Audit, Notifications | ❌ Not implemented |

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-10-19 | opencode | Added the cross-module `Lookups` slice exposing every seeded reference catalog through generic list/get/create/relabel/deactivate/activate use cases (see `Lookups/README.md`); documented it as the first entry of the per-module use-case list. |
| 2026-10-18 | opencode | Completed the Identity/`Users` slice (`GetUserByIdQuery`, `CreateUserCommand`, `UpdateUserCommand`, `DeactivateUserCommand`, `ActivateUserCommand`, `AssignUserRoleCommand`, `RevokeUserRoleCommand`); moved `ListUsersQuery`/`UserResponse` into `Features/Identity/Users/` alongside a `UserResponseFactory` (resolves role codes, sorts them); added `User.UpdateProfile`/`Activate` and enriched `UserResponse` with employee, department, scope, and timestamp fields. |
| 2026-10-18 | opencode | Added the Identity/`Employees` slice (`CreateEmployeeCommand`, `UpdateEmployeeCommand`, `DeactivateEmployeeCommand`, `ActivateEmployeeCommand`, `ListEmployeesQuery`, `GetEmployeeByIdQuery`) and the Identity/`AccessGrants` slice (`GrantDocumentAccessCommand`, `RevokeDocumentAccessCommand`, `ListDocumentAccessGrantsQuery`); added `Employee.Update`/`Activate`, the `IEmployeeRepository`/`IDocumentAccessGrantRepository` contracts, the `EMPLOYEE_MANAGE`/`ACCESS_GRANT_MANAGE` permissions, and the `Identity.EmployeeNotFound`/`Identity.AccessGrantNotFound` errors. |
| 2026-10-18 | opencode | Added the Identity/`Roles` slice (`CreateRoleCommand`, `UpdateRoleCommand`, `DeleteRoleCommand`, `ActivateRoleCommand`, `GrantRolePermissionCommand`, `RevokeRolePermissionCommand`, `ListRolesQuery`, `GetRoleByIdQuery`) and the Identity/`Permissions` catalog (`ListPermissionsQuery`); added `Role.UpdateDetails`/`Activate` and the `Identity.RoleCodeAlreadyExists`/`Identity.PermissionNotFound` errors. |
| 2026-10-18 | opencode | Added `MoveDepartmentCommand` (`PUT /api/departments/{id}/parent`) with full-subtree cycle validation, plus `MoveDepartmentRequest`. |
| 2026-10-18 | opencode | Added the Organization/`Departments` slice: `CreateDepartmentCommand`, `UpdateDepartmentCommand` (rename + move + external policy), `DeleteDepartmentCommand` (soft delete), `ActivateDepartmentCommand`, `GetDepartmentByIdQuery`, `ListDepartmentsQuery`, and `GetDepartmentTreeQuery`. |
| 2026-10-18 | opencode | `CreateDocumentCommand` now carries `OfficialNumberText` + `NumberingYear`; the handler checks the pair for duplicates (Conflict → 409), resolves/auto-creates a numbering registry, issues a `DocumentNumber`, and saves the document as REGISTERED instead of DRAFT. |
