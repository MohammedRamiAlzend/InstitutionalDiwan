# Application / Features

> **Status:** Partially implemented — Identity (`Login`, `Register`, `ChangePassword`, `ListUsers`)
> and Correspondence/`Documents` (create, get by id, list) slices are live; the remaining modules are
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

### Organization (Module 01)
- **Commands:** `CreateDepartmentCommand`, `RenameDepartmentCommand`, `MoveDepartmentCommand`, `DeactivateDepartmentCommand`, `UpdateInstitutionSettingsCommand`
- **Queries:** `GetDepartmentTreeQuery`, `GetDepartmentByIdQuery`, `GetInstitutionSettingsQuery`

### Identity (Module 02)
- **Commands:** `LoginCommand`, `RegisterCommand`, `CreateUserCommand`, `AssignUserRoleCommand`, `RevokeUserRoleCommand`, `AssignUserDepartmentScopeCommand`, `GrantDocumentAccessCommand`, `RevokeDocumentAccessCommand`
- **Queries:** `GetUserProfileQuery`, `GetUserEffectivePermissionsQuery`, `GetRolesQuery`

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
| Identity (`Login`, `Register`, `ChangePassword`, `ListUsers`) | ✅ Implemented |
| Correspondence/`Documents` (create with official number, get by id, list) | ✅ Implemented |
| Organization, Numbering, Movements, Attachments, Archiving, Relations, Audit, Notifications | ❌ Not implemented |

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-10-18 | opencode | `CreateDocumentCommand` now carries `OfficialNumberText` + `NumberingYear`; the handler checks the pair for duplicates (Conflict → 409), resolves/auto-creates a numbering registry, issues a `DocumentNumber`, and saves the document as REGISTERED instead of DRAFT. |
