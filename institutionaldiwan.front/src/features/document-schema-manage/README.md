# `src/features/document-schema-manage — Schema Administration Writes`

> **Status:** Implemented and verified in-browser (dynamic fields plan Phase 2).

## Purpose

Write side of the per-document-type field schemas: define a field, start/delete the single draft, add/edit/remove draft fields and publish. Reads live in `entities/document-schema`.

## Contents

| File | Purpose |
| --- | --- |
| `api/schemaMutations.ts` | One mutation hook per write route; every success invalidates `schemaKeys.all`. |
| `model/schemas.ts` | Zod schemas: `settingsSchemaFor(dataType)` (regex + mandatory Arabic message, max length, number range, options, display order) and `newFieldSchema` (key pattern, type). |
| `model/toRequest.ts` | Maps form strings to the request (blank → omitted) and seeds form defaults from a field. |
| `model/types.ts` | `FieldSettingsRequest`, `CreateFieldRequest`, `AddSchemaFieldRequest`. |
| `ui/FieldSettingsDialog.tsx` | Add (new identity or an existing unused field) or edit a draft field; settings inputs follow the data type; an identity created before a failed add is reused on retry. |
| `ui/SchemaConfirmDialogs.tsx` | `PublishVersionDialog`, `DeleteDraftDialog` and `RemoveFieldDialog` confirmations. |
| `index.ts` | Public barrel. |

## Notes

- Needs `LOOKUP_MANAGE`; the page guards it, the server enforces it.
- `key` and `dataType` are locked after creation; fields are never deleted, only removed from the draft.
- Server errors render as Arabic banners via `toArabicErrorMessage`.

## Implementation status

| Item | State |
| --- | --- |
| All writes | ✅ Implemented (build and lint pass; draft, add field, publish verified in-browser; delete draft, edit and remove not exercised in-browser) |

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-11-03 | Claude | Implemented per plan `plans/document-dynamic-fields-frontend.md`. |
