# `src/widgets/movement-inbox` — Inbox / Outbox Queue

> **Status:** Implemented and browser-verified.

## Purpose

Department inbox/outbox table with search, status/type filters, sorting, and a routing action menu.
Used by `pages/movements`.

Built on `shared/ui/data-table` with stable table ids `movements-inbox` / `movements-outbox`. The API
returns plain arrays, so search, filtering, sorting, and pagination are client-side but reuse the
same pager and page-size control; filters stay in URL search params, the column chooser persists
visibility in `localStorage`, and each row exposes a permission-filtered routing menu mirrored by an
overflow trigger.

## Behaviour

- Both queues come from `useMovementQueue(kind)` and therefore describe the caller's primary
  department; there is no department selector because the session does not expose a department scope
  and the API has no scoped queue parameter yet.
- The inbox offers every action `getMovementActions` allows for the caller's side of the current step.
  The outbox further keeps only `send`, `redirect`, `close` and `cancel`, because receiving belongs to
  the destination.
- A row whose document the caller cannot view has a blank title, number and instructions. It shows
  `RestrictedDocumentLabel` with no link, and its menu drops «فتح الوثيقة» but keeps the routing
  actions, because custody does not depend on document access.
- Filters are applied manually to the rows: `DataTable` in client mode only searches, sorts, and
  slices, so status and movement-type filters are pre-filtered in the widget.
- A due date in the past on a non-terminal movement is highlighted as «متأخر».
- Actions open `features/movement-routing` dialogs; a successful mutation invalidates the movement
  query prefix, so the row and the document timeline refresh together.

## Composes

- `entities/movement`
- `features/movement-routing`

## Implementation status

| Item | State |
| --- | --- |
| Docs | ✅ |
| Code | ✅ |
| Browser-verified | ✅ (inbox for both departments, row menus, filters/search, empty state, restricted rows and actions per side in light and dark) |

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-11-04 | Claude | User, department and employee references now render as «name + short id» (`UserLabel` / `DepartmentLabel` / `EmployeeLabel`) instead of a bare id. |
| 2026-11-03 | Claude | Filtered row actions by the caller's side of the current step and added the restricted label for rows the caller cannot view. |
| 2026-10-29 | Cursor | Implemented the queue table; documented the primary-department only scope and the manual client-side filtering. |
| 2026-10-25 | Buffy | Documented client-side pagination for the plain-array queues, stable table ids, `localStorage` column chooser, and row context menus. |
| 2026-10-25 | Cursor | Added movement-inbox widget docs for Module 05 queues. |
