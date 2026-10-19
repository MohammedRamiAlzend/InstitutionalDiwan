# Application / Features / Numbering — Module 04 registries and official-number lifecycle

> **Status:** Implemented — registry CRUD (`Registries/`), number issue/cancel/replace/history
> (`Numbers/`), plus the two shared helpers below.

## Purpose

Everything that allocates, renders, cancels, or corrects official document numbers. The folder has
two sub-slices and two folder-level helpers: `OfficialNumberFormatter` renders a registry's
`FormatPattern` template, and `NumberingRegistryResolver` picks (or lazily creates) the registry for
a department/document-type/direction scope. Both are used by the document-registration flow as well,
so a number issued at registration and one issued later behave identically.

## Contents

| File | Purpose |
| --- | --- |
| `OfficialNumberFormatter.cs` | Renders `{YEAR}` / `{SEQ}` / `{SEQ:D6}` tokens from a registry template into the final official number text (unknown text is preserved verbatim). |
| `NumberingRegistryResolver.cs` | `ResolveAsync` — returns the most specific active registry for a scope, creating the shared `DEFAULT` registry on first use so a document can always be numbered. |
| `Registries/` | Create, update (template locked once numbers exist), deactivate, activate, list, and get-by-id for numbering registries. |
| `Numbers/` | Issue, cancel, and replace official numbers for a document, plus the per-document number history query. |

## Conventions / Notes

- Uniqueness of a number is the `(OfficialNumberText, NumberingYear)` pair; sequence gaps are never
  reused because `DocumentNumber.Cancel`/`ReplaceWith` only changes status.
- The resolver must be called **before** `AllocateNextSequence`, and a rejected registration must
  bail out before allocating (handlers validate status first so a failed call never burns a number).

## Implementation status

| Item | State |
| --- | --- |
| Shared formatter + registry resolver | ✅ |
| `Registries/` read + write slices | ✅ |
| `Numbers/` issue/cancel/replace/history slices | ✅ |
| Module 05+ (movements, attachments, …) | ❌ Not implemented |

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-10-19 | Buffy | Created the folder README and added `OfficialNumberFormatter` + `NumberingRegistryResolver`, extracted from the register/issue flows so every number is allocated and rendered the same way. |
