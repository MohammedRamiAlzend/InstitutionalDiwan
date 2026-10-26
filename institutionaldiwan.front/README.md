# institutionaldiwan.front — Institutional Diwan React 19 + TypeScript 6 + Vite 8 SPA (Feature-Sliced Design)

> **Status:** Documentation kit complete (2026-10-26), ported from the previous client project.
> `src/` still holds only the stock Vite React-TS template; every FSD
> layer and slice folder is documentation-only. Planned npm packages are **not** installed, and the
> `@` alias, dev proxy, `.env`, and HTTPS dev certificate are **not configured yet**.
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
| Runtime deps in `package.json` | `react`, `react-dom` only | Full stack in “Required Packages” below |
| FSD folders | ✅ README scaffolds for all six layers (`app`, `pages`, `widgets`, `features`, `entities`, `shared`) | Slice code implemented per those docs |
| `vite.config.ts` | Stock template — no `@` alias, proxy, or HTTPS certificate | `@` → `./src`, env-driven `/api` + `/hubs` proxy, optional local HTTPS |
| Dev configuration | No `.env` / `.env.example` | `.env` created for the env-driven proxy; kept out of git |
| UI | Vite welcome page | RTL app shell + module pages |
| Backend coverage docs | `knowledge.md` describes the external API | Modules 01–06, 09 live; 07/08/10 deferred |

Always treat **as-is** as the truth for “what runs today,” and **to-be** as the install/implement
checklist before coding features.

## Required Packages & One-Command Installation

> These packages are **required for feature work** but are **not installed yet**. Run the commands
> inside `institutionaldiwan.front/` before implementing the first slice.

```bash
# 1. Core runtime packages (Routing, Server/Client State, HTTP, Forms, Validation, Icons, UI, SignalR, Mermaid)
npm install react-router-dom @tanstack/react-query @tanstack/react-table zustand axios react-hook-form zod @hookform/resolvers lucide-react clsx tailwind-merge class-variance-authority sonner @microsoft/signalr mermaid

# 2. Radix UI primitives (for RTL-ready shadcn/ui components in src/shared/ui)
npm install @radix-ui/react-dialog @radix-ui/react-dropdown-menu @radix-ui/react-context-menu @radix-ui/react-select @radix-ui/react-tabs @radix-ui/react-tooltip @radix-ui/react-label @radix-ui/react-slot @radix-ui/react-checkbox @radix-ui/react-popover

# 3. Tailwind CSS v4 + React Query Devtools
npm install -D tailwindcss @tailwindcss/vite @tanstack/react-query-devtools
```

### API Target & Vite Proxy (to-be)

`vite.config.ts` is currently the stock Vite template. Before the first API-backed slice, add:

- the `@` → `./src` alias used by every FSD import;
- `server.proxy` entries for `^/api` (HTTP) and `^/hubs` (WebSocket upgrade);
- an env-driven target, for example `DEV_PROXY_TARGET`, with a sensible fallback to the API's
  development origin;
- optionally, the local HTTPS certificate for parity with the deployed origin.

Target shape (not yet present in the file):

```ts
server: {
  proxy: {
    '^/api': { target, secure: false, changeOrigin: true },
    '^/hubs': { target, secure: false, changeOrigin: true, ws: true },
  },
}
```

Configuration keys to create in `.env` when the proxy lands:

| Key | Purpose |
| --- | --- |
| `DEV_PROXY_TARGET` | Absolute backend origin for local development (`http://192.168.1.20:5028`, `https://localhost:7126`). |
| `VITE_API_BASE_URL` | Public base URL baked into the build (`/api` for same-origin deployments, absolute only for a cross-origin API + CORS). |
| `DEV_SERVER_PORT` | Dev-server port for the planned HTTPS proxy setup (stock Vite uses `5173`). |
| `DEV_HOST` / `DEV_ALLOWED_HOSTS` | Interface/hostname binding and allowed host headers. |

