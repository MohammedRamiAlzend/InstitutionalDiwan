# `src/features/document-create` — Document Registration & Draft Creation Feature Slice

> **Status:** ✅ Implemented (Module 03 Phase 2, 2026-10-28) — one dialog with two paths: save as
> DRAFT (`POST /api/documents/drafts`) or register immediately with an official number
> (`POST /api/documents`).

## 1. Purpose

Owns the write side of document creation. The dialog collects the title, summary, seeded
type/direction/confidentiality/priority catalogs, and either the draft-only metadata (document date,
external reference) or the registration pair (official number + numbering year). It invalidates
`documentKeys.lists()` after either mutation so the registry refreshes immediately.

The API assigns the owning department from the signed-in user's primary department; the form does
not ask for it.

## 2. Strict FSD Rules

- Allowed Imports: `@/entities/document`, `@/entities/lookup`, `@/entities/user`, and `@/shared/*`.
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
| `ui/CreateDocumentDialog.tsx` | Radix dialog with a two-card mode selector, seeded lookup selects (type, direction, confidentiality, priority), direction auto-filled from seeded document types, draft-only dates/reference, and field-level `Numbering.OfficialNumberTaken` feedback. |
| `index.ts` | Public barrel for the request/form types, mutations, and the dialog. |

## 4. Required Packages

- `@tanstack/react-query`, `react-hook-form`, `zod`, `@hookform/resolvers`, `sonner`, `lucide-react`

## 5. Behaviour notes

- Default mode is **draft**; it starts on **register** only for callers holding `NUMBERING_MANAGE`.
- Selecting كتاب وارد / كتاب صادر / مذكرة داخلية sets the matching direction; تعميم and قرار keep the
  user's chosen direction because the seeded type allows all directions.
- The dialog renders `toArabicErrorMessage` for other failures and stays open so the user can correct
  the input.

## 6. Implementation Status

| Item | State |
| --- | --- |
| Request types + Zod schema | ✅ Implemented |
| Create/draft mutations with query invalidation | ✅ Implemented |
| Create dialog (both modes, lookups, Arabic errors) | ✅ Implemented |
| Browser verification | ⏳ Pending — the open browser has no signed-in session, so draft + registered creation against the live API still needs an interactive pass after sign-in (restart the API first so `identity-options/departments` accepts `DOCUMENT_VIEW`). |

## 7. Change Log

- 2026-10-28: Module 03 Phase 2 — implemented the request types, mode-aware Zod schema, both
  create mutations, and `CreateDocumentDialog`; replaced the planned `CreateDocumentForm`/`Modal`
  split with one dialog because both paths share the same field set.
- 2026-10-22: Converted this `README.md` from the Word document it was authored in to markdown.
- 2026-10-19: Created `features/document-create` slice folder and `README.md`.
