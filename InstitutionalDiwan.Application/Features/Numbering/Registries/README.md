# Application / Features / Numbering / Registries — numbering registry management

> **Status:** Implemented — list, get by id, create, update, deactivate, and activate.

## Purpose

CRUD over `NumberingRegistry`: the scope (department, document type, direction) decides which
registry a document is numbered from, the `FormatPattern` decides how the number reads, and the
reset policy decides when sequences restart. The important invariant lives in the update flow — once
a central number has been issued from a registry (`IDocumentNumberRepository.AnyByRegistryAsync`,
which ignores typed department entries) its format pattern is frozen with
`Numbering.RegistryTemplateLocked` (409), otherwise already-issued numbers would stop matching their
template.

The same folder also exposes the register book: `ListDepartmentRegisterQuery` lists one department's
entries ordered by the department's own date, with the central Diwan number kept alongside.

## Contents

| File | Purpose |
| --- | --- |
| `NumberingRegistryResponse.cs` | Read model with the registry's scope, template, and per-year counters (`NumberingCounterResponse`). |
| `ListRegistriesQuery.cs` / `ListRegistriesQueryHandler.cs` | Lists every registry ordered by code, with counters. |
| `GetRegistryByIdQuery.cs` / `GetRegistryByIdQueryHandler.cs` | Fetches one registry by id (404 when missing). |
| `CreateNumberingRegistryCommand.cs` | CQRS command carrying code, Arabic name, optional template, reset policy, and scope. |
| `CreateNumberingRegistryCommandHandler.cs` | Rejects duplicate codes (409), validates scope and reset policy, then persists an active registry. |
| `CreateNumberingRegistryCommandValidator.cs` | FluentValidation rules: required code/name, template ≤ 100 chars, non-negative reset policy. |
| `UpdateNumberingRegistryCommand.cs` | CQRS command for name/template/policy/scope changes (the code is immutable). |
| `UpdateNumberingRegistryCommandHandler.cs` | Revalidates scope + policy, queries `AnyByRegistryAsync`, and applies `NumberingRegistry.Update(hasIssuedNumbers:)`. |
| `UpdateNumberingRegistryCommandValidator.cs` | FluentValidation rules for the update payload. |
| `DeactivateNumberingRegistryCommand.cs` / `DeactivateNumberingRegistryCommandHandler.cs` | Stops the registry being selected for new numbers; issued numbers are untouched. |
| `ActivateNumberingRegistryCommand.cs` / `ActivateNumberingRegistryCommandHandler.cs` | Reactivates a registry so scope matching can select it again. |
| `NumberingRegistryValidation.cs` | Shared scope (department/document type/direction) and reset-policy lookup checks used by create and update. |
| `DepartmentRegisterModels.cs` | `ListDepartmentRegisterQuery` (department + optional year) and the `DepartmentRegisterLineResponse` read model for the register book. |
| `ListDepartmentRegisterQueryHandler.cs` | 404s on an unknown department, loads the department's entries, joins the documents in one batch, and reports each line's register number, date, and arrival count. |

## Conventions / Notes

- Scope fields are optional; `null` acts as a wildcard when `NumberingRegistryRepository` /
  `FakeNumberingRegistryRepository` pick the most specific match.
- A registry with a `DepartmentId` is that department's register; `GetActiveDepartmentRegistryAsync`
  selects it (most specific document type/direction first) and is what the receipt gate requires.
- Writes require `NUMBERING_MANAGE`; reads require `NUMBERING_VIEW`.

## Implementation status

| Item | State |
| --- | --- |
| `ListRegistriesQuery`, `GetRegistryByIdQuery` | ✅ |
| `CreateNumberingRegistryCommand` (+ handler, validator) | ✅ |
| `UpdateNumberingRegistryCommand` (+ handler, validator, template lock) | ✅ |
| `DeactivateNumberingRegistryCommand`, `ActivateNumberingRegistryCommand` | ✅ |
| `ListDepartmentRegisterQuery` (+ handler) | ✅ |

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-10-20 | Buffy | Added the department register book (`ListDepartmentRegisterQuery`) and noted that the template lock now counts only central numbers, so typed department entries cannot freeze a registry format. |
| 2026-10-19 | Buffy | Added registry write use cases (create, update with the issued-numbers template lock, deactivate, activate) plus the shared `NumberingRegistryValidation` scope/policy checks; write endpoints added to `NumberingRegistriesController`. |
