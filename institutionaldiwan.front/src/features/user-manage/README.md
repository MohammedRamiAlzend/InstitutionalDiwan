# `src/features/user-manage` — User Account & Department Assignment Feature Slice
## 1. Purpose
Handles creating institutional users, assigning users to departments and roles, and toggling account active status (`POST /api/users`, `PUT /api/users/{id}`).
## 2. Strict FSD Rules
- Allowed Imports: `src/entities/user`, `src/entities/department`, `src/entities/role`, and `src/shared/*`.
- Forbidden Imports: NEVER import from `src/app`, `src/pages`, `src/widgets`, or sibling `src/features/*` slices.
- Public API: Export `UserFormDialog`, `AssignUserRolesModal`, and user mutations via `src/features/user-manage/index.ts`.
## 3. Planned Contents & Segments
- `ui/UserFormDialog.tsx` — Form for user creation and department/role assignment.
- `model/user-form.schema.ts` — Zod validation schema matching backend DTOs.
- `api/use-user-mutations.ts` — Mutations for creating/updating users and invalidating `userKeys.all`.
- `index.ts` — Public API barrel file.
## 4. Required Packages
- `@tanstack/react-query`, `react-hook-form`, `zod`, `@hookform/resolvers`, `sonner`
## 5. Implementation Status
- Slice folder and FSD rules established
- Implement `UserFormDialog` and user mutations
## 6. Change Log
- 2026-10-22: Converted this `README.md` from the Word document it was authored in to markdown, so the `README.md`-per-folder rule in `AGENT.md` holds and the content is diffable and agent-editable.
- 2026-10-19: Created `features/user-manage` slice folder and `README.md`.
