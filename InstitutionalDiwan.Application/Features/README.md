# Application / Features

> **Status:** Partially implemented — `Lookups` (generic reference-catalog CRUD), Identity (`Login`,
> `Register`, `ChangePassword`, `Users`, `Roles`, `Permissions`, `Employees`, `AccessGrants`),
> Correspondence/`Documents` (full Module 03 lifecycle) and `ExternalOrganizations`, Organization
> (`Departments`, `InstitutionSettings`), Numbering (`Registries`, `Numbers`), and the whole
> Movements slice (routing workflow + inbox/outbox) are live; Attachments, Archiving, Relations,
> Audit, and Notifications are target design. The four unbounded list queries are server-paged,
> searchable, and sortable.

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
- **Commands:** `CreateDepartmentCommand` ✅ (creates under an optional active parent; validates the department type exists and is active; rejects duplicate codes with `Department.CodeAlreadyExists`), `UpdateDepartmentCommand` ✅ (renames, moves parent with self/cycle/inactive guards, updates the external-correspondence policy), `MoveDepartmentCommand` ✅ (dedicated reparent with full-subtree cycle validation → `Department.CyclicHierarchy`), `DeleteDepartmentCommand` ✅ (soft delete via `Deactivate`, blocked by `Department.HasActiveChildren`), `ActivateDepartmentCommand` ✅, `UpdateInstitutionSettingsCommand` ✅ (validates the Central Diwan department is an active root of type `CENTRAL_DIWAN`; moving it requires a transfer reason; numbering mode/reset policy checked against their lookups — see [`Organization/InstitutionSettings/README.md`](Organization/InstitutionSettings/README.md))
- **Queries:** `GetDepartmentTreeQuery` ✅ (root nodes with nested children), `GetDepartmentByIdQuery` ✅, `ListDepartmentsQuery` ✅; `GetInstitutionSettingsQuery` ✅

### Identity (Module 02)
- **Commands:** `LoginCommand` ✅, `RegisterCommand` ✅, `ChangePasswordCommand` ✅, `CreateRoleCommand` ✅ (rejects duplicate codes with `Identity.RoleCodeAlreadyExists`), `UpdateRoleCommand` ✅ (names + description; the code is immutable), `DeleteRoleCommand` ✅ (soft delete via `Deactivate`, system roles rejected with `Identity.SystemRoleProtected`), `ActivateRoleCommand` ✅, `GrantRolePermissionCommand` ✅ (validates the code against the permission catalog, 409 `Identity.PermissionAlreadyGranted`), `RevokeRolePermissionCommand` ✅ (idempotent), `CreateEmployeeCommand` ✅, `UpdateEmployeeCommand` ✅, `DeactivateEmployeeCommand` ✅, `ActivateEmployeeCommand` ✅, `GrantDocumentAccessCommand` ✅, `RevokeDocumentAccessCommand` ✅, `CreateUserCommand` ✅ (hashes the raw password, rejects duplicate usernames with `Identity.UsernameTaken`, validates the primary department and optional employee), `UpdateUserCommand` ✅, `DeactivateUserCommand` ✅, `ActivateUserCommand` ✅, `AssignUserRoleCommand` ✅ (records the grantor; 409 `Identity.RoleAlreadyAssigned`), `RevokeUserRoleCommand` ✅, `AssignUserDepartmentScopeCommand` ✅ (starts a temporal assignment; 409 when the department is already assigned), `EndUserDepartmentScopeCommand` ✅ (keeps the row with its end date)
- **Queries:** `ListUsersQuery` ✅ (paged + username search), `GetUserByIdQuery` ✅, `ListRolesQuery` ✅, `GetRoleByIdQuery` ✅, `ListPermissionsQuery` ✅ (permission catalog), `ListEmployeesQuery` ✅ (paged + name/number search), `GetEmployeeByIdQuery` ✅, `ListDocumentAccessGrantsQuery` ✅, `GetUserEffectivePermissionsQuery` ✅ (role codes + permission claims + active assignments, mirroring the token); `GetUserProfileQuery`, `GetRolesQuery` planned

### Correspondence (Module 03)
- **Commands:** `CreateDocumentCommand` ✅ (single-step create with an explicit official number: validates the number + numbering year, rejects with `Numbering.OfficialNumberTaken` (409) when the pair is taken, issues a `DocumentNumber`, saves as `REGISTERED`), `CreateDraftDocumentCommand` ✅ (DRAFT, no number consumed), `UpdateDraftMetadataCommand` ✅ (only DRAFT editable → `Correspondence.OnlyDraftEditable`), `RegisterDocumentCommand` ✅ (DRAFT → REGISTERED with a registry-allocated number), `AddDocumentParticipantCommand` ✅ (exactly-one target; one external organization max), `CancelDocumentCommand` ✅ (reason required; blocked by active movements), `CreateExternalOrganizationCommand` ✅, `UpdateExternalOrganizationCommand` ✅, `DeactivateExternalOrganizationCommand` ✅, `ActivateExternalOrganizationCommand` ✅ (see [`Correspondence/ExternalOrganizations/README.md`](Correspondence/ExternalOrganizations/README.md))
- **Queries:** `GetDocumentByIdQuery` ✅, `GetDocumentDetailsQuery` ✅, `GetDocumentTimelineQuery` ✅ (status + participant + number events), `ListDocumentsQuery` ✅ (paged, searchable, sortable, filterable by status/department/direction/type), `ListExternalOrganizationsQuery` ✅ (paged + code/name search), `GetExternalOrganizationByIdQuery` ✅; `ListDepartmentDocumentsQuery` planned

