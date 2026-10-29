# institutionaldiwan.front — Institutional Diwan React 19 + TypeScript 6 + Vite 8 SPA (Feature-Sliced Design)

> **Status:** Module 01 Organization UI is implemented and verified: department create/edit/reparent,
> managers, notes and external-correspondence policy, activate/deactivate, tree/Mermaid/chart/grid
> views, optional lazy employee branches, paged/searchable rosters with details/menus, and the guarded
> `/institution-settings` form. Shared employee picking and numbering lookup catalogs support the forms.
> Module 02 Identity auth/admin screens are implemented and verified, including permission-scoped
> `identity-options` reads, temporal department assignments, server-enforced forced password change,
> and full query-cache teardown on login/logout. Module 03 Correspondence has started: Phase 0 added
> the document-facing picker reads on the API, Phase 1 implemented the `document` entity reads and the
> Arabic Correspondence/Numbering/Movements error catalog, Phase 2 shipped the `/documents` registry
> with document/draft creation plus its active sidebar entry, and Phase 3 added the document details
> page (`/documents/:documentId`) with participants, status history, the audit timeline, and the
> draft-edit/register/participant/cancel actions, and Phase 4 added the access-grants tab (grant a
> user a level with optional expiry, revoke while keeping the record) behind `ACCESS_GRANT_MANAGE`.
> Phase 5 added the `/numbering` registry administration page, the read-only department register book,
> and the document-details numbers tab (issue, department registration, replace with reason, cancel
> with reason, full number history) behind `NUMBERING_VIEW`/`NUMBERING_MANAGE`. Phase 6 added the
> movement workflow: `entities/movement`, `features/movement-routing`, `widgets/movement-inbox`, the
> `/movements` inbox/outbox page, and the document-details movements tab with the status/permission-
> filtered timeline, steps, and events. Phase 7 added the external-correspondent administration tab
> inside `/documents` (create/edit, staged deactivate/activate, duplicate-code Arabic error, and the
> participant-picker effect of deactivation) behind `DOCUMENT_VIEW` reads / `DOCUMENT_EDIT` writes.
> `npm run build` and `npm run lint` pass; the live API was used for login, session invalidation,
> department, settings, and picker checks.
> The Arabic-first RTL/light-dark foundation, `@` alias, dev proxy, and `.env.example` are configured;
> the optional HTTPS dev certificate remains to-be.
>
> **Standalone by design:** this project is intended to live outside the `InstitutionalDiwan.slnx`
> solution. The ASP.NET Core API is an external service; there is no Visual Studio `.esproj` and no
> build-time dependency on a sibling server folder.

## Purpose

The front-end Single-Page Application (SPA) for the Institutional Diwan (نظام الديوان المؤسساتي
الموحد) system. It provides an Arabic-first (`dir="rtl"`) and bilingual (`ltr`) institutional
interface consuming the ASP.NET Core REST API (`InstitutionalDiwan.Server`) as an external service
across the live modules, with SignalR hubs planned once the API exposes them.

The codebase strictly follows Feature-Sliced Design (FSD v2.1) so AI coding agents and human
developers can implement slices predictably without spaghetti imports.

## As-is vs to-be

