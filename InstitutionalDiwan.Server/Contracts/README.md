# Presentation / Contracts

> **Status:** Target design — **no DTOs implemented yet**.

## Purpose

Immutable HTTP **request and response** record types grouped by module, so external API payloads stay
decoupled from the internal Domain entities. Controllers accept/return these records; MediatR
commands/queries map between them and the domain.

## Planned contracts by module

| Module | Types |
| --- | --- |
| Organization | `CreateDepartmentRequest`, `MoveDepartmentRequest`, `DepartmentTreeResponse` |
| Identity | `LoginRequest`, `LoginResponse`, `CreateUserRequest`, `GrantDocumentAccessRequest` |
| Correspondence | `CreateDraftDocumentRequest`, `AddParticipantRequest`, `RegisterDocumentRequest`, `DocumentDetailsResponse` |
| Numbering | `CreateNumberingRegistryRequest`, `CancelOfficialNumberRequest`, `OfficialNumberResponse` |
| Movements | `CreateMovementRequest`, `RedirectMovementRequest`, `RejectMovementStepRequest`, `MovementTimelineResponse` |
| Attachments | `UploadAttachmentVersionRequest`, `DocumentFileResponse` |
| Archiving | `AdvancedDocumentSearchRequest`, `SaveSearchRequest`, `PagedDocumentsResponse` |
| Relations | `CreateDocumentRelationRequest`, `DocumentRelationGraphResponse` |
| Audit | `AuditLogSearchRequest`, `AuditIntegrityReportResponse` |
| Notifications | `NotificationListResponse`, `UnreadCountResponse` |

## Conventions

- Use `record` types with `init` accessors for immutability.
- Never expose passwords, hashes, tokens, or internal `RowVersion` bytes in responses.
- Keep request/response names suffixed `Request` / `Response` and group them by module (mirroring
  `Features/`).
- Prefer explicit paged/query envelopes (`PagedDocumentsResponse`) over returning raw arrays.

## Implementation status

| Item | State |
| --- | --- |
| All request/response records | ❌ Not implemented |

No types exist yet, so controllers cannot be written against them. See
[`../../InstitutionalDiwan.Application/Features/`](../../InstitutionalDiwan.Application/Features/README.md)
for the use cases these contracts will serve.
