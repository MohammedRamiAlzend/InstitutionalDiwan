# Application / Features / Correspondence / DocumentFields

> **Status:** Implemented — the Application half of the dynamic document fields plan
> ([`plans/document-dynamic-fields-backend.md`](../../../../plans/document-dynamic-fields-backend.md),
> Phase 2). The HTTP surface (Phase 3) and the front end are not built yet.

## Purpose

Lets an administrator define extra fields per document type and publish them in immutable schema
versions, and lets documents carry values for them. A document is pinned to the published version
that was current when it was created (`Document.SchemaVersionId`) and never moves; its values are
validated against that version and are editable only while the document is a draft. Authorization
(`LOOKUP_MANAGE` for the administration commands) is applied by the controllers in Phase 3, like
every other slice; handlers take the acting `UserId` explicitly.

## Contents

| File | Purpose |
| --- | --- |
| `DocumentFieldCommands.cs` | The command records: `CreateDocumentFieldCommand`, `CreateSchemaDraftCommand`, `DiscardSchemaDraftCommand`, `AddFieldToDraftCommand`, `UpdateDraftFieldCommand`, `RemoveDraftFieldCommand`, `PublishSchemaVersionCommand` and `SetDocumentFieldValuesCommand`. |
| `DocumentFieldQueries.cs` | The query records: `ListDocumentFieldsQuery`, `ListSchemaVersionsQuery`, `GetSchemaVersionQuery`, `GetDocumentTypeSchemaQuery` (the published schema a new document gets) and `GetDocumentFieldsQuery` (a document's pinned schema with its values). |
| `DocumentFieldSettingsInput.cs` | The administrator-editable configuration of a field (label, requiredness, order, regex, range, max length, options) and its conversion to the validated domain `DocumentFieldSettings` using the field's own data type. |
| `DocumentFieldResponses.cs` | Read models: `DocumentFieldResponse`, `SchemaFieldResponse`, `SchemaVersionResponse`, `SchemaVersionSummaryResponse`, `DocumentTypeSchemaResponse`, `DocumentFieldValueResponse` and `DocumentFieldsResponse` (values formatted as text in the form the input accepts). |
| `SchemaFieldCommandHandlers.cs` | Handlers for creating a field identity (unique key per type) and adding, updating and removing a field in a schema draft. |
| `SchemaDraftCommandHandlers.cs` | Handlers for starting a draft (a copy of the published version, numbered next), discarding a draft, and publishing — which retires the previously published version, read before the draft is published. |
| `SetDocumentFieldValuesCommandHandler.cs` | Replaces a draft document's values against its pinned schema, after a `DOWNLOAD` access check; also holds `DocumentSchemaPinning`, the helper the create flows share to apply values (rejecting any value when the type has no schema). |
| `DocumentFieldQueryHandlers.cs` | Handlers for the five queries; reading a document's values requires `VIEW` access to that document (403 `Correspondence.DocumentAccessDenied`). |
| `DocumentFieldCommandValidators.cs` | FluentValidation rules: ids present, key format, data type defined, label length, value list size; type-specific rules stay in the domain. |

## Conventions / Notes

- The schema administration commands never touch documents; documents only ever read a version.
- `CreateDraftDocumentCommand` and `CreateDocumentCommand` (in `Documents/`) pin the schema and accept
  `FieldValues`; `RegisterDocumentCommandHandler` and `CreateDocumentCommandHandler` call
  `Document.EnsureRequiredFieldsComplete` **before** a number is allocated.
- Publishing is audited through the `DocumentSchemaVersionPublished` event (see the audit interceptor);
  field and draft changes are audited as generic entity changes under `AuditEntityType.DocumentSchemaVersionId`.

## Implementation status

| Item | State |
| --- | --- |
| Field, draft, publish and discard commands | ✅ |
| Document value commands and create/register integration | ✅ |
| Queries | ✅ |
| HTTP endpoints, OpenAPI documentation, permissions | ❌ Phase 3 |
| Registry filtering by field values | ❌ later phase |

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-11-03 | Claude | `SetDocumentFieldValuesCommandHandler` now requires `DOWNLOAD` access to the document (403 `Correspondence.DocumentAccessDenied`), matching draft metadata edits. |
| 2026-11-03 | Claude | `GetDocumentFieldsQueryHandler` now requires `VIEW` access to the document, because the values are document content. `SetDocumentFieldValuesCommandHandler` still has no document-level check; it waits for the access levels of the write handlers to be decided. |
| 2026-11-02 | Claude | Created the slice (dynamic document fields, Phase 2): schema administration commands and queries, document value command, and the shared pinning helper. |
