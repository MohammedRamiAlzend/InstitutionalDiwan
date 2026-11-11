# Application / Features / Relations — linking documents to each other

> **Status:** Implemented — Module 08 use cases are complete and served by `DocumentRelationsController`.
> Plan: `plans/module-08-relations-backend.md`.

## Purpose

Lets users link one document to another (reply to, related, supersedes, duplicate of, joined into), read those links
as a list or a graph, and keep a permanent history of every link made or removed. Creating a link saves
the `DocumentRelation` and a `DocumentRelationEvent` in one save, inside the command's transaction.

## Contents

| File | Purpose |
| --- | --- |
| `CreateDocumentRelationCommand.cs` | CQRS command linking a document to another with a type and optional notes. |
| `DocumentRelationLinking.cs` | Creates or removes a link and its history row without saving, including the cycle check. Decision issue and cancel use it so the decision and the join share one save. |
| `CreateDocumentRelationCommandHandler.cs` | Checks the documents, access, archive and canceled state, then `DocumentRelationLinking` and one save. |
| `CreateDocumentRelationCommandValidator.cs` | Requires the ids and a positive type, and limits the notes to 500 characters. |
| `RemoveDocumentRelationCommand.cs` | CQRS command removing a link of a document; the reason is required. |
| `RemoveDocumentRelationCommandHandler.cs` | Checks the document, DOWNLOAD access and that it is not archived, finds a link that touches that document, then `DocumentRelationLinking.Unlink` and one save. |
| `RemoveDocumentRelationCommandValidator.cs` | Requires the ids and a non-blank reason of at most 500 characters. |
| `ListDocumentRelationsQuery.cs` | Query for a document's direct links. |
| `ListDocumentRelationsQueryHandler.cs` | Returns links in both directions to a caller with VIEW access; links to documents the caller cannot view are left out and counted. |
| `GetDocumentRelationsGraphQuery.cs` | Query for the documents reachable within a depth of 1 to 3 (default 2). |
| `GetDocumentRelationsGraphQueryHandler.cs` | Walks level by level, keeping only documents the caller may view and never expanding from a hidden one; caps the graph at 100 documents and reports `truncated`. |
| `ListDocumentRelationEventsQuery.cs` | Paged query for a document's relation history. |
| `ListDocumentRelationEventsQueryHandler.cs` | Returns the history newest first to a caller with VIEW access, including links since removed. |
| `RelationResponses.cs` | Read models: the link with its direction, type flags and related-document summary, the list wrapper with `hiddenRelationCount`, the graph nodes, edges and types, and the history entry. |

## Conventions / Notes

- The first document of a write is the route's document. It must not be archived, but the other one may
  be, so a new reply can point at an archived letter.
- A symmetric type (`Related`) is stored once with ordered endpoints, so the reverse link is a duplicate.
  A cycle is only checked for a type with `PreventCycles` that is not symmetric, within that type.
- The removal deletes the row. The unique index would otherwise block linking the same documents again,
  and `DOCUMENT_RELATION_EVENTS` is the record that remains.
- Reads that list or walk links must keep the VIEW rule in the query (`IDocumentRepository.ListViewableIdsAsync`);
  a hidden document is never returned, followed, or described beyond the count.
- A `Supersedes` link is informational. It does not change the other document's status.

## Implementation status

| Item | State |
| --- | --- |
| Create and remove link | ✅ Implemented |
| List, graph and history reads | ✅ Implemented |
| Editing a link's notes, inverse labels, status effects of `Supersedes` | Out of scope |

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-11-11 | Claude | `DocumentRelationLinking` creates and removes a link without saving. The public commands and a join decision share that path, so a cycle is still refused. |
| 2026-11-03 | Claude | Added the Module 08 relations slice (Phase 2). |
