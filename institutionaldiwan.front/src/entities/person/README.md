# `src/entities/person`

> **Status:** Implemented and browser-verified. Plan: `plans/document-persons.md`.

## Purpose

Reads for the people named on documents, and the shared input that collects them by full name.

## Contents

| File | Purpose |
| --- | --- |
| `api/personApi.ts` | Typed `GET` calls for suggestions, list, file, documents, timeline and per-document people. |
| `model/types.ts` | Response, parameter and form-entry types. |
| `model/queries.ts` | `personKeys` and the React Query hooks. |
| `model/normalizeName.ts` | Mirrors the server's Arabic name folding to catch a name entered twice. |
| `ui/PersonNamesInput.tsx` | Multi-person field: suggestions while typing, chips for what was added. Lives here so features can use it without importing a sibling. |
| `index.ts` | Public barrel. |

## Implementation status

| Area | Status |
| --- | --- |
| Slice | ✅ Implemented |

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-11-09 | Claude | Created the slice. |
