# institutionaldiwan.front / src/entities/department — Organizational Department Entity Slice
> **Status:** ✅ Reads implemented (2026-10-27) — list, tree, detail queries and `DepartmentBadge`; `toTree`/`toMermaid` builders remain pending with the hierarchy widget.
## Purpose
Models Module 01 `Department` (`DepartmentResponse` and recursive `DepartmentTreeNodeResponse`). Owns the `GET /api/departments`, `GET /api/departments/{id}`, and `GET /api/departments/tree` reads plus a read-only department badge.
## Contents
| File | Purpose |
| --- | --- |
| `model/types.ts` | `DepartmentResponse` and `DepartmentTreeNodeResponse` contracts (camelCase JSON, verified against the live API). |
| `model/queries.ts` | `departmentKeys` factory and `useDepartments`, `useDepartmentTree`, `useDepartmentById` (detail hook stays disabled without an id). |
| `api/departmentApi.ts` | Typed `GET` calls for `/api/departments`, `/api/departments/tree`, and `/api/departments/{id}`. |
| `ui/DepartmentBadge.tsx` | Renders `nameAr` plus the LTR `code`, with a «موقوف» badge when `isActive` is false. |
| `index.ts` | Public barrel exporting types, query keys, hooks, and the badge. |
| `model/toTree.ts` | **Pending** — pure builder turning departments (plus attached employees) into a typed tree for `widgets/department-hierarchy-tree`. |
| `model/toMermaid.ts` | **Pending** — pure builder exporting the hierarchy as Mermaid `flowchart TD` / `mindmap` source. |
## Implementation status
| Item | State |
| --- | --- |
| `model/types.ts`, `model/queries.ts`, `api/`, `ui/DepartmentBadge.tsx`, `index.ts` | ✅ Implemented |
| `model/toTree.ts`, `model/toMermaid.ts` | ❌ Pending the hierarchy widget |
## Change Log
| Date | Author | Change |
| --- | --- | --- |
| 2026-10-27 | agent | Implemented Phase 2 department reads (list/tree/detail) and the department badge; live API shapes verified. |
| 2026-10-25 | Buffy | Added the pure `toTree` / `toMermaid` builders to the slice contents so the tree widget can render departments and employees as a tree, Mermaid diagram, or chart. |
| 2026-10-22 | opencode | Converted this folder's `README.md` from the Word document it was authored in to markdown, so the `README.md`-per-folder rule in `AGENT.md` holds and the content is diffable and agent-editable. |
| 2026-10-19 | agent | Initialized `src/entities/department` slice folder and README. |
