# `src/features/document-create` — Document Registration & Draft Creation Feature Slice

> **Status:** ✅ Implemented (Module 03 Phase 2, 2026-10-28) —
> one dialog with two paths: save as DRAFT (`POST /api/documents/drafts`) or register immediately
> with an official number (`POST /api/documents`). People named on the document go in the same request.

## 1. Purpose

Owns the write side of document creation. The dialog collects the seeded document type, its dynamic
fields, the people named on the document, and in register mode the department register number, its
date and an optional movement. The server names the document. It invalidates `documentKeys.lists()`
after either mutation so the registry refreshes immediately.

The API assigns the owning department from the signed-in user's primary department; the form does
not ask for it.

## 2. Strict FSD Rules

- Allowed Imports: `@/entities/department`, `@/entities/document`, `@/entities/person`,
  `@/entities/lookup`, `@/entities/user`, and `@/shared/*`.
- Forbidden Imports: NEVER import from `@/app`, `@/pages`, `@/widgets`, or sibling `@/features/*` slices.
- Public API: export the request types, form value types, both mutation hooks, and
  `CreateDocumentDialog` via `src/features/document-create/index.ts`.
- The mounting widget owns the toolbar/menu entries; this slice exports the dialog and mutations.

## 3. Contents

| File | Purpose |
| --- | --- |
| `model/types.ts` | `CreateDocumentRequest` (`POST /api/documents`) and `CreateDraftDocumentRequest` (`POST /api/documents/drafts`), matching the server contracts. |
| `model/schemas.ts` | `createDocumentSchema` — shared Arabic-validated fields plus a `mode: 'draft' \| 'register'` discriminator; the official number and a 4-digit 1900–2200 numbering year are required only when registering. |
| `api/documentMutations.ts` | `useCreateDocumentMutation` and `useCreateDraftDocumentMutation`, invalidating `documentKeys.lists()` on success. |
| `ui/CreateDocumentDialog.tsx` | Radix dialog arranged in titled sections: a two-card mode selector, the department register (register mode), the document-type select and dynamic fields, people named on the document, and sending (register mode), and field-level `Numbering.OfficialNumberTaken` feedback. |
| `ui/FormSection.tsx` | One titled group of the form: icon, title, hint, and an end slot for a badge or switch. |
| `ui/ChoiceCard.tsx` | Radio option drawn as a card, used for the creation mode. |
| `index.ts` | Public barrel for the request/form types, the two mutations, and the dialog. |

## 4. Required Packages

- `@tanstack/react-query`, `react-hook-form`, `zod`, `@hookform/resolvers`, `sonner`, `lucide-react`

## 5. Behaviour notes

- Default mode is **draft**; it starts on **register** only for callers holding `NUMBERING_MANAGE`.
- The dialog renders `toArabicErrorMessage` for other failures and stays open so the user can correct
  the input.
- `Numbering.OfficialNumberTaken` is shown once, on the official-number field; the form-level alert is
  suppressed for that code so the same Arabic message is not printed twice.

## 6. Implementation Status

| Item | State |
| --- | --- |
| Request types + Zod schema | ✅ Implemented |
| Create/draft mutations with query invalidation | ✅ Implemented |
| Create dialog (both modes, lookups, Arabic errors) | ✅ Implemented |
| Browser verification | ✅ Verified in-browser (2026-10-28) against the live API: draft «مسودة تحقق Phase 2» saved as DRAFT with «بدون رقم»; registering an existing `15/2024` returned `409 Numbering.OfficialNumberTaken` as the Arabic field error «رقم النظام مستخدم مسبقًا في هذه السنة.» with the dialog staying open; registering `999/2026` closed the dialog, showed the toast «تم تسجيل الوثيقة برقم 999/2026.», and the row appeared first in the registry; the numbering-year default rendered `2026` from the browser clock. |

## 7. Change Log
- 2026-11-09: The department register section moved to the top of the form.
- 2026-11-09: While `FEATURES.drafts` is off the form hides the mode choice and always registers.
- 2026-11-09: Removed the recipient block: it only recorded a participant and read as if it sent the document. Recipients are added from «إضافة مشارك» on the document page.
- 2026-11-09: Redesigned the form into titled sections with card-style choices.
- 2026-11-09: Removed the document date and external reference fields; anything still needed is defined as a dynamic field on the document type.
- 2026-11-09: Removed the title and summary fields; the server names the document.
- 2026-11-09: The form names people on the document through `PersonNamesInput` (optional); they are sent inside the create request, so they save or fail with the document.
- 2026-11-08: New documents prefill schema defaults and honour read-only, hidden and sectioned fields.
- 2026-11-08: Registering asks for the department register before the number and date.
- 2026-11-05: Removed the direction, confidentiality and priority fields (suspended): the dialog no longer asks for them, no longer auto-fills the direction from the type, and sends none of the three, so the API stores null.
| 2026-11-03 | Claude | The dialog now loads the chosen type's published schema, renders `DynamicFieldInputs` in a full-width fieldset, validates locally and sends `fieldValues`; changing the type clears the values. |

- 2026-10-30: Added the optional recipient block to `CreateDocumentDialog` (checkbox + external/
  department/person target, debounced organization search, active-department picker, person name),
  `CreateDocumentRecipientRequest`, `useAddCreationRecipientMutation`, and the conditional schema
  checks. The recipient is posted as a primary RECEIVER participant after the document is created;
  a participant failure keeps the document and reports only the recipient error.
- 2026-10-28: Fixed a duplicate error: `Numbering.OfficialNumberTaken` is attached to the
  official-number field by `setError`, and the form-level alert no longer repeats it. Confirmed
  in-browser that exactly one `role="alert"` remains after a 409.
- 2026-10-28: Completed the interactive pass for both creation paths (draft and registered) plus the
  conflict and success paths; no further code changes were needed beyond the duplicate-error fix.
- 2026-10-28: Module 03 Phase 2 — implemented the request types, mode-aware Zod schema, both
  create mutations, and `CreateDocumentDialog`; replaced the planned `CreateDocumentForm`/`Modal`
  split with one dialog because both paths share the same field set.
- 2026-10-22: Converted this `README.md` from the Word document it was authored in to markdown.
- 2026-10-19: Created `features/document-create` slice folder and `README.md`.
