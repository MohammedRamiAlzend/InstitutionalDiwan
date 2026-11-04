# `src/widgets/document-relations-panel` — Relations Panel

> **Status:** Implemented — the «الروابط» tab body of the document page (Module 08).

## Purpose

Shows a document's links as a grid, a Mermaid diagram or a tree, with the permanent history underneath. View, depth,
filters, search, sort and paging live in the URL beside `?tab=relations`.

## Contents

| File | Purpose |
| --- | --- |
| `ui/DocumentRelationsPanel.tsx` | Orchestrator: view switch (جدول / مخطط / شجرة), hidden-links notice, history section and the add/remove dialogs. |
| `ui/RelationsGrid.tsx` | Client-mode grid (`tableId="document-relations"`): type and direction filters, search, sort, pager, column chooser, row menu (open, copy number, remove). |
| `ui/RelationsGraphView.tsx` | Depth selector plus the lazy Mermaid diagram (node click opens the document) or the accessible tree. |
| `ui/RelationEventsSection.tsx` | Paged relation history, newest first, with the URL-held `eventsPage`. |
| `index.ts` | Public barrel. |

## Implementation status

| Item | State |
| --- | --- |
| Grid, graph/tree, history, hidden notice, dialogs | ✅ Implemented (build and lint pass; accepted as verified by the project owner, 2026-11-03) |

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-11-04 | Claude | User, department and employee references now render as «name + short id» (`UserLabel` / `DepartmentLabel` / `EmployeeLabel`) instead of a bare id. |
| 2026-11-03 | agent | Implemented the Module 08 relations panel. |
