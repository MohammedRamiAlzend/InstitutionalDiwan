# `src/features/lookup-manage` — Reference Catalog Writes (Module 01)

> **Status:** Implemented; build and lint pass, add/duplicate-id flows exercised against the live API.

## Purpose

Writes for the reference catalogs administered on `/lookups`: add an entry with an
administrator-chosen id, relabel it, deactivate and reactivate it. Complements the
`entities/lookup` reads.

## Contents

| File | Purpose |
| --- | --- |
| `api/lookupMutations.ts` | `useCreateLookupEntryMutation` (`POST /api/lookups/{lookup}`), `useRelabelLookupEntryMutation` (`PUT /{id}`), `useSetLookupEntryActiveMutation` (`PATCH /{id}/activate\|deactivate`); every success invalidates `lookupKeys.all` because lookup reads are cached forever. |
| `model/types.ts` | `CreateLookupEntryRequest` and `RelabelLookupEntryRequest` payloads. |
| `model/schemas.ts` | `lookupEntryFormSchema`: positive integer id, both labels required and ≤ 150 characters (mirrors `CreateLookupEntryCommandValidator`). |
| `ui/LookupEntryFormDialog.tsx` | Add/relabel dialog; id pre-filled with the next free id and read-only while editing; shows server errors in Arabic and closes only on success. |
| `index.ts` | Public barrel. |

## Domain rules encoded in the UI

- A duplicate id answers `409 Lookup.IdAlreadyExists`; the dialog stays open.
- `file-storage-providers` and `notification-types` reject creation (`Lookup.CatalogReadOnly`); the widget disables «قيمة جديدة» for them.
- Deactivation is rejected with `409 Lookup.EntryInUse` when other records reference the entry.
- Every write requires `LOOKUP_MANAGE`.

## Implementation status

| Item | State |
| --- | --- |
| Create, relabel, activate, deactivate | ✅ |

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-11-05 | Claude | Added `useSetLookupEntryDefaultMutation` (`PATCH .../set-default` and `.../clear-default`). |
| 2026-11-04 | Claude | Added the Module 01 lookups administration writes (plan `plans/module-01-lookups-frontend.md`). |
