# Application / Features / Archiving — archiving documents and searching the archive

> **Status:** Partially implemented — the use cases are complete, but no HTTP endpoint exposes them
> yet (Module 07, Phase 3). Retention periods are out of scope; see `plans/module-07-archiving-backend.md`.

## Purpose

Moves registered documents into the archive and back, keeps a history of those transitions, and
searches what is archived. Each transition changes the document, its status history, and a
`DocumentArchiveEvent` in one save, inside the command's transaction. Saved searches live in
[`SavedSearches/`](SavedSearches/README.md).

## Contents

| File | Purpose |
| --- | --- |
| `ArchiveDocumentCommand.cs` | CQRS command archiving a document at a location, with an optional reason. |
| `ArchiveDocumentCommandHandler.cs` | Checks the document exists, `MANAGE` access, an active location and active movements, then runs `Document.Archive` and records the `ARCHIVED` event in the same save. |
| `ArchiveDocumentCommandValidator.cs` | Requires the ids and a positive location, and limits the reason to 500 characters. |
| `UnarchiveDocumentCommand.cs` | CQRS command restoring an archived document; the reason is required. |
| `UnarchiveDocumentCommandHandler.cs` | Checks the document exists and `MANAGE` access (or archive-diwan custody), then runs `Document.Unarchive`, revokes an in-force send-to-archive decision with the same reason, and records the `UNARCHIVED` event in the same save. |
| `UnarchiveDocumentCommandValidator.cs` | Requires the ids and a non-blank reason of at most 500 characters. |
| `ArchiveDiwanCustody.cs` | Resolves the designated archive diwan and whether it holds a document (its latest movement that was not canceled ended there and was received); shared by archive, unarchive and the document details read. |
| `ListDocumentArchiveEventsQuery.cs` | Query for one document's archive history. |
| `ListDocumentArchiveEventsQueryHandler.cs` | Returns the history newest first to a caller with `VIEW` access on the document. |
| `SearchArchivedDocumentsQuery.cs` | Paged archive search with free text, sort, department, type, direction, location and archive-date filters. |
| `SearchArchivedDocumentsQueryHandler.cs` | Passes the caller, their accessible departments and the current time to the repository, which applies the `VIEW` rule in SQL. |
| `ArchivedDocumentResponse.cs` | Search row: the `DocumentResponse` plus who archived it, when, and the location. |
| `DocumentArchiveEventResponse.cs` | Read model for one archive transition. |
| `ArchiveSearchCriteria.cs` | The stored shape of a saved search; `IsValid` rejects malformed JSON, unknown fields, unknown sort keys or directions and unreadable dates. |

## Conventions / Notes

- Writes require `MANAGE` through `IDocumentAccessEvaluator`, the same rule as attachment writes:
  scope alone grants `MANAGE` only at an institution-wide scope, so a department-scoped archivist
  needs a `MANAGE` grant on the document.
- An archived document rejects changes with `Correspondence.DocumentArchived`, enforced by
  `Document.EnsureNotArchived` in the Attachments, Numbering and participant write paths. Reads,
  downloads, checksum verification and access grants stay available.
- `SearchArchivedDocumentsQueryHandler` must keep access in the query. Filtering a fetched page would
  make the paging counters reveal documents the caller cannot see.

## Implementation status

| Item | State |
| --- | --- |
| Archive and unarchive commands | ✅ |
| Archive history and search queries | ✅ |
| HTTP endpoints | ❌ Phase 3 |
| Retention periods | ❌ Out of scope |

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-11-11 | Claude | Reopening a document revokes an in-force decision whose effect is send-to-archive, using the reopen reason. A referral or any other effect stays in force, and no return movement is created. |
| 2026-11-11 | Claude | With an archive diwan designated, archiving happens there: the document must be held by the archive diwan and the caller's scope must cover it (custody replaces `MANAGE` and the owning-department rule), and the archiving closes the movement that brought the document. A document the archive diwan holds is unarchived by the archive diwan only. Without an archive diwan nothing changes. |
| 2026-11-10 | Claude | Archive and unarchive are limited to the department that created the document (`Correspondence.OnlyOwningDepartmentCanClose`); archiving locks every open department record. |
| 2026-11-08 | Claude | `ArchiveSearchCriteria` accepts the keys the archive grid saves (`sort`, `dir`, `archivedFrom`, `archivedTo`); a saved search with a sort or archive dates was rejected as invalid. |
| 2026-11-08 | Claude | `SearchArchivedDocumentsQuery` accepts `FieldFilters`; saved-search criteria accept the joined field filters (`F`). |
| 2026-11-03 | Claude | Added the slice for Module 07, Phase 2: archive and unarchive commands, archive history and access-filtered search queries, and the saved search criteria check. |
