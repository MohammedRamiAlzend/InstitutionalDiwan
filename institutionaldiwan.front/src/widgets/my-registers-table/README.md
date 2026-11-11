# institutionaldiwan.front / src/widgets/my-registers-table — My Registers Grid

> **Status:** ✅ Implemented. Exercised in the browser (see the page README).

## Purpose

Shows one register of the caller's own department at a time. The department's register book
(`GET /api/departments/{id}/register`) is read once; the chosen register, the year, the number status
and a date range narrow it in the browser, and search, sort, paging, column chooser and export come
from the shared `DataTable` (client mode).

## Layer & Import Rules (FSD Strict Hierarchy)

- Allowed: `@/entities/numbering-registry`, `@/entities/document`, `@/entities/user`, `@/shared/*`
  (public barrels only).
- Forbidden: `@/app/*`, `@/pages/*`, sibling widgets, `@/features/*` (read-only slice).
- Export only the root widget via `index.ts`.

## Contents

| File | Purpose |
| --- | --- |
| `ui/MyRegistersTable.tsx` | Server-mode grid (`tableId="my-registers"`): register select (each option shows its line count, inactive registers are marked «موقوف»), year, status and date-range filters, columns, open/copy row menu, and empty/loading/error states. |
| `index.ts` | Public barrel exporting `MyRegistersTable`. |

## Behaviour

- The registers are the ones whose `departmentId` is the caller's primary department; the department
  comes from `useMyPrimaryDepartmentId`.
- Filters live in the URL (`registry`, `type`, `version`, `year`, `status`, `from`, `to`) together with the search, sort
  and page; changing the form type resets the version. A department with exactly one register opens it without a choice.
- Nothing is filtered in the browser: search, filters, sort and paging are sent to `GET .../register/entries`.
- The form type defaults to the register's document type when the department has a form for it; the
  version defaults to the newest. The chosen version's fields become columns (sortable by type) and the
  grid keeps only lines recorded on that version, showing the latest visit's values. «بلا بيانات نموذج»
  shows every line without form data. Field-value filters are not offered; the search covers text values.
- A line whose document the caller cannot view keeps its number but shows `RestrictedDocumentLabel`
  and no «فتح الوثيقة» entry, the same as the department register book.
- Search covers the register number, the document title, the central number and the form values.

## Implementation status

| Item | State |
| --- | --- |
| Grid, filters, search, sort, paging, column chooser, export | ✅ Implemented |
| Server-side paging | ❌ Not needed yet: the book is read whole, as in the register book |

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-11-11 | Claude | Moved to server mode and added the form type/version selects with field columns. |
| 2026-11-11 | Claude | Added the widget behind `/my-registers`. |
