# Application / Features / Numbering — Module 04 registries and official-number lifecycle

> **Status:** Implemented — registry CRUD (`Registries/`), central number
> issue/cancel/replace/history and department register entries (`Numbers/`), plus the two shared
> helpers below.

## Purpose

Everything that allocates, renders, cancels, or corrects official document numbers. The folder has
two sub-slices and two folder-level helpers: `OfficialNumberFormatter` renders a registry's
`FormatPattern` template, and `NumberingRegistryResolver` resolves both kinds of register — the
central one (most specific match, lazily creating the shared `DEFAULT` registry) and a department's
own register, which has no fallback because a department without one must not number anything. Both
are used by the document-registration flow as well, so a number issued at registration and one
issued later behave identically.

## Contents

| File | Purpose |
| --- | --- |
| `OfficialNumberFormatter.cs` | Renders `{YEAR}` / `{SEQ}` / `{SEQ:D6}` tokens from a registry template into the final official number text (unknown text is preserved verbatim). |
| `NumberingRegistryResolver.cs` | `ResolveAsync` returns the most specific active registry for a central scope (creating the shared `DEFAULT` on first use); `RequireDepartmentRegistryAsync` returns only the registry owned by the given department and fails with `DepartmentRegistryNotConfigured` instead of falling back. |
| `Registries/` | Create, update (template locked once numbers exist), deactivate, activate, list, and get-by-id for numbering registries. |
| `Numbers/` | Issue, cancel, and replace central numbers, record department register entries (typed number + date, one line per department), and read a document's numbers. |

## Conventions / Notes

- Central numbers are unique on `(OfficialNumberText, NumberingYear)` across the whole institution;
  department numbers are unique on `(RegistryId, OfficialNumberText, NumberingYear)`, so two
  departments may both write `45`. Sequence gaps are never reused because
  `DocumentNumber.Cancel`/`ReplaceWith` only changes status.
- The resolver must be called **before** `AllocateNextSequence`, and a rejected registration must
  bail out before allocating (handlers validate status first so a failed call never burns a number).

## Implementation status

| Item | State |
| --- | --- |
| Shared formatter + registry resolver | ✅ |
| `Registries/` read + write slices | ✅ |
| `Numbers/` issue/cancel/replace/history slices | ✅ |
| Department register entries (typed number + date per department) | ✅ |
| Module 05+ (movements, attachments, …) | ❌ Not implemented |

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-10-20 | Buffy | Split registry resolution into central (`ResolveAsync`, default fallback allowed) and department (`RequireDepartmentRegistryAsync`, no fallback) so a document can only be received where a department register exists; documented the per-scope uniqueness rule. |
| 2026-10-19 | Buffy | Created the folder README and added `OfficialNumberFormatter` + `NumberingRegistryResolver`, extracted from the register/issue flows so every number is allocated and rendered the same way. |
