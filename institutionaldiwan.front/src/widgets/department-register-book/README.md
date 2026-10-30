# institutionaldiwan.front / src/widgets/department-register-book — Department Register Book

> **Status:** ✅ Implemented (Module 04 Phase 3) and verified in-browser on `/numbering?tab=register`.

## Purpose

The read-only book of numbers departments wrote in their own registers, paired with the central
Diwan number for the same document. Wraps `GET /api/departments/{id}/register` in the shared
`DataTable` (client mode) so the grid behaves like every other grid: URL state, column chooser,
context menus, and loading/empty/error states.

## Layer & Import Rules (FSD Strict Hierarchy)

- Allowed: `@/entities/*`, `@/shared/*` (public barrels only).
- Forbidden: `@/app/*`, `@/pages/*`, sibling widgets, `@/features/*` (read-only slice).
- Export only the root widget via `index.ts`.

## Contents

| File | Purpose |
| --- | --- |
| `ui/DepartmentRegisterBook.tsx` | Client-mode `DataTable` (`tableId="department-register"`) over `useDepartmentRegister`; department select (inactive departments are listed with «موقوف») and numbering-year filter through `useTableState({ filterKeys: ['departmentId', 'year'] })`; row menu (open document, copy central number / department number / document id); refresh menu and empty/loading/error states. |
| `index.ts` | Public barrel exporting `DepartmentRegisterBook`. |

## Behaviour

- A department must be selected before the API is called; without one the grid shows «اختر دائرة
  لعرض سجلها» and the rows stay empty.
- The year filter is optional: blank means every numbering year; a four-digit value narrows the call
  to `?numberingYear=`.
- Inactive departments stay selectable because they can still hold historical numbers.
- The document column and the row menu both open `/documents/{id}?tab=numbers`.
- Search covers the document title, the central number, the department number, and the year; sorting
  defaults to the registration date, newest first.

## Implementation status

| Item | State |
| --- | --- |
| Grid, filters, menus | ✅ Implemented (Module 04 Phase 3) |
| Browser verification | ✅ No-selection state, department + year in the URL across reloads, inactive department with historical rows, search/sort/column chooser, row menu |

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-10-30 | agent | Module 04 Phase 5: verified the inactive-department path with a permanent fixture (`VER-04-P5`, kept deactivated) that retains its historical `VER-2026-001` line; no code change. |
| 2026-10-29 | agent | Module 04 Phase 3: replaced the bespoke register table with a shared `DataTable` widget — URL-backed department/year filters, sorting, search, column chooser, row/header menus, and department options that include inactive departments; verified in-browser. |
