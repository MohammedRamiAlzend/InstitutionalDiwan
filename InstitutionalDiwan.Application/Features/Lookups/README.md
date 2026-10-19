# Application / Features / Lookups

> **Status:** Implemented — generic list/get/create/relabel/deactivate/activate slices over every
> seeded reference catalog.

## Purpose

Cross-module CQRS slice backing `LookupsController` (`/api/lookups/{lookup}`). The 33 seeded
reference tables (`DEPARTMENT_TYPES`, `DOCUMENT_TYPES`, `MOVEMENT_STATUSES`, `AUDIT_ACTION_TYPES`, …)
are reached through one slug-addressed contract, `ILookupRepository`, so no per-table repository,
controller, or command is needed. Handlers stay thin: they resolve the slug, delegate to the
repository, and map the tracked `LookupEntity` into `LookupEntryResponse`.

## Contents

| File | Purpose |
| --- | --- |
| `LookupEntryResponse.cs` | Shared read model (`Id`, `Desc`, `DescAr`, `IsActive`, `CreatedAt`) with `From(LookupEntity)`. |
| `ListLookupEntriesQuery.cs` / `ListLookupEntriesQueryHandler.cs` | Lists a catalog ordered by id; `IncludeInactive` (default `false`) also returns deactivated entries. Unknown slug → `Lookup.UnknownCatalog` (404). |
| `GetLookupEntryByIdQuery.cs` / `GetLookupEntryByIdQueryHandler.cs` | Fetches one entry, active or not; missing id → `Lookup.EntryNotFound` (404). |
| `CreateLookupEntryCommand.cs` / `CreateLookupEntryCommandHandler.cs` / `CreateLookupEntryCommandValidator.cs` | Adds an entry with an administrator-chosen stable id; duplicate id → `Lookup.IdAlreadyExists` (409), read-only catalog → `Lookup.CatalogReadOnly` (400). |
| `RelabelLookupEntryCommand.cs` / `RelabelLookupEntryCommandHandler.cs` / `RelabelLookupEntryCommandValidator.cs` | Updates the English/Arabic labels; missing entry → `Lookup.EntryNotFound` (404). |
| `DeactivateLookupEntryCommand.cs` / `DeactivateLookupEntryCommandHandler.cs` | Soft-deletes an entry; an entry referenced by any business row is rejected with `Lookup.EntryInUse` (409) before the state change. |
| `ActivateLookupEntryCommand.cs` / `ActivateLookupEntryCommandHandler.cs` | Re-enables a previously deactivated entry. |

## Conventions / Notes

- The lookup slug is part of the query/command (`Lookup`), not a separate handler family — adding a
  catalog means registering it once in the Infrastructure `LookupCatalog`.
- Handlers call `IsSupported` first so an unknown slug fails with the same 404 whether the operation
  is a read or a write.
- Writes are the only place `IUnitOfWork.SaveChangesAsync` is called; the repository only tracks the
  entity (relabel/activate/deactivate mutate the tracked instance).
- The in-use check reads EF metadata, so a table that starts referencing a lookup automatically
  blocks its deactivation.
- `LOOKUP_MANAGE` guards the write endpoints; reads need only an authenticated token.

## Implementation status

| Item | State |
| --- | --- |
| List / get by id | ✅ |
| Create / relabel / deactivate / activate | ✅ |
| Slug registry covering all 33 seeded catalogs | ✅ (Infrastructure `LookupRepository`) |
| Generic create for `department-types`, `notification-types`, `file-storage-providers` | ❌ Rejected with `Lookup.CatalogReadOnly` (extra fields required) |

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-10-19 | opencode | Added the Lookups slice (`ListLookupEntriesQuery`, `GetLookupEntryByIdQuery`, `CreateLookupEntryCommand`, `RelabelLookupEntryCommand`, `DeactivateLookupEntryCommand`, `ActivateLookupEntryCommand` + validators/handlers) and `LookupEntryResponse` to expose the 33 seeded reference catalogs over HTTP. |
