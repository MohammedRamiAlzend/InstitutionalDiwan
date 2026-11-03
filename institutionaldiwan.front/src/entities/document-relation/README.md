# `src/entities/document-relation` — Relation Reads

> **Status:** Implemented — links, graph and history reads for Module 08.

## Purpose

Read side of Module 08: a document's links (with the hidden-link count), the relations graph rooted at it, and the
permanent relation history. Writes live in `features/document-relation-manage`.

## Contents

| File | Purpose |
| --- | --- |
| `api/relationApi.ts` | `list`, `graph`, `events` → `GET /api/documents/{id}/relations`, `.../graph?depth=`, `.../events`. |
| `model/types.ts` | Response types mirroring `RelationResponses.cs` plus the graph depth bounds (1–3, default 2). |
| `model/queries.ts` | `relationKeys` factory and the `useDocumentRelations`, `useRelationGraph`, `useRelationEvents` hooks. |
| `model/direction.ts` | Composes the Arabic phrase of a link from its type and the server's direction (the inverse reading is built here). |
| `model/graph.ts` | Turns the graph into Mermaid source (sequential node ids mapped back to document ids) and into an accessible tree. |
| `ui/RelationTypeBadge.tsx` | Reply-to / related / supersedes / duplicate-of badge with its Arabic label. |
| `ui/RelationDirectionLabel.tsx` | The link read from the viewed document, e.g. «هذه الوثيقة ردّ على». |
| `index.ts` | Public barrel. |

## Notes

- Links to documents the caller cannot view are omitted by the server and only counted in `hiddenRelationCount`.
- Diagram labels strip characters that break Mermaid; node ids are `n1…nK` because GUIDs are not valid node ids.

## Implementation status

| Item | State |
| --- | --- |
| Reads, keys, hooks, direction phrases, graph/tree builders, badges | ✅ Implemented (build and lint pass; accepted as verified by the project owner, 2026-11-03) |

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-11-03 | agent | Implemented the Module 08 read slice. |
