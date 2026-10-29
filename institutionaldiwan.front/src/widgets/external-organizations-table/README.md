# `src/widgets/external-organizations-table` — External Correspondents Grid

> **Status:** Implemented and browser-verified.

## Purpose

Administration grid for the Module 03 external correspondent catalog, mounted by the
`organizations` tab of `pages/documents`. No dedicated route or navigation entry exists: the same
`DOCUMENT_VIEW`/`DOCUMENT_EDIT` permissions govern it.

Built on `shared/ui/data-table` with the stable table id `external-organizations-table`. The list
endpoint is server-paged, so search, paging, and page size travel in the URL through
`useTableState({ filterKeys: ['include'] })`; column visibility persists in `localStorage`, and each
row exposes a permission-filtered menu mirrored by an overflow trigger.

## Behaviour

- Rows show the code (LTR mono), the Arabic name with an optional English line, the organization
  type label, contact info, status («نشط»/«موقوف»), and created/updated timestamps (hidden by
  default).
- The status filter (`active` → «الفعّالة فقط», `all` → «الفعّالة والموقوفة») maps to the API's
  `includeInactive` flag; both search terms and the filter live in the URL (`?tab=organizations&…`).
- Write actions (تعديل البيانات، إيقاف/تفعيل الجهة، جهة خارجية جديدة) appear only with
  `DOCUMENT_EDIT`; copy-code and copy-id stay available to every reader.
- Status changes go through `ConfirmDialog` before calling
  `features/external-organization-manage`; success invalidates the query prefix, so the row and the
  participant picker both refresh.
- The API list accepts no sort parameters, so the grid keeps the server's code ordering and its
  headers are not sortable.
- Empty, filtered-empty, and error states offer «مسح البحث والمرشحات» or «إضافة جهة» as appropriate.

## Composes

- `entities/external-organization`
- `entities/lookup` (`organization-types` labels)
- `entities/user` (`hasPermission`)
- `features/external-organization-manage`

## Implementation status

| Item | State |
| --- | --- |
| Docs | ✅ |
| Code | ✅ |
| Browser-verified | ✅ (server search/paging, `include=all` filter, create/edit, deactivate/activate badges, copy menus) |

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-10-29 | agent | Implemented the grid: URL search/paging over the paged API, active/all filter, lookup-labelled type, status badges, permission-filtered row/header/empty menus, and the create/edit/status dialogs; documented the missing server sort parameters. |
| 2026-10-25 | Cursor | Documented the external-organizations table for the Module 03 admin tab. |
