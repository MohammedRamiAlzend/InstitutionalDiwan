# institutionaldiwan.client / src/entities/department — Organizational Department Entity Slice
> **Status:** Scaffolding & Rules — Entity read models, tree query hooks, and node card defined; code pending.
## Purpose
Models Module 01 `Department` (`DepartmentResponse` and recursive `DepartmentTreeNodeResponse`). Owns `GET /api/departments`, `GET /api/departments/{id}`, and `GET /api/departments/tree` read queries and read-only department display badges/cards.
## Contents
| File | Purpose |
| --- | --- |
| `model/types.ts` | `DepartmentResponse` and `DepartmentTreeNodeResponse` TypeScript contracts. |
| `model/queries.ts` | `departmentKeys` factory and `useDepartments`, `useDepartmentById`, `useDepartmentTree` hooks. |
| `api/departmentApi.ts` | Typed `GET` functions for `/api/departments` and `/api/departments/tree`. |
| `ui/DepartmentBadge.tsx` | Displays department `nameAr` and `code` (`dir="ltr"`) with active/inactive indicator. |
| `index.ts` | Public barrel exporting types, query keys, hooks, and UI components. |
## Implementation status
| Item | State |
| --- | --- |
| `model/`, `api/`, `ui/`, `index.ts` | ❌ Not implemented |
## Change Log
| Date | Author | Change |
| --- | --- | --- |
| 2026-10-22 | opencode | Converted this folder's `README.md` from the Word document it was authored in to markdown, so the `README.md`-per-folder rule in `AGENT.md` holds and the content is diffable and agent-editable. |
| 2026-10-19 | agent | Initialized `src/entities/department` slice folder and README. |