### Numbering (Module 04)
- **Commands:** `IssueOfficialNumberCommand` ✅, `CancelOfficialNumberCommand` ✅ (reason required; sequence never reused), `ReplaceOfficialNumberCommand` ✅ (links the replacement and re-points the document), `RecordDepartmentRegistrationCommand` ✅ (clerk-typed number + date in a department register; a second arrival keeps the number and adds a date), `CreateNumberingRegistryCommand` ✅, `UpdateNumberingRegistryCommand` ✅ (format locked once central numbers exist), `DeactivateNumberingRegistryCommand` ✅, `ActivateNumberingRegistryCommand` ✅
- **Queries:** `GetDocumentNumberHistoryQuery` ✅, `GetDocumentRegistryNumbersQuery` ✅ (central + per-department lines), `ListDepartmentRegisterQuery` ✅ (register book), `ListRegistriesQuery` ✅, `GetRegistryByIdQuery` ✅

### Movements (Module 05)
- **Commands:** `ReceiveDocumentCommand` ✅ (blocked until the destination department recorded its register number), `CreateMovementCommand` ✅ (only a REGISTERED document can be routed; one active movement per document), `SendMovementCommand` ✅, `CompleteMovementStepCommand` ✅, `RejectMovementStepCommand` ✅, `ReturnMovementToSenderCommand` ✅, `RedirectMovementCommand` ✅, `CloseMovementCommand` ✅, `CancelMovementCommand` ✅ — see [`Movements/README.md`](Movements/README.md)
- **Queries:** `GetMovementByIdQuery` ✅, `ListDocumentMovementsQuery` ✅, `GetDepartmentInboxQuery` ✅, `GetDepartmentOutboxQuery` ✅ (both read the caller's primary department by default, or any department inside the caller's scope)

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

## Paged list convention

Every list query that can grow without bound returns `PagedResult<T>` from
`InstitutionalDiwan.Application/Common/PagedResult.cs` instead of a bare collection:

```json
{ "items": [ ... ], "page": 1, "pageSize": 20, "totalCount": 42, "totalPages": 3,
  "hasPrevious": false, "hasNext": true }
```

The query record carries `Page`, `PageSize`, `Search`, and (where useful) `SortBy` / `SortDescending`
plus entity-specific filters. The handler wraps them in a `PageRequest`
(`InstitutionalDiwan.Domain/Common/PageRequest.cs`), which clamps the page to ≥1 and the page size to
1–100, and passes it to a repository `ListPagedAsync` that applies the filter and returns a
`PagedList<T>(Items, TotalCount)`. Filtering, sorting, and `Skip`/`Take` therefore run in SQL; the
application layer never loads a whole table to page it in memory.

Currently paged: `ListDocumentsQuery`, `ListUsersQuery`, `ListEmployeesQuery`,
`ListExternalOrganizationsQuery`. Bounded reference data (lookups, permissions, roles, departments,
registries, per-document grants/movements) still returns a plain collection on purpose.

## Implementation status

