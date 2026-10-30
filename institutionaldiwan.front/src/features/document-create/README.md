# `src/features/document-create` — Document Registration & Draft Creation Feature Slice

> **Status:** ✅ Implemented (Module 03 Phase 2, 2026-10-28; optional recipient added 2026-10-30) —
> one dialog with two paths: save as DRAFT (`POST /api/documents/drafts`) or register immediately
> with an official number (`POST /api/documents`), plus an optional RECEIVER participant attached
> right after creation (`POST /api/documents/{documentId}/participants`).

## 1. Purpose

Owns the write side of document creation. The dialog collects the title, summary, seeded
type/direction/confidentiality/priority catalogs, and either the draft-only metadata (document date,
external reference) or the registration pair (official number + numbering year). It invalidates
`documentKeys.lists()` after either mutation so the registry refreshes immediately.

Since 2026-10-30 it also offers an optional recipient block (external organization, internal
department, or a person by name) for callers holding `DOCUMENT_EDIT`. The recipient is not part of
the create payload: the dialog first creates the document, then posts the participant with
`PARTICIPANT_ROLE.Receiver` and `isPrimary: true`. If the participant call fails, the created
document is kept and only the recipient failure is reported.

The API assigns the owning department from the signed-in user's primary department; the form does
not ask for it.

## 2. Strict FSD Rules

- Allowed Imports: `@/entities/department`, `@/entities/document`, `@/entities/external-organization`,
  `@/entities/lookup`, `@/entities/user`, and `@/shared/*`.
- Forbidden Imports: NEVER import from `@/app`, `@/pages`, `@/widgets`, or sibling `@/features/*` slices.
  The recipient participant mutation lives here (rather than reusing `document-manage`'s) because a
  sibling feature import is not allowed.
- Public API: export the request types, form value types, all three mutation hooks, and
  `CreateDocumentDialog` via `src/features/document-create/index.ts`.
- The mounting widget owns the toolbar/menu entries; this slice exports the dialog and mutations.

## 3. Contents

| File | Purpose |
| --- | --- |
| `model/types.ts` | `CreateDocumentRequest` (`POST /api/documents`), `CreateDraftDocumentRequest` (`POST /api/documents/drafts`), and `CreateDocumentRecipientRequest` (`POST /api/documents/{documentId}/participants`), matching the server contracts. |
| `model/schemas.ts` | `createDocumentSchema` — shared Arabic-validated fields plus a `mode: 'draft' \| 'register'` discriminator; the official number and a 4-digit 1900–2200 numbering year are required only when registering. When `addRecipient` is on, the selected target (`recipientType`) must be filled, with a per-type Arabic message. |
| `api/documentMutations.ts` | `useCreateDocumentMutation` and `useCreateDraftDocumentMutation`, invalidating `documentKeys.lists()` on success, plus `useAddCreationRecipientMutation`, which posts the recipient after creation and invalidates the document detail and lists. |
| `ui/CreateDocumentDialog.tsx` | Radix dialog with a two-card mode selector, seeded lookup selects (type, direction, confidentiality, priority), direction auto-filled from seeded document types, draft-only dates/reference, the optional recipient block (external/department/person, `DOCUMENT_EDIT` only), and field-level `Numbering.OfficialNumberTaken` feedback. |
| `index.ts` | Public barrel for the request/form types, the three mutations, and the dialog. |

## 4. Required Packages

- `@tanstack/react-query`, `react-hook-form`, `zod`, `@hookform/resolvers`, `sonner`, `lucide-react`

## 5. Behaviour notes

- Default mode is **draft**; it starts on **register** only for callers holding `NUMBERING_MANAGE`.
- Selecting كتاب وارد / كتاب صادر / مذكرة داخلية sets the matching direction; تعميم and قرار keep the
  user's chosen direction because the seeded type allows all directions.
- The dialog renders `toArabicErrorMessage` for other failures and stays open so the user can correct
  the input.
- `Numbering.OfficialNumberTaken` is shown once, on the official-number field; the form-level alert is
  suppressed for that code so the same Arabic message is not printed twice.
- The recipient block is hidden without `DOCUMENT_EDIT` (the server is still the authority and would
  answer 403). It is unchecked by default, so the create flow is unchanged when the user skips it.
- The recipient is always a primary RECEIVER participant; the server allows only one external party
  per document, so a document that already carries one cannot accept an external recipient.
- The external-organization picker reuses the entity search (debounced, 20 rows) and the department
  picker lists active departments only. The recipient call runs *after* the document exists, so its
  failure never rolls back the document — the user gets an error toast naming the recipient, and the
  document row is already in the registry.

## 6. Implementation Status

| Item | State |
| --- | --- |
| Request types + Zod schema | ✅ Implemented |
| Create/draft mutations with query invalidation | ✅ Implemented |
| Create dialog (both modes, lookups, Arabic errors) | ✅ Implemented |
| Optional recipient at creation (external/department/person, `DOCUMENT_EDIT` only) | ✅ Implemented (2026-10-30) |
| Browser verification | ✅ Verified in-browser (2026-10-28) against the live API: draft «مسودة تحقق Phase 2» saved as DRAFT with «بدون رقم»; registering an existing `15/2024` returned `409 Numbering.OfficialNumberTaken` as the Arabic field error «الرقم الرسمي مستخدم مسبقًا في هذه السنة.» with the dialog staying open; registering `999/2026` closed the dialog, showed the toast «تم تسجيل الوثيقة برقم 999/2026.», and the row appeared first in the registry; the numbering-year default rendered `2026` from the browser clock. |
| Recipient browser verification | ✅ Verified in-browser (2026-10-30) as `admin`: ticking «إضافة مستلم للوثيقة» and submitting a department recipient without a department showed «اختر القسم المستلم.»; after choosing «قسم الموارد البشرية» the draft was created with the toast «تم حفظ الوثيقة كمسودة. وأُضيف المستلم.»; the detail page `?tab=participants` of document `596424ec-cb7c-4fa6-9ea2-a9537a4e65a2` showed «مستلم \| أساسي \| قسم داخلي · قسم الموارد البشرية». |

## 7. Change Log

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
