# `src/features/document-person-manage`

> **Status:** Implemented and browser-verified. Plan: `plans/document-persons.md`.

## Purpose

Changes who is named on a document after it exists.

## Contents

| File | Purpose |
| --- | --- |
| `api/documentPersonMutations.ts` | Add, change-role and remove mutations; each invalidates `personKeys.all`. |
| `model/schemas.ts` | Zod schemas of the two dialogs. |
| `ui/AddDocumentPersonsDialog.tsx` | Adds several people in one role; keeps whatever failed in the form. |
| `ui/ChangeDocumentPersonRoleDialog.tsx` | Changes one person's role. |
| `index.ts` | Public barrel. |

## Implementation status

| Area | Status |
| --- | --- |
| Slice | ✅ Implemented |
| UI labels | ✅ Phase 4 (persons): both dialogs render from `persons.add.*`/`persons.change-role.*` — titles/descriptions/toasts/placeholders/aria via `t()`, fields via `FormField labelKey`, buttons via `<Label>` (cancel/save reuse `common.action.*`), Zod messages via `t()` at module load. The shared role field/placeholder/aria reuse `persons.field-role`/`persons.role-*`. Data-driven person names and role `descAr` stay out. |

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-11-18 | Qoder | UI labels Phase 4 (persons): the two dialogs and their Zod schemas render from the `persons.*` catalog. No Arabic literals remain. |
| 2026-11-09 | Claude | Created the slice. |
