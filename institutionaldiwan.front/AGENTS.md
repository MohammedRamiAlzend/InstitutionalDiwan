# AGENTS.md — Rules for AI Agents Working in `institutionaldiwan.front`

> This file is the agent rulebook for this standalone front-end project (ported from the previous
> client project). Read it **before** changing anything under
> `institutionaldiwan.front/`. Keep [`knowledge.md`](knowledge.md) in sync with the external API
> whenever Modules or permissions change.

## Standalone boundary

`institutionaldiwan.front` is a separate Vite project and is not part of the
`InstitutionalDiwan.slnx` solution. There is no Visual Studio `.esproj`, no launch profile, and no
build-time dependency on a sibling server folder. The ASP.NET Core API is an external service:
configure its origin through Vite's dev proxy or `VITE_API_BASE_URL`, never by hardcoding it.

> **Current as-is:** `vite.config.ts` is the stock template — the `@` → `src` alias, the `/api` and
> `/hubs` proxy, and the HTTPS dev certificate described below are **not configured yet**. Add the
> alias before creating the first slice.

## The one rule everything else follows

**Code and docs change together.** Whenever a folder's contents change, that folder's `README.md`
must be updated in the same turn — the `Contents` table, the `Implementation status` table, and a
new `Change Log` row. A change is not "done" until both happened.

## Non-negotiable: Arabic/RTL first, light + dark themes

**Every component must be built Arabic-first (`lang="ar"`, `dir="rtl"`) and must also work in LTR.**
There is no later "RTL pass": logical CSS, mirrored icons, menu/tooltip positioning, and
Arabic-capable typography are part of a component's definition of done.

The application ships **light and dark themes**, both driven by one token source:
`src/app/styles/tokens.css`. Components never contain hex colors; they consume semantic CSS
variables (`--color-bg`, `--color-surface`, `--color-text`, `--color-accent`, …). Changing a value
in that one file updates both themes everywhere.

- **Direction:** `<html lang="ar" dir="rtl">` by default. Use `dir="ltr"` islands only for codes,
official numbers, hashes, ISO dates, and Latin-only content. Never use physical CSS (`ml-*`, `mr-*`,
`left-*`, `right-*`, `text-left/right`); use `ms-*`, `me-*`, `start-*`, `end-*`, `text-start/end`.
- **Mirroring:** mirror directional icons (arrows, chevrons, back/forward, indent) in RTL. Never
mirror logos, numbers, or numeric chart axes.
- **Theme:** default to the OS (`prefers-color-scheme`); the user can choose light / dark / system.
The choice persists in `localStorage` under `diwan.theme` and is applied as `data-theme` on `<html>`
before first paint (small inline boot script in `index.html`, no white flash).
- **Verification:** every table, tree, menu, drawer, form, and Mermaid surface is checked in both
RTL and LTR, and in both light and dark. Contrast must meet WCAG AA for body text, table text, and
menu items; focus rings stay visible on every surface.
- **Charts/diagrams:** Mermaid keeps its geometric layout LTR while labels may be Arabic; pass the
current theme's tokens to Mermaid (`themeVariables`) and re-render on theme change. Native trees
follow RTL ordering.

### Brand palette (single source: `src/app/styles/tokens.css`)

| Family | Shades |
| --- | --- |
| Forest | `#428177` · `#054239` · `#002623` |
| Golden Wheat | `#edebe0` · `#b9a779` · `#988561` |
| Charcoal | `#ffffff` · `#3d3a3b` · `#161616` |

