# institutionaldiwan.client — Institutional Diwan React 19 + TypeScript 6 + Vite 8 SPA (Feature-Sliced Design)

> **Status:** Documentation complete and current as of **2026-10-25**. FSD layers, agent rules, and
> backend contracts (Modules 01–06 + 09) are documented. **No application feature code yet** —
> `src/` still holds only the Vite template's `App.tsx` / `main.tsx`. Planned npm packages are
> **not** installed.

## Purpose

The front-end Single-Page Application (SPA) for the Institutional Diwan (نظام الديوان المؤسساتي
الموحد) system. It provides an Arabic-first (`dir="rtl"`) and bilingual (`ltr`) institutional
interface consuming the ASP.NET Core 10 REST API (`InstitutionalDiwan.Server`) across the live
modules, with SignalR hubs planned once the server wires them.

The codebase strictly follows Feature-Sliced Design (FSD v2.1) so AI coding agents and human
developers can implement slices predictably without spaghetti imports.

## As-is vs to-be

| Item | As-is | To-be |
| --- | --- | --- |
| Runtime deps in `package.json` | `react`, `react-dom` only | Full stack in “Required Packages” below |
| Dev proxy | `^/weatherforecast` in `vite.config.ts` | `^/api` + `^/hubs` → API |
| UI | Vite welcome page | RTL app shell + module pages |
| Backend coverage docs | Must stay aligned with `knowledge.md` | Modules 01–06, 09 live; 07/08/10 deferred |

Always treat **as-is** as the truth for “what runs today,” and **to-be** as the install/implement
checklist before coding features.

## Required Packages & One-Command Installation

> These packages are **required for feature work** but are **not installed yet**. Run the commands
> inside `institutionaldiwan.client/` before implementing the first slice.

```bash
# 1. Core runtime packages (Routing, Server/Client State, HTTP, Forms, Validation, Icons, UI, SignalR)
npm install react-router-dom @tanstack/react-query @tanstack/react-table zustand axios react-hook-form zod @hookform/resolvers lucide-react clsx tailwind-merge class-variance-authority sonner @microsoft/signalr

# 2. Radix UI primitives (for RTL-ready shadcn/ui components in src/shared/ui)
npm install @radix-ui/react-dialog @radix-ui/react-dropdown-menu @radix-ui/react-select @radix-ui/react-tabs @radix-ui/react-tooltip @radix-ui/react-label @radix-ui/react-slot @radix-ui/react-checkbox @radix-ui/react-popover

# 3. Tailwind CSS v4 + React Query Devtools
npm install -D tailwindcss @tailwindcss/vite @tanstack/react-query-devtools
```

### Required `vite.config.ts` Proxy Update

Replace the sample `^/weatherforecast` proxy in `vite.config.ts` with `^/api` and `^/hubs` so all
API and SignalR traffic proxies cleanly to `InstitutionalDiwan.Server`
(`https://localhost:7126` / `http://localhost:5028`):

```ts
server: {
  proxy: {
    '^/api': {
      target,
      secure: false,
    },
    '^/hubs': {
      target,
      secure: false,
      ws: true,
    },
  },
  port: parseInt(env.DEV_SERVER_PORT || '61325'),
  // ...
}
```

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
- Golden rule (`AGENT.md`): update each touched folder's `README.md` in the same turn.

## UI / design contract

Institutional, Arabic-first, calm — not a generic SaaS dashboard skin.

| Token / concern | Contract |
| --- | --- |
| Direction | `html[dir="rtl"]` by default; `dir="ltr"` islands for codes, official numbers, hashes, ISO dates |
| Typography | Prefer Arabic-capable faces (e.g. Cairo / Readex Pro / Noto Sans Arabic) via `app/styles`; avoid Inter/Roboto/Arial as the primary UI font |
| Color | CSS variables on `:root` — `--color-bg`, `--color-surface`, `--color-text`, `--color-accent`, `--color-danger`, `--color-border`. Prefer a restrained institutional palette (deep teal/ink + warm paper), not purple gradients |
| Spacing | 4-px base scale; content max-width for reading panes; full-bleed only for the app shell chrome |
| Components | Prefer `shared/ui` primitives; cards only when they wrap an interactive unit |
| Motion | Subtle (150–250 ms) for drawer/dialog; no decorative loop animations |
| Screen states | Loading / empty / error / forbidden required (see `AGENTS.md` and `knowledge.md`) |
| Density | Tables are the primary correspondence surface; keep filter bars URL-driven and compact |

