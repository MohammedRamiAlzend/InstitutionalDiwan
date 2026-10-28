# `src/features/department-manage` — Department CRUD & Hierarchy Management Feature Slice

> **Status:** Implemented for Module 01 Organization; final integration verification is pending.

## 1. Purpose
Creates, edits, reparents, and activates/deactivates departments under `DEPARTMENT_MANAGE`.
`widgets/department-hierarchy-tree` owns action menus, dialog selection, and status confirmations;
this feature owns write requests and the create/edit form.

## 2. Contents
| File | Purpose |
| --- | --- |
| `ui/DepartmentFormDialog.tsx` | Create/edit dialog with detail loading, parent/type choices, edit-only manager picker, external-correspondence flags, notes, and Arabic feedback. |
| `model/types.ts` | Separate create/update request types; update includes parent and nullable manager but not immutable code/type. |
| `model/schemas.ts` | `departmentFormSchema` and form values with Arabic required-field and length validation. |
| `model/hierarchy.ts` | `collectDescendantIds` excludes the current department and its descendants from possible parents. |
| `api/departmentMutations.ts` | Create, update, and active-status mutations, invalidating `departmentKeys.all` after success. |
| `index.ts` | Public exports for the dialog/target, mutation hooks, and request types. |

## 3. Conventions / Notes
- Import only entity public barrels (`department`, `employee`, `lookup`, `user`) and `shared`.
  Never import upper layers or sibling features.
- Creation sends code, Arabic name, type, optional parent/English name/notes, and external send/receive
  flags. Assign the manager in a later edit; creation has no manager field or numbering-prefix input.
- Editing keeps code/type immutable and sends parent, names, manager, policies, and notes together
  through `PUT /api/departments/{id}`. The UI does not call the separate backend `PUT /{id}/parent` route.
  Null parent makes a root; null manager clears the assignment.
- Parent choices exclude self/descendants and disable inactive departments. The backend remains
  authoritative for cycle/parent validity and prevents reparenting the configured central Diwan
  under another department or deactivating it (`Department.CentralDiwanProtected`). Transfer the
  designation through institution settings first. Reactivation requires an active parent.
- The edit-only `EmployeePicker` uses paged/searchable reads. Without `EMPLOYEE_MANAGE`, it is disabled
  with explanatory text, performs no employee requests, and retains the current manager value.
- Detail loading uses skeletons; read failures offer retry and prevent edit submission. Supporting
  lookup/department failures and mutation errors render Arabic messages. Successful saves toast and close.
- `DELETE /api/departments/{id}` means deactivation, not deletion; active children also block it.

## 4. Required Packages
- `@tanstack/react-query`, `react-hook-form`, `zod`, `@hookform/resolvers`, `sonner`, shared UI primitives.

## 5. Implementation status
| Item | State |
| --- | --- |
| Create/edit/reparent form and query invalidation | Implemented. |
| Manager, notes, external send/receive policies | Implemented; manager assignment is edit-only and permission-gated. |
| Activate/deactivate mutations | Implemented; confirmations/menu orchestration belong to the hierarchy widget. |
| Backend central-hub and inactive-parent activation guards | Implemented; errors surface through the shared Arabic catalog. |
| Final browser/build/test verification | Pending main-agent verification. |

## 6. Change Log
- 2026-10-28: Document the implemented Module 01 form and mutations, immutable code/type, edit-only manager picker, parent/cycle safeguards, and central Diwan protection; replace planned filenames with actual contents.
- 2026-10-22: Converted this `README.md` from the Word document it was authored in to markdown, so the `README.md`-per-folder rule in `AGENT.md` holds and the content is diffable and agent-editable.
- 2026-10-19: Created `features/department-manage` slice folder and `README.md`.