There is no `.env` or `.env.example` yet. When you add them, keep `.env` out of git — the stock
`.gitignore` ignores `*.local` but not `.env`.

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
| `src/app/` | Application providers, router guards, and Tailwind v4 RTL global styles (`README.md` inside; docs-only). |
| `src/pages/` | Route-level page slices mapped to Institutional Diwan modules (`README.md` inside; docs-only). |
| `src/widgets/` | Composite page sections combining entities and features (`README.md` inside; docs-only). |
| `src/features/` | CQRS Command mutations, Zod forms, and interactive workflows (`README.md` inside; docs-only). |
| `src/entities/` | Domain read models, query key factories, `useQuery` hooks, and status badges (`README.md` inside; docs-only). |
| `src/shared/` | Axios client + RFC 7807 adapter, RTL UI kit, utilities, and constants (`README.md` inside; docs-only). |
| `src/App.tsx` | Vite template placeholder root; replaced by the `app` layer once built. |
| `src/App.css` | Vite template stylesheet for the placeholder; removed with it. |
| `src/index.css` | Vite template global stylesheet; replaced by the `app/styles` Tailwind v4 entry point. |
| `src/main.tsx` | Vite entry that mounts `<App />`; will mount `app` providers instead. |
| `src/assets/` | Vite template images (`hero.png`, `react.svg`, `vite.svg`); replaced by the institution's own artwork. |
| `public/` | Vite template favicon and icon sprite. |
| `AGENTS.md` | Agent rules, desktop-grade interaction contract, 16 permissions, first-slice TypeScript contracts, UI state checklist. |
| `DEPLOYMENT.md` | Publishing guide, external API host/IP configuration, and step-by-step endpoint wiring. |
| `knowledge.md` | As-is/to-be, live endpoints (incl. Attachments + Audit), lookups, errors, build order. |
| `CHANGELOG.md` | Creation notes for this project. |
| `README.vite-template.md` | Stock Vite README kept for toolchain reference. |
| `vite.config.ts` | Stock Vite configuration (no `@` alias, proxy, or HTTPS certificate yet). |
| `package.json` | Scripts + current deps (React only until the install commands above are run). |
| `package-lock.json` | Locked dependency tree. |
| `eslint.config.js` | Flat ESLint config. |
| `index.html` | Vite HTML host; currently `lang="en"` — RTL `lang`/`dir` and the theme boot script are to-be. |
| `tsconfig.json` / `tsconfig.app.json` / `tsconfig.node.json` | TypeScript project references. |
| `.gitignore` | Node/Vite ignores; add `.env` when you create it. |

## Implementation status

| Layer / Area | State |
| --- | --- |
| FSD folder structure & layer `README.md` rules | ✅ Documented |
| Agent docs (`AGENTS.md`, `knowledge.md`) aligned to Modules 01–06 + 09 | ✅ Current (ported 2026-10-26) |
| Per-slice docs (`entities/*`, `features/*`, `pages/*`, `widgets/*` READMEs) | ✅ Documented — code still pending |
| UI / design contract documented | ✅ Documented |
| Interaction contract (context menus, data-grid preferences, Mermaid visualization) | ✅ Documented |
| Standalone framing (external API, no `.esproj`, no solution references) | ✅ Documented |
| Required npm packages installed | ❌ Not installed — React/Vite only |
| `@` → `./src` alias | ❌ Not configured (stock `vite.config.ts`) |
| Dev proxy + `.env` | ❌ Not configured |
| HTTPS dev certificate | ❌ Not configured |
| `index.html` RTL / document title / theme boot script | ❌ Still the stock `lang="en"` template |
| `src/shared/` (`api`, `ui`, `lib`, `config`, `types`) | ❌ Not implemented |
| `src/entities/` (document, document-file, department, user, role, numbering-registry, movement, audit-log, lookup) | ❌ Not implemented |
| `src/features/` (auth, document-*, department-*, user-*, role-*, movement-routing, attachment-upload, audit-manage) | ❌ Not implemented |
| `src/widgets/` (app-shell, registry table, hierarchy tree, movement timeline/inbox, attachments panel, audit table, visualization panel) | ❌ Not implemented |
| `src/pages/` (login, dashboard, documents, movements, departments, users, numbering, audit; archive deferred) | ❌ Not implemented |
| `src/app/` (`providers`, `router`, `styles`) | ❌ Not implemented |
| Publishing & API wiring guide (`DEPLOYMENT.md`) | ✅ Documented (target state) |

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-10-26 | agent | Ported the complete FSD documentation kit from the previous client project; renamed project references and recorded the standalone as-is state (stock Vite config, no alias/proxy/env/cert, no `.esproj`). |
| 2026-10-26 | agent | Added standalone extraction notes, external-API framing, and truthful to-be rows for the tooling that is not configured yet. |