Print: official receipts/register extracts use a dedicated `@media print` block in `app/styles`
(no sidebar, black text, LTR for numbers).

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

## Contents

| File / Folder | Purpose |
| --- | --- |
| `src/app/` | Application providers, router guards, and Tailwind v4 RTL global styles (`README.md` inside). |
| `src/pages/` | Route-level page slices mapped to Institutional Diwan modules (`README.md` inside). |
| `src/widgets/` | Composite page sections combining entities and features (`README.md` inside). |
| `src/features/` | CQRS Command mutations, Zod forms, and interactive workflows (`README.md` inside). |
| `src/entities/` | Domain read models, query key factories, `useQuery` hooks, and status badges (`README.md` inside). |
| `src/shared/` | Axios client + RFC 7807 adapter, RTL UI kit, utilities, and constants (`README.md` inside). |
| `src/App.tsx` | Vite template placeholder root; replaced by the `app` layer once built. |
| `src/main.tsx` | Vite entry that mounts `<App />`; will mount `app` providers instead. |
| `AGENTS.md` | Agent rules, 16 permissions, first-slice TypeScript contracts, UI state checklist. |
| `knowledge.md` | As-is/to-be, live endpoints (incl. Attachments + Audit), lookups, errors, build order. |
| `vite.config.ts` | Vite bundler, `@` → `./src`, HTTPS cert, proxy (**still weatherforecast until updated**). |
| `package.json` | Scripts + current deps (React only until the install commands above are run). |
| `package-lock.json` | Locked dependency tree. |
| `eslint.config.js` | Flat ESLint config. |
| `index.html` | Vite HTML host; `dir` and document title live here. |
| `tsconfig.json` / `tsconfig.app.json` / `tsconfig.node.json` | TypeScript project references. |
| `institutionaldiwan.client.esproj` | Visual Studio JavaScript project file. |
| `.env` / `.env.example` | Dev-server settings for `vite.config.ts`. |
| `README.vite-template.md` | Stock Vite README kept for toolchain reference. |
| `CHANGELOG.md` | Vite template creation notes. |

## Implementation status

| Layer / Area | State |
| --- | --- |
| FSD folder structure & layer `README.md` rules | ✅ Configured |
| Agent docs (`AGENTS.md`, `knowledge.md`) aligned to Modules 01–06 + 09 | ✅ Current (2026-10-25) |
| UI / design contract documented | ✅ Documented |
| Required npm packages installed | ❌ Not installed — React/Vite only |
| `src/shared/` (`api`, `ui`, `lib`, `config`, `types`) | ❌ Not implemented |
| `src/entities/` (document, document-file, department, user, role, numbering-registry, movement, audit-log, lookup) | ❌ Not implemented |
| `src/features/` (auth, document-*, department-*, user-*, role-*, movement-routing, attachment-upload, audit-manage) | ❌ Not implemented |
| `src/widgets/` (app-shell, registry table, hierarchy tree, movement timeline/inbox, attachments panel, audit table) | ❌ Not implemented |
| `src/pages/` (login, dashboard, documents, movements, departments, users, numbering, audit; archive deferred) | ❌ Not implemented |
| `src/app/` (`providers`, `router`, `styles`) | ❌ Not implemented |
| `vite.config.ts` proxy for `^/api` and `^/hubs` | ❌ Still `^/weatherforecast` |

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-10-25 | Cursor | Brought client docs to current backend truth: as-is/to-be, Modules 06/09, 16 permissions, UI/design contract, and backend-ready route map. |
| 2026-10-22 | opencode | Converted this folder's `README.md` from the Word document it was authored in to markdown, so the `README.md`-per-folder rule in `AGENT.md` holds and the content is diffable and agent-editable. |
| 2026-10-22 | opencode | Restored this folder's documentation as markdown: converted `README.md.docx` and `knowledge.md.docx` to `README.md` and `knowledge.md` so the golden rule in `AGENT.md` (a `README.md` per folder) holds and the content is diffable again; added the missing `AGENTS.md`; re-fenced the npm, `vite.config.ts`, and FSD-tree code blocks; completed the `Contents` table with every file in the folder. |
| 2026-10-19 | agent | Replaced default Vite template README with full Feature-Sliced Design (FSD) architecture guide, required npm package specification, and OpenCode/Freebuff rules. |
