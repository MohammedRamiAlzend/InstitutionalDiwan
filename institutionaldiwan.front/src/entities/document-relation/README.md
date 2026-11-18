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
| `model/direction.ts` | Maps a link's type + server direction to a catalog `LabelKey` (`relationPhraseKey`); `relationPhrase` resolves it via `t()` for non-reactive callers (sort, print). `RELATION_DIRECTION_LABELS` is a `LabelKey` map. |
| `model/graph.ts` | Turns the graph into Mermaid source (sequential node ids mapped back to document ids) and into an accessible tree. |
| `ui/RelationTypeBadge.tsx` | Reply-to / related / supersedes / duplicate-of badge with its Arabic label. |
| `ui/RelationDirectionLabel.tsx` | The link read from the viewed document, e.g. «هذه الوثيقة ردّ على»; resolves `relationPhraseKey` reactively with `useLabel`. |
| `index.ts` | Public barrel. |

## Notes

- Links to documents the caller cannot view are omitted by the server and only counted in `hiddenRelationCount`.
- Diagram labels strip characters that break Mermaid; node ids are `n1…nK` because GUIDs are not valid node ids.

## Implementation status

| Item | State |
| --- | --- |
| Reads, keys, hooks, direction phrases, graph/tree builders, badges | ✅ Implemented (build and lint pass; accepted as verified by the project owner, 2026-11-03) |
| Direction phrases/labels editable via the `relation.*` catalog | ✅ Implemented (build and lint pass, 2026-11-18) |

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-11-18 | Qoder | UI-labels edit mode: direction phrases and labels moved to the `relation.*` catalog (`relationPhraseKey` + `t()`-based `relationPhrase`, `RELATION_DIRECTION_LABELS` as `LabelKey`s); `RelationDirectionLabel` resolves reactively. |
| 2026-11-11 | Claude | Direction phrases and the badge variant for relation type 5 «موحَّد مع». |
| 2026-11-03 | agent | Implemented the Module 08 read slice. |
