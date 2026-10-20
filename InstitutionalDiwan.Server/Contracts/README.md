# Presentation / Contracts

> **Status:** Partially implemented — Correspondence, Organization (incl. institution settings),
> Numbering, Identity (incl. employees and document access grants), and Lookups requests exist; the
> remaining modules are target design.

## Purpose

Immutable HTTP **request and response** record types grouped by module, so external API payloads stay
decoupled from the internal Domain entities. Controllers accept/return these records; MediatR
commands/queries map between them and the domain.

## Planned contracts by module

| Module | Types |
| --- | --- |
| Lookups | `CreateLookupEntryRequest` ✅ (`Id`, `Desc`, `DescAr`), `RelabelLookupEntryRequest` ✅ (`Desc`, `DescAr`); the shared `LookupEntryResponse` read model lives in `Application/Features/Lookups/` |
| Organization | `CreateDepartmentRequest` ✅, `UpdateDepartmentRequest` ✅, `MoveDepartmentRequest` ✅, `UpdateInstitutionSettingsRequest` ✅, `DepartmentTreeResponse` |
| Identity | `LoginRequest` ✅, `RegisterRequest` ✅, `ChangePasswordRequest` ✅, `CreateRoleRequest` ✅, `UpdateRoleRequest` ✅, `GrantPermissionRequest` ✅, `CreateEmployeeRequest` ✅, `UpdateEmployeeRequest` ✅, `GrantDocumentAccessRequest` ✅, `CreateUserRequest` ✅, `UpdateUserRequest` ✅, `AssignUserRoleRequest` ✅, `AssignUserDepartmentScopeRequest` ✅ |
| Correspondence | `CreateDocumentRequest` ✅ (title, type, direction, `OfficialNumberText`, `NumberingYear`, optional summary/levels), `CreateDraftDocumentRequest` ✅, `UpdateDocumentMetadataRequest` ✅, `RegisterDocumentRequest` ✅, `AddDocumentParticipantRequest` ✅, `CancelDocumentRequest` ✅, `CreateExternalOrganizationRequest` ✅, `UpdateExternalOrganizationRequest` ✅ |
| Numbering | `CreateNumberingRegistryRequest` ✅, `UpdateNumberingRegistryRequest` ✅, `IssueOfficialNumberRequest` ✅, `CancelOfficialNumberRequest` ✅, `ReplaceOfficialNumberRequest` ✅, `RecordDepartmentRegistrationRequest` ✅ (department id, typed number, register date, notes), `OfficialNumberResponse` |
| Movements | `ReceiveDocumentRequest` ✅ (optional notes; the register entry must already exist), `CreateMovementRequest`, `RedirectMovementRequest`, `RejectMovementStepRequest`, `MovementTimelineResponse` |
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
| Organization `CreateDepartmentRequest`, `UpdateDepartmentRequest`, `MoveDepartmentRequest` | ✅ Implemented |
| Identity `CreateRoleRequest`, `UpdateRoleRequest`, `GrantPermissionRequest` | ✅ Implemented |
| Identity `CreateUserRequest`, `UpdateUserRequest`, `AssignUserRoleRequest` | ✅ Implemented |
| Identity `CreateEmployeeRequest`, `UpdateEmployeeRequest`, `GrantDocumentAccessRequest` | ✅ Implemented |
| Lookups `CreateLookupEntryRequest`, `RelabelLookupEntryRequest` | ✅ Implemented |
| Organization `UpdateInstitutionSettingsRequest` | ✅ Implemented |
| Correspondence draft/update/participant/cancel + external-organization requests | ✅ Implemented |
| Numbering registry create/update + number issue/cancel/replace requests | ✅ Implemented |
| Department register entry + movement receipt requests | ✅ Implemented |
| Identity `AssignUserDepartmentScopeRequest` | ✅ Implemented |
| Other request/response records | ❌ Not implemented |

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-10-20 | Buffy | Added `RecordDepartmentRegistrationRequest` (department register number + date typed by the clerk) and the `Movements/ReceiveDocumentRequest` folder for the register-gated receipt. |
| 2026-10-19 | Buffy | Added `UpdateInstitutionSettingsRequest`, `CreateDraftDocumentRequest`, `UpdateDocumentMetadataRequest`, `RegisterDocumentRequest`, `AddDocumentParticipantRequest`, `CancelDocumentRequest`, `CreateExternalOrganizationRequest`, `UpdateExternalOrganizationRequest`, `AssignUserDepartmentScopeRequest`, and the `Numbering/` registry + number requests for the Module 01–04 endpoints. |
| 2026-10-19 | opencode | Added `CreateLookupEntryRequest` and `RelabelLookupEntryRequest` (`Contracts/Lookups/LookupContracts.cs`) for the generic lookups endpoints. |
| 2026-10-18 | opencode | Added `CreateUserRequest`, `UpdateUserRequest`, and `AssignUserRoleRequest` for the user-management endpoints. |
| 2026-10-18 | opencode | Added `CreateEmployeeRequest`, `UpdateEmployeeRequest` (for `/api/employees`) and `GrantDocumentAccessRequest` (for `/api/documents/{documentId}/access-grants`). |
| 2026-10-18 | opencode | Added `CreateRoleRequest`, `UpdateRoleRequest`, and `GrantPermissionRequest` for the role-management endpoints. |
| 2026-10-18 | opencode | Added `MoveDepartmentRequest` for `PUT /api/departments/{id}/parent` (optional parent; `null` promotes to root; cycle moves rejected with 409). |
| 2026-10-18 | opencode | Added `CreateDepartmentRequest` and `UpdateDepartmentRequest` for the department CRUD endpoints (parent is optional; update carries name, parent, and external-correspondence policy). |
| 2026-10-18 | opencode | `CreateDocumentRequest` gained required `OfficialNumberText` + `NumberingYear`, which `POST /api/documents` forwards to `CreateDocumentCommand` (409 when the number+year pair is taken). |
