# `src/features/department-manage` — Department CRUD & Hierarchy Management Feature Slice
## 1. Purpose
Provides forms, dialogs, and mutations for creating, updating, and toggling organizational departments (`POST /api/departments`, `PUT /api/departments/{id}`) within the institutional hierarchy.
## 2. Strict FSD Rules
- Allowed Imports: `src/entities/department`, `src/entities/lookup`, and `src/shared/*`.
- Forbidden Imports: NEVER import from `src/app`, `src/pages`, `src/widgets`, or sibling `src/features/*` slices.
- Public API: Export `DepartmentFormDialog` and department mutations via `src/features/department-manage/index.ts`.
## 3. Planned Contents & Segments
- `ui/DepartmentFormDialog.tsx` — Create/edit department dialog with parent department selector and numbering prefix input.
- `model/department.schema.ts` — Zod validation schema for `CreateDepartmentRequestDto` and `UpdateDepartmentRequestDto`.
- `api/use-department-mutations.ts` — TanStack Query mutations invalidating `departmentKeys.all`.
- `index.ts` — Public API barrel file.
## 4. Required Packages
- `@tanstack/react-query`, `react-hook-form`, `zod`, `@hookform/resolvers`, `sonner`
## 5. Implementation Status
- Slice folder and FSD rules established
- Implement `DepartmentFormDialog` and mutations
## 6. Change Log
- 2026-10-22: Converted this `README.md` from the Word document it was authored in to markdown, so the `README.md`-per-folder rule in `AGENT.md` holds and the content is diffable and agent-editable.
- 2026-10-19: Created `features/department-manage` slice folder and `README.md`.
