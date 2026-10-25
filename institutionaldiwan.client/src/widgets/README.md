# institutionaldiwan.client / src/widgets — Composite UI Widgets Layer

> **Status:** Scaffolding & Rules — FSD widget slices and composition rules defined; code pending.

## Purpose

`src/widgets/` contains self-contained page blocks that combine `entities` (reads) with `features`
(writes) into reusable UI regions — app shell, registry tables, inbox, attachments panel, audit
browser.

## Layer & Import Rules (FSD Strict Hierarchy)

- Allowed: `@/features/*`, `@/entities/*`, `@/shared/*` (public barrels).
- Forbidden: `@/app/*`, `@/pages/*`, sibling widgets.
- Export only the root widget via `index.ts`.

## Planned Slices & Contents

| Slice Folder | Purpose | Composes | State |
| --- | --- | --- | --- |
| `app-shell/` | RTL layout: permission-filtered sidebar, top bar, logout | `entities/user` + `features/auth` | ❌ Planned |
| `document-registry-table/` | Correspondence grid + actions | `entities/document` + `features/document-create` + access-grant | ❌ Planned |
| `document-attachments-panel/` | File list, upload, download, verify checksum, archive | `entities/document-file` + `features/attachment-upload` | ❌ Planned (API live) |
| `department-hierarchy-tree/` | Org tree with node context menus, inline manage actions, and a tree / Mermaid / chart switcher | `entities/department` + `features/department-manage` | ❌ Planned |
| `movement-inbox/` | Inbox/outbox table with routing action menu | `entities/movement` + `features/movement-routing` | ❌ Planned (API live) |
| `movement-timeline/` | Vertical step chain for one document | `entities/movement` + `features/movement-routing` | ❌ Planned |
| `audit-log-table/` | Filterable audit grid + hash columns + integrity toolbar | `entities/audit-log` + `features/audit-manage` | ❌ Planned (API live) |
| `visualization-panel/` | Mermaid source viewer plus data-derived diagram / chart / tree switcher with node context menus | `entities/department`, `entities/movement` + `shared/ui/mermaid-view` | ❌ Planned |
| `notification-center/` | SignalR bell/drawer | Deferred until Module 10 + hubs | ❌ Blocked |

## Conventions

- No `useMutation` inside widgets — mount feature UI triggers instead.
- Logical Tailwind props only (`ms-*` / `me-*` / `ps-*` / `pe-*`).
- Every table widget uses `shared/ui/data-table`: URL-driven filters/search/sort/pagination and a
  column chooser whose visibility is saved per table in `localStorage` (`diwan.table.<tableId>.columns.v1`).
  No bespoke `<table>` markup.
- Every row, header, empty area, tree node, and visualization node exposes a permission-filtered
  context menu through `shared/ui/context-menu`, mirrored by an always-visible overflow trigger.
- Visualization widgets compose `shared/ui/mermaid-view`; data → Mermaid/tree builders stay in the
  owning entity's `model/`.

## Implementation status

| Widget Slice | State |
| --- | --- |
| `app-shell/`, `document-registry-table/`, `department-hierarchy-tree/` | ❌ Not implemented |
| `document-attachments-panel/`, `movement-inbox/`, `movement-timeline/`, `audit-log-table/` | ❌ Not implemented (backend live) |
| `visualization-panel/` | ❌ Not implemented (depends on `shared/ui/mermaid-view`) |
| `notification-center/` | ❌ Blocked on Module 10 / SignalR |

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-10-25 | Buffy | Documented the mandatory table/context-menu conventions and added the `visualization-panel` widget (Mermaid + data-derived tree/chart views); upgraded the department tree with node menus and a view switcher. |
| 2026-10-25 | Cursor | Added attachments panel, movement inbox, and audit-log table widgets; marked notification-center blocked. |
| 2026-10-22 | opencode | Converted this folder's `README.md` from the Word document it was authored in to markdown. |
| 2026-10-19 | agent | Initialized `src/widgets` FSD layer folder and documented widget slices, rules, and packages. |
