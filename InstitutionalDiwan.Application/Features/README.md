# Application / Features

> **Status:** Target design — **no features implemented yet**.

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
- **Commands:** `CreateDraftDocumentCommand`, `UpdateDraftMetadataCommand`, `AddDocumentParticipantCommand`, `RegisterDocumentCommand`, `CancelDocumentCommand`
- **Queries:** `GetDocumentDetailsQuery`, `GetDocumentTimelineQuery`, `ListDepartmentDocumentsQuery`

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
| Organization, Identity, Correspondence, Numbering, Movements, Attachments, Archiving, Relations, Audit, Notifications | ❌ None implemented |

This is the largest remaining piece of work in the backend. Nothing here can be wired up until the
[`Abstractions/`](../Abstractions/README.md) contracts and [Behaviors](../Behaviors/README.md) exist.
