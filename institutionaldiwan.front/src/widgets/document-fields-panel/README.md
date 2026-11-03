# `src/widgets/document-fields-panel — Document Fields Panel`

> **Status:** Implemented and verified in-browser (plan Phase 4).

## Purpose

The «الحقول» tab of a document: values against the schema version the document was pinned to, editable for a draft.

## Contents

| File | Purpose |
| --- | --- |
| `ui/DocumentFieldsPanel.tsx` | Read view (boolean → نعم/لا, select → option label, dates, numbers without padding zeros) and the edit toggle. |
| `index.ts` | Public barrel. |

## Notes

- A document with no pinned schema shows no tab (decided in the details page).
- Registered, canceled and archived documents show the values read-only with a reason.

## Implementation status

| Item | State |
| --- | --- |
| Panel | ✅ Implemented |

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-11-03 | Claude | Implemented per plan `plans/document-dynamic-fields-frontend.md`. |
