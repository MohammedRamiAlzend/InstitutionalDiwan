# institutionaldiwan.front — Institutional Diwan React 19 + TypeScript 6 + Vite 8 SPA (Feature-Sliced Design)

> **Status:** Identity module (Module 01) vertical slice implemented (2026-10-27): shared
> foundation (`api`/`config`/`lib`/`types`/core `ui` primitives), `entities/user` session slice,
> `features/auth` (login/logout/permission guard), `app` providers + router + theme tokens, and the
> `pages/login` + `pages/dashboard` routes — Arabic-first RTL with light/dark themes, verified
> against the live API. The `@` alias, dev proxy, and `.env.example` are configured; the HTTPS dev
> certificate is still to-be.
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
| Runtime deps in `package.json` | React stack + Module 01 slice packages (see “Required Packages” history in the Change Log) | Remaining planned packages per slice (Radix primitives, `@tanstack/react-table`, SignalR, Mermaid) as their slices land |
| FSD folders | ✅ README scaffolds for all six layers; Module 01 slices implemented (`shared` foundation, `entities/user`, `features/auth`, `pages/login` + `dashboard`, `app`) | Remaining slice code implemented per those docs |
| `vite.config.ts` | ✅ `@` → `./src` alias + env-driven `/api` + `/hubs` proxy | Optional local HTTPS certificate |
| Dev configuration | ✅ `.env.example` committed (`DEV_PROXY_TARGET`, `VITE_API_BASE_URL`); create local `.env` as needed | — |
| UI | ✅ Arabic RTL login + minimal dashboard, light/dark themes from `tokens.css` | Full app shell + module pages |
| Backend coverage docs | `knowledge.md` describes the external API | Modules 01–06, 09 live; 07/08/10 deferred |

Always treat **as-is** as the truth for “what runs today,” and **to-be** as the install/implement
checklist before coding features.

## Required Packages & One-Command Installation

> The Module 01 slice packages are **installed** (React Router, TanStack Query, Zustand, Axios,
> react-hook-form, Zod, resolvers, lucide-react, clsx, tailwind-merge, CVA, sonner, Tailwind v4,
> devtools). The groups below remain the checklist for the remaining slices.

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
- **Mermaid visualization:** lazy-loaded `shared/ui/mermaid-view` (`securityLevel: 'strict'`)
  with a diagram / chart / tree switcher; entity `model/` builders turn API hierarchies into
  Mermaid or tree data (for example departments → employees), and invalid Mermaid shows an
  Arabic-first error state.

## Planned routes (backend-ready first)

| Route | Page slice | Backend module | Notes |
| --- | --- | --- | --- |
| `/login`, `/change-password` | `pages/login` | 02 Identity | Public login |
| `/` | `pages/dashboard` | — | Recent docs + inbox counts |
| `/documents`, `/documents/:id` | `pages/documents` | 03 + 06 | Detail hosts attachments panel |
| `/movements` | `pages/movements` | 05 | Inbox / outbox tabs via search params |
| `/departments` | `pages/departments` | 01 | Hierarchy + settings entry |
| `/users`, `/roles`, `/employees` | `pages/users` | 02 | Admin |
| `/numbering` | `pages/numbering` | 04 | Registries |
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
| `src/pages/` | Route-level page slices; `login` and `dashboard` implemented, the rest documented. |
| `src/widgets/` | Composite page sections combining entities and features (`README.md` inside; docs-only). |
| `src/features/` | CQRS Command mutations, Zod forms, and interactive workflows; `auth` implemented, the rest documented. |
| `src/entities/` | Domain read models, query key factories, `useQuery` hooks, and status badges; `user` (session) implemented, the rest documented. |
| `src/shared/` | Axios client + RFC 7807 adapter + Arabic error catalog, config, utilities, core UI primitives. |
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
| `src/shared/` (`api`, `ui`, `lib`, `config`, `types`) | ✅ Core implemented (`data-table`, `context-menu`, `mermaid-view` pending their slices) |
| `src/entities/` | ✅ `user` (session) implemented; document, document-file, department, role, numbering-registry, movement, audit-log, lookup pending |
| `src/features/` | ✅ `auth` implemented; document-*, department-*, user-*, role-*, movement-routing, attachment-upload, audit-manage pending |
| `src/widgets/` (app-shell, registry table, hierarchy tree, movement timeline/inbox, attachments panel, audit table, visualization panel) | ❌ Not implemented |
| `src/pages/` | ✅ `login` + minimal `dashboard`; documents, movements, departments, users, numbering, audit pending (archive deferred) |
| `src/app/` (`providers`, `router`, `styles`) | ✅ Implemented (login-protected routes; `/change-password` pending) |
| Publishing & API wiring guide (`DEPLOYMENT.md`) | ✅ Documented (proxy section reflects the configured setup) |

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-10-27 | agent | Implemented the Identity Module 01 vertical slice: shared foundation (axios client + Arabic error catalog, config, utils, UI primitives), `entities/user` session, `features/auth` (login/logout/permission guard), `app` providers/router/tokens, and the Arabic RTL `login` + `dashboard` pages; configured the `@` alias, dev proxy, `.env.example`, RTL `index.html` with the theme boot script, and git-ignored `.env`. Verified in-browser (RTL, light/dark, login/logout/401). |
| 2026-10-26 | agent | Ported the complete FSD documentation kit from the previous client project; renamed project references and recorded the standalone as-is state (stock Vite config, no alias/proxy/env/cert, no `.esproj`). |
| 2026-10-26 | agent | Added standalone extraction notes, external-API framing, and truthful to-be rows for the tooling that is not configured yet. |
