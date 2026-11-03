# `src/pages/document-schemas — `/document-schemas``

> **Status:** Implemented behind `LOOKUP_MANAGE`.

## Purpose

Route for administering the dynamic field schemas of document types.

## Contents

| File | Purpose |
| --- | --- |
| `ui/DocumentSchemasPage.tsx` | `RequirePermission` + guide provider + header around `DocumentSchemaEditor`. |
| `index.ts` | Public barrel. |

## Notes

- Type and version are URL search params.

## Implementation status

| Item | State |
| --- | --- |
| Page | ✅ Verified in-browser as admin; `user` gets 403 from the API |

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-11-03 | Claude | Implemented per plan `plans/document-dynamic-fields-frontend.md`. |
