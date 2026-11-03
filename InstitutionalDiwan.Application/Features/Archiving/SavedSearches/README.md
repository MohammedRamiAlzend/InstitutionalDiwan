# Application / Features / Archiving / SavedSearches — a user's saved archive searches

> **Status:** Partially implemented — the use cases are complete, but no HTTP endpoint exposes them
> yet (Module 07, Phase 3).

## Purpose

Lets each user keep named archive searches, with at most one marked as their default. Searches are
personal: they are not shared, and another user's search answers as not found. The server validates
the stored criteria but never runs them; the client passes them to the archive search, where access
is enforced.

## Contents

| File | Purpose |
| --- | --- |
| `CreateSavedSearchCommand.cs` | CQRS command saving a new search for the caller. |
| `CreateSavedSearchCommandHandler.cs` | Validates the criteria, the 50-search limit and the unique name, moves the default if requested, and adds the search. |
| `CreateSavedSearchCommandValidator.cs` | Requires a name of at most 150 characters and criteria of at most 4000. |
| `UpdateSavedSearchCommand.cs` | CQRS command replacing the name, criteria and default flag of one search. |
| `UpdateSavedSearchCommandHandler.cs` | Confirms ownership, validates the criteria and name, moves the default if requested, and applies the update. |
| `UpdateSavedSearchCommandValidator.cs` | Same limits as create, plus the search id. |
| `DeleteSavedSearchCommand.cs` | CQRS command deleting one search permanently. |
| `DeleteSavedSearchCommandHandler.cs` | Confirms ownership and removes the search. |
| `ListSavedSearchesQuery.cs` | Query for the caller's searches. |
| `ListSavedSearchesQueryHandler.cs` | Returns the caller's searches ordered by name. |
| `SavedSearchDefaults.cs` | Clears the user's current default and saves at once, so the filtered unique index never sees two defaults. |
| `SavedSearchResponse.cs` | Read model returned by these use cases. |

## Conventions / Notes

- Moving the default takes two saves inside the command's transaction, in the order clear then set.
  A single save could reach the database in the other order and violate
  `UX_SAVED_SEARCHES_USER_DEFAULT`. `SavedSearchHandlerTests` runs this against a real database.
- Saved searches are not added to the entity audit map; they are personal preferences, and the
  action-level audit layer still records the calls.

## Implementation status

| Item | State |
| --- | --- |
| Create, update, delete and list | ✅ |
| HTTP endpoints | ❌ Phase 3 |

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-11-03 | Claude | Added the slice for Module 07, Phase 2. |
