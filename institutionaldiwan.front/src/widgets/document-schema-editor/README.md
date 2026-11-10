# `src/widgets/document-schema-editor — Schema Editor`

> **Status:** Implemented and verified in-browser (plan Phases 1–2).

## Purpose

Administration view of one document type's field schema: version history chips and the selected version's fields on the shared grid, with the draft actions.

## Contents

| File | Purpose |
| --- | --- |
| `ui/DocumentSchemaEditor.tsx` | Type picker (`?type=`), version chips (`?version=`), field grid (`tableId` `document-schema-fields`) with row menu and toolbar actions; dialogs from `features/document-schema-manage`. |
| `index.ts` | Public barrel. |

## Notes

- Only the draft is editable; the new-draft button is disabled while a draft exists.
- Publish needs at least one field.

## Implementation status

| Item | State |
| --- | --- |
| Editor | ✅ Implemented |

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-11-10 | Claude | Form selector (`?dept=`): edit the creation form or a department record form. |
| 2026-11-08 | Claude | The constraints column lists the new field behavior. |
| 2026-11-03 | Claude | Implemented per plan `plans/document-dynamic-fields-frontend.md`. |
