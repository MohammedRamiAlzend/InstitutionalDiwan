# institutionaldiwan.front / src/entities/department — Organizational Department Entity Slice
> **Status:** ✅ Reads implemented (2026-10-27) — list, tree, detail queries, the read-only `DepartmentBadge`, and (2026-10-28) the `toMermaid` `flowchart TD` builder; an employee-augmented `toTree` builder remains pending.
## Purpose
Models Module 01 `Department` (`DepartmentResponse` and recursive `DepartmentTreeNodeResponse`). Owns the `GET /api/departments`, `GET /api/departments/{id}`, and `GET /api/departments/tree` reads, the minimal `GET /api/identity-options/departments` picker read, and a read-only department badge.
## Contents
| File | Purpose |
| --- | --- |
| `model/types.ts` | `DepartmentResponse` and `DepartmentTreeNodeResponse` contracts (camelCase JSON, verified against the live API). |
| `model/queries.ts` | `departmentKeys` factory and `useDepartments`, `useDepartmentTree`, `useDepartmentById` (detail hook stays disabled without an id). |
| `model/options.ts` | `useDepartmentOptions` → minimal `{ departmentId, code, nameAr, isActive }` rows for administrative forms (user create/update, employee form, user access) and the Module 03 document filters, accepted by `USER_MANAGE` **or** `EMPLOYEE_MANAGE` **or** `DEPARTMENT_MANAGE` **or** `DOCUMENT_VIEW` **or** `DOCUMENT_CREATE` **or** `DOCUMENT_EDIT`. |
| `api/departmentApi.ts` | Typed `GET` calls for `/api/departments`, `/api/departments/tree`, and `/api/departments/{id}`. |
| `ui/DepartmentBadge.tsx` | Renders `nameAr` plus the LTR `code`, with a «موقوف» badge when `isActive` is false. |
| `index.ts` | Public barrel exporting types, query keys, hooks, the Mermaid builder, and the badge. |
| `model/toTree.ts` | **Pending** — pure builder turning departments (plus attached employees) into a typed tree for `widgets/department-hierarchy-tree`. |
| `model/toMermaid.ts` | Pure builder returning `{ source, departmentIdByNodeId }`: the hierarchy as Mermaid `flowchart TD` source (sequential `d1…` ids, sanitized quoted Arabic labels, `(موقوف)` suffix for inactive departments, empty source when the tree is empty) plus the node-id → department-id map the widget uses to open a department from its diagram node. |
## Implementation status
| Item | State |
| --- | --- |
| `model/types.ts`, `model/queries.ts`, `model/options.ts`, `api/`, `ui/DepartmentBadge.tsx`, `index.ts` | ✅ Implemented |
| `model/toMermaid.ts` | ✅ Implemented (Module 01 Phase 1) |
| `model/toTree.ts` | ❌ Pending employee augmentation |
## Change Log
| Date | Author | Change |
| --- | --- | --- |
| 2026-10-28 | agent | Module 03 Phase 2: the department picker read is also accepted for `DOCUMENT_VIEW`/`DOCUMENT_CREATE`/`DOCUMENT_EDIT` (server + tests), so read-only registry users can resolve owning-department labels and drive the registry filter. |
| 2026-10-28 | agent | Added `model/options.ts` (`useDepartmentOptions`) for the permission-scoped `GET /api/identity-options/departments` picker read used by identity and employee forms. |
| 2026-10-28 | agent | `departmentTreeToMermaid` now returns `{ source, departmentIdByNodeId }` so the hierarchy widget can resolve a clicked diagram node back to its department and open the detail dialog. |
| 2026-10-28 | agent | Added `model/toMermaid.ts` (`departmentTreeToMermaid`) with sanitized quoted labels and sequential node ids for `shared/ui/mermaid-view`; exported it through the barrel for `widgets/department-hierarchy-tree`. |
| 2026-10-27 | agent | Implemented Phase 2 department reads (list/tree/detail) and the department badge; live API shapes verified. |
| 2026-10-25 | Buffy | Added the pure `toTree` / `toMermaid` builders to the slice contents so the tree widget can render departments and employees as a tree, Mermaid diagram, or chart. |
| 2026-10-22 | opencode | Converted this folder's `README.md` from the Word document it was authored in to markdown, so the `README.md`-per-folder rule in `AGENT.md` holds and the content is diffable and agent-editable. |
| 2026-10-19 | agent | Initialized `src/entities/department` slice folder and README. |
