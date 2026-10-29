# institutionaldiwan.front / src/features/document-manage — Document Lifecycle Actions

> **Status:** ✅ Implemented (2026-10-28) — draft edit, draft registration, participant insert, and cancel-with-reason.
> Movement routing and number cancellation/replacement belong to later phases; access grants live in
> `features/document-access-grant`.

## Purpose

Owns the four Phase 3 lifecycle commands on one document: editing DRAFT metadata, registering a draft
with the next official number from the numbering registry, adding a participant, and canceling with a
mandatory reason. The details page mounts the dialogs only when the document's status and the caller's
permissions allow the transition, so the slice itself renders no permission banners.

## Contents

| File | Purpose |
| --- | --- |
| `model/types.ts` | `UpdateDraftDocumentRequest`, `RegisterDraftDocumentRequest`, `AddDocumentParticipantRequest`, `CancelDocumentRequest` matching `InstitutionalDiwan.Server.Contracts.Correspondence`. |
| `model/schemas.ts` | Zod schemas and Arabic validation for the edit, register, participant, and cancel forms; participant `superRefine` requires exactly the field matching the chosen target kind. |
| `api/documentMutations.ts` | `useUpdateDraftDocumentMutation`, `useRegisterDraftDocumentMutation`, `useAddDocumentParticipantMutation`, `useCancelDocumentMutation`; all invalidate `documentKeys.detail(documentId)` and the registry lists after writes. |
| `ui/EditDraftDialog.tsx` | Title/summary/confidentiality/priority only (type and direction stay immutable); surfaces `Correspondence.OnlyDraftEditable` in Arabic. |
| `ui/RegisterDraftDialog.tsx` | Optional numbering year (blank = document year, then current year); success toast quotes the allocated `officialNumberText` and surfaces `Numbering.RegistryNotFound`/`Numbering.OfficialNumberTaken`. |
| `ui/AddParticipantDialog.tsx` | Role lookup, three target kinds (external organization, internal department, person), primary flag, notes; external organizations come from a debounced search over `GET /api/external-organizations`, departments from `identity-options`. |
| `ui/CancelDocumentDialog.tsx` | Destructive confirmation with a required reason; surfaces `Correspondence.HasActiveMovements` and `Correspondence.AlreadyCanceled`. |
| `index.ts` | Public barrel exporting request/form types, the four mutations, and the four dialogs. |

## Rules & error handling

- Edit and register are DRAFT-only; the page hides them otherwise, and the server's 409 codes are still
  rendered through `toArabicErrorMessage`.
- Register consumes the next sequence from the registry matching the document's owning department,
  type, and direction — no client-side number entry.
- A document accepts a single external participant (`Correspondence.SingleExternalPartyLimit`); the
  dialog explains the rule but the server is authoritative.
- Cancel is blocked for archived, already canceled, or actively moving documents; the reason is
  client-validated (1–1000 chars) and stored on the status-history row.

## Implementation status

| Item | State |
| --- | --- |
| Four mutations, schemas, dialogs, barrel | ✅ Implemented |
| Interactive verification | ✅ (2026-10-28) Draft edit, registration (`2026/000004`), person and department participants, and cancel-with-reason all round-tripped against the live API; empty-name and empty-reason submissions were rejected in Arabic. |
| External organization creation | Out of scope (Phase 7); the picker reads existing rows only. |
| Number cancel/replace and department registration | Out of scope (Phase 5). |

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-10-28 | agent | Verified the four commands in-browser: draft edit persisted title/summary/priority, registration allocated `2026/000004` and flipped the header to «مسجل», participants resolved to «خالد الحلبي» and «قسم الموارد البشرية», and cancel recorded the Arabic reason and produced «ملغي». |
| 2026-10-28 | agent | Module 03 Phase 3: created the slice with draft edit/register, participant insert (department/external/person), and cancel-with-reason, plus Arabic schema validation and detail/list invalidation. |
