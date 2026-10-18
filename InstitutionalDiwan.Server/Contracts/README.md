# Presentation / Contracts

> **Status:** Partially implemented — Correspondence (`CreateDocumentRequest`), Organization, and
> Identity/Users requests/responses exist; the remaining modules are target design.

## Purpose

Immutable HTTP **request and response** record types grouped by module, so external API payloads stay
decoupled from the internal Domain entities. Controllers accept/return these records; MediatR
commands/queries map between them and the domain.

## Planned contracts by module

| Module | Types |
| --- | --- |
| Organization | `CreateDepartmentRequest` ✅, `UpdateDepartmentRequest` ✅, `MoveDepartmentRequest`, `DepartmentTreeResponse` |
| Identity | `LoginRequest`, `LoginResponse`, `CreateUserRequest`, `GrantDocumentAccessRequest` |
| Correspondence | `CreateDocumentRequest` ✅ (title, type, direction, `OfficialNumberText`, `NumberingYear`, optional summary/levels), `AddParticipantRequest`, `RegisterDocumentRequest`, `DocumentDetailsResponse` |
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
| Correspondence `CreateDocumentRequest` | ✅ Implemented (incl. official number + numbering year) |
| Organization `CreateDepartmentRequest`, `UpdateDepartmentRequest` | ✅ Implemented |
| Other request/response records | ❌ Not implemented |

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-10-18 | opencode | Added `CreateDepartmentRequest` and `UpdateDepartmentRequest` for the department CRUD endpoints (parent is optional; update carries name, parent, and external-correspondence policy). |
| 2026-10-18 | opencode | `CreateDocumentRequest` gained required `OfficialNumberText` + `NumberingYear`, which `POST /api/documents` forwards to `CreateDocumentCommand` (409 when the number+year pair is taken). |
