# `src/entities/document-schema` — Dynamic Field Schema Reads

> **Status:** Implemented — reads, keys, types and badges for the dynamic document fields (plan `plans/document-dynamic-fields-frontend.md`, Phase 0).

## Purpose

Read side of the per-document-type field schemas: a type's field identities, its schema version history, one version with its fields, the published schema that renders the create form, and a document's pinned schema with stored values. Writes live in `features/document-schema-manage` and `features/document-field-values`.

## Contents

| File | Purpose |
| --- | --- |
| `api/schemaApi.ts` | `fields`, `versions`, `version`, `typeSchema`, `documentFields` → the `GET` routes of `DocumentFieldsController`. |
| `model/types.ts` | Response types mirroring `DocumentFieldResponses.cs`, the integer enums (`FIELD_DATA_TYPE`, `SCHEMA_STATUS`) and the limits mirrored from the backend. |
| `model/labels.ts` | Arabic labels for data types and version statuses. |
| `model/queries.ts` | `schemaKeys` factory and the `useDocumentTypeFields`, `useSchemaVersions`, `useSchemaVersion`, `useDocumentTypeSchema`, `useDocumentFields` hooks. |
| `model/values.ts` | Form-value helpers: `toFieldValueMap`, `toFieldValueRequests`, `withFieldDefaults`, `withFixedValues`, `visibleFields`, `formatFieldValue`, `validateFieldValues` (local mirror of the backend rules) and `normalizeNumberText`. |
| `ui/DynamicFieldInputs.tsx` | The visible fields of a schema over a key → text map, in the schema's sections and widths; used by create and the details panel. |
| `ui/FieldValueControl.tsx` | The input for one field's data type (text, long text, number, date, yes/no, select, multi-select); also the editor's default-value input. |
| `ui/FieldFilterBar.tsx` | Quick filters for fields marked filterable, the «تصفية بالحقول» button and the chips of other filters; used by the registry and the archive; an explicit `fields` list (a department record form version) replaces the document type's creation form and offers every field in the dialog. |
| `ui/FieldFilterDialog.tsx` | Up to five field/operator/value rows with local validation. |
| `ui/SchemaBadges.tsx` | `SchemaStatusBadge` (draft / published / retired) and `FieldTypeBadge`. |
| `index.ts` | Public barrel. |

## Notes

- The API serializes enums as integers; values are sent and received as text (numbers invariant, dates ISO 8601, booleans `true`/`false`, selects the option key, multi-selects the keys joined by commas).
- `GET /document-types/{id}/schema` answers `schema: null` for a type with no published version; callers hide the dynamic section then.
- Administration reads need `LOOKUP_MANAGE`; the type schema needs `DOCUMENT_VIEW`; a document's fields also need VIEW access to it.
- Error responses carry only the `DocumentFields.*` code plus an English detail, with no structured field key, so per-field errors must be caught by local validation before submit.

## Implementation status

| Item | State |
| --- | --- |
| Reads, keys, hooks, labels, badges, value helpers, dynamic inputs | ✅ Implemented and verified in-browser against the live API (2026-11-03) |

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-11-11 | Claude | `FieldFilterBar` takes an optional `fields` list, so the «سجلاتي» page filters on a record form version's fields. |
| 2026-11-11 | Claude | Added `departmentRegisterForms` and `useDepartmentRegisterForms` (a department's issued record forms per document type, versions newest first). |
| 2026-11-10 | Claude | Schema reads take an optional department (record forms); added department-record types, `departmentRecords` API and `useDepartmentRecords`. |
| 2026-11-08 | Claude | `schemaApi` fills in `behavior` when the API predates it, so a server not yet upgraded no longer blanks the page. |
| 2026-11-08 | Claude | Field `behavior` and the multi-select type; `visibleFields`, `withFieldDefaults`, `withFixedValues`, `formatFieldValue`; added `FieldValueControl`; `FieldFilterBar` and `FieldFilterDialog` moved here from the registry widget so the archive shares them, with quick filters on the bar. |
| 2026-11-04 | Claude | Registry filtering by dynamic field values (plan `plans/document-registry-field-filters.md`): added `model/filters.ts` (operators per data type, Arabic operator labels, `encodeFieldFilter`/`decodeFieldFilter`, local value validation, `MAX_FIELD_FILTERS`). |
| 2026-11-03 | Claude | Added value helpers and `DynamicFieldInputs` (plan Phase 3) and verified the slice against the live API. |
| 2026-11-03 | Claude | Implemented the dynamic fields read slice (plan Phase 0). |