These nine colors are the only brand colors. Functional status colors (`danger`, `success`,
`warning`) live in the same token file as semantic variables and must never be hardcoded in
components. The exact light/dark mapping is in
[`src/app/styles/README.md`](src/app/styles/README.md#design-tokens-contract).

## Feature-Sliced Design (FSD v2.1) is not negotiable

The six layers live in `src/` and imports may only ever flow **downward**:

```
app → pages → widgets → features → entities → shared
```

- Never import upward (`entities` may not import from `features`).
- Never import across sibling slices on the same layer (`entities/document` may not import from
  `entities/department`).
- Every slice and every `shared` segment exposes its public interface through its own
  `index.ts` barrel. Deep imports such as `@/entities/document/ui/DocumentStatusBadge` are
  forbidden — import `@/entities/document` instead.
- `shared` imports nothing from the layers above it.

## Reads live in `entities`, writes live in `features`

| Kind | Belongs to | Files |
| --- | --- | --- |
| `GET` request + `useQuery` + query-key factory | `src/entities/<slice>/` | `api/`, `model/queries.ts` |
| Read-only presentation of a domain object | `src/entities/<slice>/ui/` | badges, cards |
| `POST`/`PUT`/`DELETE` request + `useMutation` + Zod schema | `src/features/<slice>/` | `api/`, `model/*.schema.ts` |
| Interactive UI for a write | `src/features/<slice>/ui/` | forms, dialogs |

A feature that mutates must invalidate the entity query keys it affects — use the entity's query
key factory, never a hand-written string.

## Conventions

- **Workspace config is not installed yet.** The stock `vite.config.ts` has no `@` alias, proxy, or
  HTTPS certificate, and there is no `.env` file. Add them (see [`DEPLOYMENT.md`](DEPLOYMENT.md))
  before writing imports that rely on `@/...` or calling the API.
- **Arabic first.** The document root is `lang="ar" dir="rtl"`. Latin-only content (codes, ids,
official numbers, dates, hash hex) is rendered `dir="ltr"` inside an RTL page. Use logical CSS
properties (`ps-*`/`pe-*`, `ms-*`/`me-*`) so a single stylesheet serves both directions. The app also
ships light and dark themes from one token file — see the non-negotiable section above.
- **One slice per route.** `pages/<slice>/` holds exactly one route page; keep it under ~120
  lines and push composite sections into `widgets/` and interactive forms into `features/`.
- **URL-driven filters.** Table filters, search terms, sorting, and pagination belong in URL search
  params (`useSearchParams`), not local `useState`, so views are bookmarkable and shareable. Column
  visibility is the one exception — it is a per-browser preference stored in `localStorage`
  (see [Desktop-grade interaction contract](#desktop-grade-interaction-contract)).
- **Server state is React Query's job.** Do not copy server data into `useState`; use the entity
  hooks. Local `useState` is for view state only.
- **Errors come from the API, but the UI is Arabic-only.** The backend answers RFC 7807
  `ProblemDetails`; branch on `error.code` (the domain error code), never on the HTTP status
  alone, and render `toArabicErrorMessage(error)` from `shared/api/errorMessages.ts` — never the
  server's English `detail`. Extend that catalog with each module's codes as slices land. Exception: `POST /api/audit-logs/integrity-checks` returns **200** with
  `isValid: false` when the chain is broken — read the body, do not treat it as a transport error.
- **Permissions are exact strings.** The codes in `src/shared/config/permissions.ts` must match
  the API's `InstitutionalDiwan.Domain/Common/Permissions.cs` exactly (the API is a separate
  project). There are **16** of them:
  `DOCUMENT_VIEW`, `DOCUMENT_CREATE`, `DOCUMENT_EDIT`, `DOCUMENT_DELETE`, `DOCUMENT_APPROVE`,
  `NUMBERING_VIEW`, `NUMBERING_MANAGE`, `DEPARTMENT_MANAGE`, `ROLE_MANAGE`, `USER_MANAGE`,
  `EMPLOYEE_MANAGE`, `ACCESS_GRANT_MANAGE`, `REPORT_VIEW`, `LOOKUP_MANAGE`,
  `AUDIT_VIEW`, `AUDIT_MANAGE`.
- **Do not invent backend routes.** Live vs pending modules are listed in `knowledge.md`. Never call
  Modules 07 / 08 / 10 until those APIs exist.
- **No deep imports, no `any`.** `npm run build` runs `tsc -b` — it must pass with no errors,
  and `npm run lint` must stay clean.

## Desktop-grade interaction contract

The front end is a **desktop-style institutional workspace**, not a marketing dashboard. The three
behaviours below are mandatory on every data surface; a screen that lacks them is not done.

### 1. Right-click context menus everywhere

- One shared primitive in `src/shared/ui/context-menu` (built on
  `@radix-ui/react-context-menu`). Never hand-roll a menu inside a feature.
- Every interactive data surface supplies its **own menu definition** to that primitive: table row,
  table header, empty table area, tree node, card, tab, attachment row, timeline step, and the
  app-shell chrome. The widget/feature owns the items; the primitive only renders and positions.
- Menu items are **permission-filtered** with the same codes as route guards (`DOCUMENT_EDIT`,
  `AUDIT_MANAGE`, …). Never render an item the caller cannot execute; render it disabled with a
  reason only when that aids discovery.
- Every context-menu action is also keyboard reachable: `Shift+F10` or the `ContextMenu` key opens
  the menu on the focused element, arrow keys move, `Enter` activates, `Esc` closes and returns
  focus to the trigger.
- Destructive entries sit in their own group, use the danger token, and require confirmation. Show
  shortcut hints where a shortcut exists.
- Menus open at the pointer in RTL and LTR alike; labels stay start-aligned and inherit `dir`.
- The app shell has a menu on empty chrome (refresh data, toggle density, open settings). Tables
  have row, header, and empty-area menus.

### 2. Every table is a full data grid

Mandatory for **all** tables, including bounded-list endpoints that return a plain array:

| Requirement | Contract |
| --- | --- |
| Filtering | A filter bar on every table; filter values live in URL search params, never `useState`. |
| Search | Free-text search in the URL; debounce the input and reset `page` when it changes. |
| Pagination | Paged endpoints send `page`/`pageSize`; plain arrays paginate client-side with the same pager UI. |
| Sorting | Sort key and direction in the URL; server-side where the endpoint supports it. |
| Column chooser | Mandatory. Each table declares its default columns in code; the user toggles visibility and can reset to defaults. At least one identifying column can never be hidden. |
| Preference storage | Visibility persists **per table, per browser** in `localStorage` under `diwan.table.<tableId>.columns.v1` (versioned; parse defensively and fall back to defaults). It is view state — never server state, React Query, or URL state. |
| Stable table id | Use a stable id (`documents`, `audit-logs`, `movements-inbox`, …) so saved preferences survive refactors. |
| Row actions | Available through the row context menu **and** an always-visible overflow trigger for touch and keyboard users. |

### 3. Mermaid-driven visualization

- Render Mermaid through one lazy-loaded `shared/ui/mermaid-view` component built on the `mermaid`
  package (`securityLevel: 'strict'`, HTML/script labels rejected, external links disabled by
  default, input size capped).
- A visualization surface offers a **view switcher**: rendered Mermaid diagram, chart (pie, gantt,
  sequence, timeline, …), and **tree view** for hierarchies. Default to the diagram type declared
  by the source (`flowchart`, `mindmap`, `timeline`, …).
- Tree view is the canonical hierarchy presentation (departments → employees, document → movement
  chain, role → permission). Build it from API data and allow exporting to Mermaid
  (`flowchart TD`) so the same data can be shown as a Mermaid diagram.
- Data → Mermaid and data → tree builders live in the owning entity's `model/` (for example
  `entities/department/model/toMermaid.ts`); rendering and interaction primitives live in
  `shared/ui`; the composing surface is a widget.
- Invalid Mermaid renders an Arabic-first error state with the parser message, never a blank area.
- Tree and diagram nodes expose their own context menus (expand/collapse, copy id, open record)
  through the same shared primitive.

## First-slice file contracts (copy shapes, then implement)

When creating the first real TypeScript files, match these public shapes so agents stay consistent.

### `src/shared/config/permissions.ts`

```ts
export const Permissions = {
  DocumentView: 'DOCUMENT_VIEW',
  DocumentCreate: 'DOCUMENT_CREATE',
  DocumentEdit: 'DOCUMENT_EDIT',
  DocumentDelete: 'DOCUMENT_DELETE',
  DocumentApprove: 'DOCUMENT_APPROVE',
  NumberingView: 'NUMBERING_VIEW',
  NumberingManage: 'NUMBERING_MANAGE',
  DepartmentManage: 'DEPARTMENT_MANAGE',
  RoleManage: 'ROLE_MANAGE',
  UserManage: 'USER_MANAGE',
  EmployeeManage: 'EMPLOYEE_MANAGE',
  AccessGrantManage: 'ACCESS_GRANT_MANAGE',
  ReportView: 'REPORT_VIEW',
  LookupManage: 'LOOKUP_MANAGE',
  AuditView: 'AUDIT_VIEW',
  AuditManage: 'AUDIT_MANAGE',
} as const;

export type PermissionCode = (typeof Permissions)[keyof typeof Permissions];

export const AllPermissions: readonly PermissionCode[] = Object.values(Permissions);
```

### `src/shared/api` — `ApiError` + query-key factory pattern

```ts
export class ApiError extends Error {
  constructor(
    public readonly code: string,   // ProblemDetails.title
    public readonly detail: string, // ProblemDetails.detail
    public readonly status: number,
  ) {
    super(detail);
  }
}

// entities/document/model/queries.ts
export const documentKeys = {
  all: ['documents'] as const,
  list: (params: object) => [...documentKeys.all, 'list', params] as const,
  detail: (id: string) => [...documentKeys.all, 'detail', id] as const,
};
```

### Entity barrel

Every new entity/feature slice ships `index.ts` that re-exports only the public hooks/components —
never deep paths from outside the slice.

## UI / state contract (every interactive screen)

| State | Required behavior |
| --- | --- |
| Loading | Region skeleton / spinner; never a blank content area |
| Empty | Arabic-first empty copy; optional primary CTA |
| Error | Show `ApiError.detail`; offer retry when idempotent |
| Forbidden | Dedicated “no permission” state for missing route policy |
| Success mutation | Toast (`sonner`) + invalidate the owning entity query keys |

## Before you finish

- [ ] `npm run build` passes (`tsc -b && vite build`).
- [ ] `npm run lint` passes.
- [ ] Every touched folder's `README.md` reflects the new `Contents`.
- [ ] A `Change Log` row was appended to each touched folder's `README.md`.
- [ ] The new public exports are reachable through the slice's `index.ts` barrel.
- [ ] If endpoints or permissions changed, `knowledge.md` was updated in the same turn.
- [ ] The component is verified in Arabic RTL **and** LTR, and in light **and** dark themes; no
      physical CSS and no hardcoded hex colors.
- [ ] New tables ship filters, pagination, sorting, a column chooser persisted in `localStorage`,
      and row/header context menus.
- [ ] Context-menu actions are permission-filtered and keyboard reachable (`Shift+F10`).
- [ ] Mermaid/visualization surfaces are lazy-loaded and handle invalid syntax.

## Where to look

- `knowledge.md` — as-is/to-be, live endpoint list (incl. Attachments + Audit), lookup slugs, errors.
- `README.md` — FSD map, package install commands, UI/design contract, implementation status.
- `src/<layer>/README.md` — rules and planned slices for that layer.