| Module slice | State |
| --- | --- |
| Identity (`Login`, `Register`, `ChangePassword`) | ✅ Implemented |
| Identity/`Users` (list, get by id, create, update, deactivate/activate, assign/revoke role) | ✅ Implemented |
| Identity/`Roles` (create, update, soft delete, activate, get by id, list, grant/revoke permission) + Identity/`Permissions` catalog | ✅ Implemented |
| Identity/`Employees` (create, update, deactivate/activate, get by id, list) | ✅ Implemented |
| Identity/`AccessGrants` (grant, revoke, list by document) | ✅ Implemented |
| Identity/`Users` department scope (assign, end, effective permissions) | ✅ Implemented |
| Correspondence/`Documents` (immediate create, draft, update, register, participants, cancel, details, timeline, list) | ✅ Implemented |
| Correspondence/`ExternalOrganizations` (create, update, list, get, deactivate, activate) | ✅ Implemented |
| `Lookups` (list, get by id, create, relabel, deactivate, activate over all 33 catalogs) | ✅ Implemented |
| Organization/`Departments` (create, update, soft delete, activate, get by id, list, tree) | ✅ Implemented |
| Organization/`InstitutionSettings` (get, update incl. Central Diwan guards) | ✅ Implemented |
| Numbering/`Registries` + `Numbers` (registry CRUD, issue/cancel/replace, history, department register entries, register book) | ✅ Implemented |
| Movements (routing workflow + inbox/outbox) | ✅ Implemented |
| Attachments, Archiving, Relations, Audit, Notifications | ❌ Not implemented |

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-10-22 | opencode | Added server-side paging, search, sorting, and filtering to the four unbounded list queries (`ListDocumentsQuery`, `ListUsersQuery`, `ListEmployeesQuery`, `ListExternalOrganizationsQuery`). They now return `PagedResult<T>`; the repositories gained `ListPagedAsync` so filtering and `Skip`/`Take` run in SQL. |
| 2026-10-21 | opencode | Completed the Movements slice: the routing workflow (`CreateMovementCommand`, `SendMovementCommand`, `CompleteMovementStepCommand`, `RejectMovementStepCommand`, `ReturnMovementToSenderCommand`, `RedirectMovementCommand`, `CloseMovementCommand`, `CancelMovementCommand`) plus the read side (`GetMovementByIdQuery`, `ListDocumentMovementsQuery`, `GetDepartmentInboxQuery`, `GetDepartmentOutboxQuery`). |
| 2026-10-20 | Buffy | Added department register numbering (`RecordDepartmentRegistrationCommand`, `GetDocumentRegistryNumbersQuery`, `ListDepartmentRegisterQuery`) and the first Movements use case, `ReceiveDocumentCommand`, which refuses custody until that register entry exists. |
| 2026-10-19 | Buffy | Completed Modules 01–04: added `Organization/InstitutionSettings`, `Correspondence/ExternalOrganizations`, the rest of the `Documents` lifecycle (draft/update/register/participants/cancel/details/timeline), `Identity/Users` department-scope commands, and the whole `Numbering` (`Registries`, `Numbers`) slice. |
| 2026-10-19 | opencode | Added the cross-module `Lookups` slice exposing every seeded reference catalog through generic list/get/create/relabel/deactivate/activate use cases (see `Lookups/README.md`); documented it as the first entry of the per-module use-case list. |
| 2026-10-18 | opencode | Completed the Identity/`Users` slice (`GetUserByIdQuery`, `CreateUserCommand`, `UpdateUserCommand`, `DeactivateUserCommand`, `ActivateUserCommand`, `AssignUserRoleCommand`, `RevokeUserRoleCommand`); moved `ListUsersQuery`/`UserResponse` into `Features/Identity/Users/` alongside a `UserResponseFactory` (resolves role codes, sorts them); added `User.UpdateProfile`/`Activate` and enriched `UserResponse` with employee, department, scope, and timestamp fields. |
| 2026-10-18 | opencode | Added the Identity/`Employees` slice (`CreateEmployeeCommand`, `UpdateEmployeeCommand`, `DeactivateEmployeeCommand`, `ActivateEmployeeCommand`, `ListEmployeesQuery`, `GetEmployeeByIdQuery`) and the Identity/`AccessGrants` slice (`GrantDocumentAccessCommand`, `RevokeDocumentAccessCommand`, `ListDocumentAccessGrantsQuery`); added `Employee.Update`/`Activate`, the `IEmployeeRepository`/`IDocumentAccessGrantRepository` contracts, the `EMPLOYEE_MANAGE`/`ACCESS_GRANT_MANAGE` permissions, and the `Identity.EmployeeNotFound`/`Identity.AccessGrantNotFound` errors. |
| 2026-10-18 | opencode | Added the Identity/`Roles` slice (`CreateRoleCommand`, `UpdateRoleCommand`, `DeleteRoleCommand`, `ActivateRoleCommand`, `GrantRolePermissionCommand`, `RevokeRolePermissionCommand`, `ListRolesQuery`, `GetRoleByIdQuery`) and the Identity/`Permissions` catalog (`ListPermissionsQuery`); added `Role.UpdateDetails`/`Activate` and the `Identity.RoleCodeAlreadyExists`/`Identity.PermissionNotFound` errors. |
| 2026-10-18 | opencode | Added `MoveDepartmentCommand` (`PUT /api/departments/{id}/parent`) with full-subtree cycle validation, plus `MoveDepartmentRequest`. |
| 2026-10-18 | opencode | Added the Organization/`Departments` slice: `CreateDepartmentCommand`, `UpdateDepartmentCommand` (rename + move + external policy), `DeleteDepartmentCommand` (soft delete), `ActivateDepartmentCommand`, `GetDepartmentByIdQuery`, `ListDepartmentsQuery`, and `GetDepartmentTreeQuery`. |
| 2026-10-18 | opencode | `CreateDocumentCommand` now carries `OfficialNumberText` + `NumberingYear`; the handler checks the pair for duplicates (Conflict → 409), resolves/auto-creates a numbering registry, issues a `DocumentNumber`, and saves the document as REGISTERED instead of DRAFT. |