| Item | As-is | To-be |
| --- | --- | --- |
| Runtime deps in `package.json` | React/shared/auth stack, Radix UI primitives, and lazy `mermaid` (see “Required Packages”) | Remaining packages as their slices land; shared grid owns its URL/localStorage state model |
| FSD folders | Six documented layers; shared/app foundation, Module 02 auth/admin slices, Module 01 department/hierarchy/settings slices, and the Module 03 `document`/`external-organization`/`numbering-registry`/`movement` entities + `document-create`/`document-manage`/`document-access-grant`/`numbering-manage`/`movement-routing`/`external-organization-manage` features + `document-registry-table`/`external-organizations-table`/`numbering-registry-table`/`movement-inbox` widgets + registry/details/numbering/movements pages implemented, including `entities/institution-setting`, `features/{department-manage,institution-settings-manage}`, and reusable `EmployeePicker` | Remaining module frontends |
| `vite.config.ts` | ✅ `@` → `./src` alias + env-driven `/api` + `/hubs` proxy | Optional local HTTPS certificate |
| Dev configuration | ✅ `.env.example` committed (`DEV_PROXY_TARGET`, `VITE_API_BASE_URL`); create local `.env` as needed | — |
| UI | Arabic-first login/password change/dashboard/Identity admin plus `/departments` management and tree/Mermaid/chart/grid, optional lazy employee branches and paged rosters, `/institution-settings`, the `/documents` registry (filters, search, sort, create draft/registered) with the external-organizations admin tab (create/edit/deactivate/activate), the document details page (participants, access grants, numbers, movements, status history, timeline, draft-edit/register/participant/cancel/number/routing lifecycle), `/numbering` (registry grid + department register book), and `/movements` (department inbox/outbox with routing menus); shared theme/density tokens | Remaining module pages (audit) |
| Backend coverage docs | `knowledge.md` describes the external API | Modules 01–06, 09 live; 07/08/10 deferred |

Always treat **as-is** as the truth for “what runs today,” and **to-be** as the install/implement
checklist before coding features.

## Required Packages & One-Command Installation

> The shared/auth and hierarchy packages are **installed** (React Router, TanStack Query, Zustand, Axios,
> react-hook-form, Zod, resolvers, lucide-react, clsx, tailwind-merge, CVA, sonner, Tailwind v4,
> devtools, `mermaid`) together with the Radix primitives used by the shared UI kit (`dialog`,
> `dropdown-menu`, `context-menu`, `select`, `checkbox`). The groups below remain the checklist for
> the remaining slices.

```bash
# 1. Core runtime packages (Routing, Server/Client State, HTTP, Forms, Validation, Icons, UI, SignalR, Mermaid)
npm install react-router-dom @tanstack/react-query @tanstack/react-table zustand axios react-hook-form zod @hookform/resolvers lucide-react clsx tailwind-merge class-variance-authority sonner @microsoft/signalr mermaid

# 2. Radix UI primitives (for RTL-ready shadcn/ui components in src/shared/ui)
npm install @radix-ui/react-dialog @radix-ui/react-dropdown-menu @radix-ui/react-context-menu @radix-ui/react-select @radix-ui/react-tabs @radix-ui/react-tooltip @radix-ui/react-label @radix-ui/react-slot @radix-ui/react-checkbox @radix-ui/react-popover

# 3. Tailwind CSS v4 + React Query Devtools
npm install -D tailwindcss @tailwindcss/vite @tanstack/react-query-devtools
```

### API Target & Vite Proxy

`vite.config.ts` ships the `@` → `./src` alias and an env-driven dev proxy for `/api` (HTTP) and
`/hubs` (WebSocket upgrade). The proxy target resolves in this order:

1. `DEV_PROXY_TARGET` from `.env` (see `.env.example`);
2. `ASPNETCORE_HTTPS_PORT` from the shell;
3. `https://localhost:7126` (the API's usual development origin).

The local HTTPS **dev certificate** is still to-be (optional; only needed for parity with a
deployed HTTPS origin during development).

**Changing the API host, publishing the SPA, and wiring a new endpoint end to end are covered in
[`DEPLOYMENT.md`](DEPLOYMENT.md).**

## Feature-Sliced Design (FSD) Layer Hierarchy

```text
src/
├── app/        # 1. Global providers, router, auth/permission guards, global styles
│   ├── providers/
│   ├── router/
│   └── styles/
├── pages/      # 2. Route pages (1 slice ≈ 1 route family)
├── widgets/    # 3. Composite blocks (app-shell, registry table, inbox, attachments panel, …)
├── features/   # 4. Write mutations + Zod forms mapped to backend Commands
├── entities/   # 5. Read models, useQuery hooks, read-only badges
└── shared/     # 6. api, ui, lib, config, types — imports nothing from upper layers
```

### Strict FSD Rules for AI Agents

- Unidirectional imports: `app → pages → widgets → features → entities → shared`.
- Public API barrel (`index.ts`) per slice; no deep imports.
- Reads (`GET` / `useQuery`) in `entities/`; writes (`useMutation`) in `features/`.
- Golden rule: update each touched folder's `README.md` in the same turn.

## UI / design contract

Institutional, Arabic-first, calm — not a generic SaaS dashboard skin.

| Token / concern | Contract |
| --- | --- |
| Direction | `html[lang="ar"][dir="rtl"]` by default; every component must also work in LTR; `dir="ltr"` islands for codes, official numbers, hashes, ISO dates |
| Typography | Prefer Arabic-capable faces (e.g. Cairo / Readex Pro / Noto Sans Arabic) via `app/styles`; avoid Inter/Roboto/Arial as the primary UI font |
| Color | One token file, `src/app/styles/tokens.css`: Forest `#428177` / `#054239` / `#002623`, Golden Wheat `#edebe0` / `#b9a779` / `#988561`, Charcoal `#ffffff` / `#3d3a3b` / `#161616`. Components consume semantic variables (`--color-bg`, `--color-surface`, `--color-text`, `--color-accent`, …) and never hardcode hex |
| Theme | Light + dark from the same tokens; default follows `prefers-color-scheme`, user picks light/dark/system, persisted as `diwan.theme` and applied via `data-theme` before first paint |
| Spacing | 4-px base scale; content max-width for reading panes; full-bleed only for the app shell chrome |
| Components | Prefer `shared/ui` primitives; cards only when they wrap an interactive unit |
| Motion | Subtle (150–250 ms) for drawer/dialog; no decorative loop animations |
| Screen states | Loading / empty / error / forbidden required (see `AGENTS.md` and `knowledge.md`) |
| Density | Tables are the primary correspondence surface; keep filter bars URL-driven and compact |
| Interaction | Desktop-style workspace: right-click context menus on every data surface, keyboard equivalents, and always-visible overflow triggers for touch |
| Tables | Every table ships filters, search, sorting, pagination, and a column chooser; the user's column visibility is saved per table in `localStorage` |
| Visualization | Mermaid renderer with a diagram / chart / tree view switcher; hierarchies also render as native trees built from API data |

Print: official receipts/register extracts use a dedicated `@media print` block in `app/styles`
(no sidebar, black text, LTR for numbers).

### Desktop-grade interaction contract

The full, binding rules live in [`AGENTS.md`](AGENTS.md#desktop-grade-interaction-contract).
Summary:

- **Context menus:** one `shared/ui/context-menu` primitive; each widget/feature supplies its own
  permission-filtered items for rows, headers, empty areas, tree nodes, cards, and shell chrome.
  The same actions are reachable with `Shift+F10` / the `ContextMenu` key.
- **Data grids:** filters, search, sorting, and pagination are URL state; column visibility is
  view state persisted per table under `diwan.table.<tableId>.columns.v1` (versioned, with a
  reset-to-default action). Bounded arrays paginate client-side with the same pager.
- **Mermaid visualization:** lazy-loaded `shared/ui/mermaid-view` (`securityLevel: 'strict'`,
  token-derived `themeVariables` that follow light/dark, no external links) with a diagram / chart /
  tree switcher owned by the hierarchy widget; entity `model/` builders turn API hierarchies into
  Mermaid or tree data (today `entities/department/model/toMermaid.ts`), and invalid Mermaid shows an
  Arabic-first error state with the parser message.

## Planned routes (backend-ready first)

| Route | Page slice | Backend module | Notes |
| --- | --- | --- | --- |
| `/login` | `pages/login` | 02 Identity | Public login |
| `/change-password` | `pages/change-password` | 02 Identity | ✅ Signed-in password change (forced after reset) |
| `/` | `pages/dashboard` | — | Recent docs + inbox counts |
| `/documents`, `/documents/:id` | `pages/documents` | 03 + 06 | ✅ Registry (list/filter/search/sort + create draft/registered) with the external-organizations admin tab (create/edit/deactivate/activate); ✅ detail page with participants, access grants, numbers (issue/cancel/replace/department registration/history), movements (open/send/receive/complete/reject/return/redirect/close/cancel + step timeline and event log), status history, timeline, and lifecycle actions; files tab pending |
| `/movements` | `pages/movements` | 03 | ✅ Department inbox/outbox (URL tabs, client filters/sort/paging, routing menus) |
| `/departments` | `pages/departments` | 01 | ✅ Hierarchy (tree / Mermaid / table) + department employees in the detail dialog; settings entry pending |
| `/users` | `pages/users` | 02 | ✅ Users admin |
| `/roles` | `pages/roles` | 02 | ✅ Roles + permission matrix |
| `/employees` | `pages/employees` | 02 | ✅ Employees admin (department-linked) |
| `/numbering` | `pages/numbering` | 04 | ✅ Registry administration + department register book |
| `/audit` | `pages/audit` | 09 | Search + integrity (self-auditing reads) |
| `/archive` | `pages/archive` | 07 | **Blocked** until Module 07 API exists |

Full endpoint tables: [`knowledge.md`](knowledge.md).

## Standalone extraction notes

- This folder is self-contained: no `.esproj`, solution entry, or sibling project reference.
- The API is external. Configure its origin with `DEV_PROXY_TARGET` for development and
  `VITE_API_BASE_URL` for builds; never hardcode an origin in source.
- [`DEPLOYMENT.md`](DEPLOYMENT.md) covers both same-origin reverse-proxy hosting and cross-origin
  static hosting.
- When this folder becomes its own repository, keep the FSD import direction and the
  `README.md`-per-folder rule intact.

## Contents

| File / Folder | Purpose |
| --- | --- |
| `src/app/` | Implemented application root (`index.tsx`), providers (theme, query, session loader), router + guards, and the Tailwind v4 global styles with `tokens.css`. |
| `src/pages/` | Route-level page slices; `login`, `change-password`, `dashboard`, `departments`, `users`, `roles`, `employees`, `documents` (registry + organizations tab + details), `numbering`, and `movements` implemented, the rest documented. |
| `src/widgets/` | Composite page sections combining entities and features; `app-shell`, `users-table`, `roles-table`, `employees-table`, `department-hierarchy-tree`, `document-registry-table`, `external-organizations-table`, `numbering-registry-table`, and `movement-inbox` implemented, the rest documented. |
| `src/features/` | CQRS Command mutations, Zod forms, and interactive workflows; `auth`, `user-manage`, `role-manage`, `employee-manage`, `document-create`, `document-manage`, `document-access-grant`, `numbering-manage`, `movement-routing`, and `external-organization-manage` implemented, the rest documented. |
| `src/entities/` | Domain read models, query key factories, `useQuery` hooks, and status badges; `user` (session + admin reads), `department`, `role`, `employee` (paged list with department/sub-department filters), `lookup`, `document`, `external-organization`, and `numbering-registry` implemented, the rest documented. |
| `src/shared/` | Axios client + RFC 7807 adapter + Arabic error catalog, config, utilities (table state, column preferences, dates, JWT hints), and the RTL UI kit including the shared data grid, context/dropdown menus, dialogs, select, checkbox, and the lazy Mermaid renderer. |
| `src/main.tsx` | Vite entry: mounts the `app` layer root and the global stylesheet. |
| `src/assets/` | Vite template images (`hero.png`, `react.svg`, `vite.svg`); replaced by the institution's own artwork. |
| `public/` | Vite template favicon and icon sprite. |
| `AGENTS.md` | Agent rules, desktop-grade interaction contract, 16 permissions, first-slice TypeScript contracts, UI state checklist. |
| `DEPLOYMENT.md` | Publishing guide, external API host/IP configuration, and step-by-step endpoint wiring. |
| `knowledge.md` | As-is/to-be, live endpoints (incl. Attachments + Audit), lookups, errors, build order. |
| `CHANGELOG.md` | Creation notes for this project. |
| `README.vite-template.md` | Stock Vite README kept for toolchain reference. |
| `vite.config.ts` | `@` → `./src` alias and env-driven `/api` + `/hubs` dev proxy. |
| `.env.example` | Documents `DEV_PROXY_TARGET` and `VITE_API_BASE_URL`; copy to `.env` locally (`.env` is git-ignored). |
| `package.json` | Scripts + installed Module 01 dependency stack. |
| `package-lock.json` | Locked dependency tree. |
| `eslint.config.js` | Flat ESLint config. |
| `index.html` | Arabic RTL document root (`lang="ar" dir="rtl"`), Arabic fonts, and the pre-paint theme boot script. |
| `tsconfig.json` / `tsconfig.app.json` / `tsconfig.node.json` | TypeScript project references (`@/*` → `./src/*`). |
| `.gitignore` | Node/Vite ignores plus `.env` / `.env.*` (keeping `.env.example` tracked). |

## Implementation status

| Layer / Area | State |
| --- | --- |
| FSD folder structure & layer `README.md` rules | ✅ Documented |
| Agent docs (`AGENTS.md`, `knowledge.md`) aligned to Modules 01–06 + 09 | ✅ Current |
| Per-slice docs (`entities/*`, `features/*`, `pages/*`, `widgets/*` READMEs) | ✅ Documented |
| UI / design contract documented | ✅ Documented |
| Interaction contract (context menus, data-grid preferences, Mermaid visualization) | ✅ Documented |
| Standalone framing (external API, no `.esproj`, no solution references) | ✅ Documented |
| Module 01 dependency stack installed | ✅ Installed |
| `@` → `./src` alias | ✅ Configured (`vite.config.ts` + `tsconfig.app.json`) |
| Dev proxy + `.env.example` | ✅ Configured (HTTPS dev certificate still to-be) |
| `index.html` RTL / document title / theme boot script | ✅ Configured |
| `src/shared/` (`api`, `ui`, `lib`, `config`, `types`) | ✅ Core implemented including `data-table`, `context-menu`, dialogs, menus, select/checkbox, URL table state, persisted columns, the lazy `mermaid-view`, and the expanded Arabic error catalog |
| `src/entities/` | ✅ `user` (session + admin reads), `department` (reads + `toMermaid`), `role`, `employee` (reads), `lookup` (incl. participant role/target catalogs), `document` (reads + details/timeline + number history), `external-organization`, `numbering-registry`, and `movement` (queues, by-document, actions rules, badges) implemented; document-file and audit-log pending |
| `src/features/` | ✅ `auth`, `user-manage` (incl. administrator password reset), `role-manage`, `employee-manage`, `document-create` (registered + draft), `document-manage` (draft edit/register, add participant, cancel with reason), `document-access-grant` (grant a user an access level with optional expiry; revoke), `numbering-manage` (registries; issue/cancel/replace/department registration), `movement-routing` (open/send/receive/complete/reject/return/redirect/close/cancel), and `external-organization-manage` (correspondent create/edit/deactivate/activate) implemented; department-*, attachment-upload, audit-manage pending |
| `src/widgets/` | ✅ `app-shell` (sidebar, header, drawer, settings, shell/item context menus), `users-table`, `roles-table`, `employees-table`, `department-hierarchy-tree` (tree / Mermaid / table + a detail dialog with the department's employees), `document-registry-table` (URL filters/sort/paging + create entry points + details link), `external-organizations-table` (server search/paging, active/all filter, status badges), `numbering-registry-table` (scope/format/reset/counters/status + create/edit/activate), and `movement-inbox` (inbox/outbox queue + routing menus); movement timeline panel, attachments panel, audit table, visualization panel pending |
| `src/pages/` | ✅ `login`, standalone `change-password`, shell-framed `dashboard`, `departments`, `users`, `roles`, `employees`, `documents` registry + external-organizations tab, `documents/:id` details (participants, access grants, numbers, movements, status history, timeline, lifecycle actions), `numbering`, and `movements`; audit pending (archive deferred) |
| `src/app/` (`providers`, `router`, `styles`) | ✅ Implemented (login-protected routes incl. `/change-password`, `/departments`, `/users`, `/roles`, `/employees`, `/documents`, `/documents/:documentId`, `/numbering`, `/movements`; forced reset redirect) |
| Publishing & API wiring guide (`DEPLOYMENT.md`) | ✅ Documented (proxy section reflects the configured setup) |

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-10-29 | agent | Completed and verified Module 03 Phase 7: added `features/external-organization-manage` (create/update, staged deactivate/activate, duplicate-code Arabic error kept inside the retained dialog, optional fields sent as `null`) and `widgets/external-organizations-table` (server search/paging with URL state, `active`/`all` filter, lookup-labeled type, permission-filtered create/edit/status menus), mounted as the «الجهات الخارجية» tab of `/documents` behind the existing `DOCUMENT_VIEW` gate with writes gated on `DOCUMENT_EDIT`; added the `organization-types` lookup and `ORGANIZATION_TYPE` IDs. In-browser: created `MOI-01`, a duplicate code produced «توجد جهة خارجية أخرى بنفس الرمز.» with the dialog open, edit carried the read-only code, the participant picker offered the updated organization, deactivation («إيقاف الجهة») hid it from that picker and showed «موقوف» under `include=all`, and activation restored both. No new route or sidebar entry. Build, lint, and 487 backend tests pass. |
| 2026-10-29 | agent | Completed and verified Module 03 Phase 6: added `entities/movement` (queues/by-document reads, query keys, transition rules, badges), `features/movement-routing` (open + nine routing commands with notes/reason/redirect forms), `widgets/movement-inbox`, the `/movements` inbox/outbox page, and the document-details movements tab (status/permission-filtered actions, step timeline, Arabic event log). Activated the «الحركات» sidebar entry. In-browser: opened and sent a movement from the Central Diwan to `DEP-HR`, the HR user received it against department number `HR-2026-118`, rejected it with a reason (auto-rerouted to the Central Diwan as step 2), the Central Diwan registered `CEN-2026-1000` and received, then closed — the document returned to «مسجل», both queues and the timeline refreshed. Fixed a stale Vite transform that crashed the queue table, mistranslated rejected step timestamps, and untranslated `Rejected:` step notes. Build, lint, and 487 backend tests pass. |
| 2026-10-29 | agent | Completed and verified Module 03 Phase 5: added `entities/numbering-registry` (registries/counters/department register), document number reads in `entities/document`, `features/numbering-manage` (registry CRUD/activate; issue/cancel/replace/department registration), `widgets/numbering-registry-table`, the `/numbering` page, and the document-details numbers tab; activated the «الترقيم» sidebar entry. In-browser: registry CRUD/status/counters, department register, draft issue → registered `2026/000005`, department registration `DEP-2026-001`, duplicate `2026/000007` rejected in Arabic with the dialog retained, replace `2026/000008` → `2026/000009`, cancel with reason, and a `USER`-role login with no numbers tab/registry page whose direct writes returned 403. Build, lint, and 487 backend tests pass. |
| 2026-10-28 | agent | Completed and verified Module 03 Phase 4: extended `entities/user` with the `identity-options/users` option read, added `features/document-access-grant` (grant a user an access level with optional future expiry and reason; revoke while keeping the record), and the `grants` tab in the document details page — visible only with `ACCESS_GRANT_MANAGE`, with `?tab=grants` falling back to details otherwise. In-browser: required-field and past-expiry Arabic validation, a grant to `user` appearing immediately with grantor/expiry and counts, a revoked grant staying as «ملغى» with timestamp + revoker, and a `USER`-role login seeing no tab (endpoint 403). The API accepts no revocation reason, so the revoke dialog confirms without one. Build and lint pass. |
| 2026-10-28 | agent | Completed Module 03 Phase 3: added `entities/external-organization` (paged/searchable reads), `features/document-manage` (edit draft, register draft into the numbering registry, add participant by external organization / internal department / named person, cancel with a reason), and the `/documents/:documentId` details page with participants, status history, and an Arabic-built audit timeline. Every transition is gated on both status and permission, registry rows link to details, and the participant role/target catalogs joined `entities/lookup`. Verified in-browser against the live API: draft actions, `?tab=participants`, person + department participants, draft edit, registration `2026/000004`, cancel with an Arabic reason, status history, timeline, and canceled-document gating. Build and lint pass. |
| 2026-10-28 | agent | Finished the Module 03 Phase 2 interactive pass against the live API: searched/filtered/sorted the registry, cleared the query in one URL write, saved a draft, hit `409 Numbering.OfficialNumberTaken` (Arabic field error, dialog stays open), and registered `999/2026` (toast + immediate row). Fixed the 409 message being rendered twice by suppressing the form-level alert for that code. Build and lint pass. |
| 2026-10-28 | agent | Completed Module 03 Phase 2: added `features/document-create` (mode cards for registered vs draft, lookup-driven type/direction/confidentiality/priority, Arabic validation, `Numbering.OfficialNumberTaken` field error) and `widgets/document-registry-table` (server paging/search/sort, status/direction/type/owning-department filters, copy actions, permission-filtered create menu), mounted `/documents` behind `DOCUMENT_VIEW`, registered the route, and activated the sidebar entry. Opened the department picker read to `DOCUMENT_VIEW` on the API so read-only users resolve department labels. Build and lint pass. |
| 2026-10-28 | agent | Clicking a department now works in the «مخطط» view too: `shared/ui/mermaid-view` gained an optional `onNodeClick` (activatable flowchart nodes, `Enter`/`Space`, label as accessible name) and `departmentTreeToMermaid` returns a node-id → department-id map, so a diagram node opens the same detail dialog with the employee roster. Verified in-browser (click and keyboard, filtered diagram). Build and lint pass. |
| 2026-10-28 | agent | Clicking a department now shows its employees: new `DepartmentEmployeesSection` in the hierarchy detail dialog (count badge, «تضمين الأقسام الفرعية» toggle on by default, permission gate on `EMPLOYEE_MANAGE`, skeleton/error/empty states, first-50 footer), fed by the new `departmentId` + `includeSubDepartments` filters on `GET /api/employees`. Verified in-browser (directorate 13 → toggle off 2). Build and lint pass. |
| 2026-10-28 | agent | Completed Module 01 Phase 1 (read-only hierarchy): added `shared/ui/mermaid-view` (lazy `mermaid`, strict security, token-driven theme variables, Arabic error state), `entities/department/model/toMermaid`, `widgets/department-hierarchy-tree` (interactive tree, Mermaid diagram, table, URL-driven search and view, node context menus, collapse/expand, detail dialog), and the `/departments` page + route + enabled sidebar entry behind `DEPARTMENT_MANAGE`. Verified in-browser (tree, theme-aware Mermaid, table search, dialog, keyboard menus); screenshots were unavailable. Build and lint pass. |
| 2026-10-28 | agent | Added the administrator password reset (`POST /api/users/{userId}/reset-password`): a «إعادة تعيين كلمة المرور» row action (disabled for the signed-in account) opens `ResetPasswordDialog`, which warns the admin, can generate a 12-character temporary password, and forces the user to set a new password at the next sign-in. Extracted the shared `PasswordField` (login, change-password, reset) and kept the dialog footer/body bounded. Build and lint pass. |
| 2026-10-28 | agent | Completed Phase 6 of Module 02 (final): implemented `/change-password` (Zod form + mutation + standalone page), forced the reset redirect from the JWT `must_change_password` claim so it survives a reload, routed reset logins there, added the account-menu entry, and mapped `Identity.NewPasswordSameAsOld`. Re-verified all protected routes, RTL, and both themes; build and lint pass. This closes the Identity admin front end. |
| 2026-10-28 | agent | Completed Phase 5 of Module 02: implemented `features/employee-manage` (Zod form + create/update, activate/deactivate, department selector sourced from `entities/department`), `widgets/employees-table` (server-side search/paging, department badges, row actions), and the `/employees` page behind `EMPLOYEE_MANAGE`; enabled its sidebar entry and added the employee validation error codes to the Arabic catalog. Verified in-browser (create validation, department load, create/edit, invalid email, deactivation, light/dark); build and lint pass. |
| 2026-10-28 | agent | Completed Phase 4 of Module 02: implemented `features/role-manage` (role create/edit, activate/deactivate, and a grouped permission-matrix dialog that saves only the grant/revoke diff), `widgets/roles-table`, and the `/roles` page behind `ROLE_MANAGE`; moved permission module labels into `entities/role` and reused them in the user access dialog. Verified in-browser (search, sorting, dialogs, matrix toggles, system-role protection, light/dark); build and lint pass. |
| 2026-10-28 | agent | Completed Phase 3 of Module 02: implemented `features/user-manage` (Zod request models + create/update, activate/deactivate, role assign/revoke, department-assignment add/end), `widgets/users-table`, and the `/users` page behind `USER_MANAGE`; enabled its sidebar entry and added the Phase 3 Arabic identity error codes. Verified in-browser (search, dialogs, validation, permissions, light/dark); build and lint pass. |
| 2026-10-27 | agent | Completed Phase 2 of Module 02: implemented the supporting entity reads (`department` list/tree/detail, `role`+permission catalog, `employee` paged list/detail, generic `lookup` catalogs with Arabic fallbacks), extended `entities/user` with paged user/detail/effective-permission hooks, and moved deterministic lookup ids to `shared/config`. All seven read endpoints were exercised against the live API (200, matching DTO shapes); build and lint pass. |
| 2026-10-27 | agent | Completed Phase 1 of Module 02: implemented `widgets/app-shell` (permission-filtered collapsible sidebar with «قريبًا» states, sticky header with shell/account menus, mobile drawer, display-settings dialog for theme + density, shell and item context menus with `Shift+F10`), added shared `useLogout`, nested protected routes under the shell, and slimmed the dashboard to content. Verified in-browser (RTL, dark/light, drawer, menus, density persistence, logout); build and lint pass. |
| 2026-10-27 | agent | Completed Phase 0 of the Module 02 (Identity admin) work: installed Radix primitives; added URL table state, column preferences, debounce, Damascus date/number, and JWT hint helpers; built dialogs/menus/select/checkbox/badges/skeletons and the shared `data-table`; extended the Arabic error catalog with Department/Lookup codes; added overlay + entrance-animation tokens; `npm run build` and `npm run lint` pass. |
| 2026-10-27 | agent | Implemented the Identity Module 01 vertical slice: shared foundation (axios client + Arabic error catalog, config, utils, UI primitives), `entities/user` session, `features/auth` (login/logout/permission guard), `app` providers/router/tokens, and the Arabic RTL `login` + `dashboard` pages; configured the `@` alias, dev proxy, `.env.example`, RTL `index.html` with the theme boot script, and git-ignored `.env`. Verified in-browser (RTL, light/dark, login/logout/401). |
| 2026-10-26 | agent | Ported the complete FSD documentation kit from the previous client project; renamed project references and recorded the standalone as-is state (stock Vite config, no alias/proxy/env/cert, no `.esproj`). |
| 2026-10-26 | agent | Added standalone extraction notes, external-API framing, and truthful to-be rows for the tooling that is not configured yet. |
