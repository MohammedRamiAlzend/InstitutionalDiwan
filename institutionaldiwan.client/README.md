# institutionaldiwan.client — Institutional Diwan React 19 + TypeScript 6 + Vite 8 SPA (Feature-Sliced Design)

> **Status:** FSD Architecture & Vibe-Coding Rules Configured — 6 FSD layers (`app`, `pages`,
> `widgets`, `features`, `entities`, `shared`), required npm packages, and OpenCode/Freebuff rules
> & skills defined. No application code written yet: `src/` still holds only the Vite template's
> `App.tsx` and `main.tsx`.

## Purpose

The front-end Single-Page Application (SPA) for the Institutional Diwan (نظام الديوان المؤسساتي
الموحد) system. It provides an Arabic-first (`dir="rtl"`) and bilingual (`ltr`) institutional
interface consuming the ASP.NET Core 10 REST API (`InstitutionalDiwan.Server`) and SignalR
real-time hubs across all 10 modules.

The codebase strictly follows Feature-Sliced Design (FSD v2.1) so that AI coding agents (OpenCode
and Freebuff / Codebuff) and human developers can vibe-code slices predictably without spaghetti
imports.

## Required Packages & One-Command Installation

Run these commands inside `institutionaldiwan.client/` before building features:

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
├── app/        # 1. Global providers, router, auth/permission guards, global styles (may import from all lower layers)
│   ├── providers/
│   ├── router/
│   └── styles/
├── pages/      # 2. Route pages (1 slice = 1 route; composes widgets, features, entities)
├── widgets/    # 3. Standalone composite UI blocks (app-shell, document-registry-table, department-hierarchy-tree, ...)
├── features/   # 4. User interactions & write mutations mapped to backend CQRS Commands (POST/PUT/DELETE)
├── entities/   # 5. Business domain models, GET queries (useQuery), and read-only badges/cards
└── shared/     # 6. Domain-agnostic foundation (api, ui, lib, config, types — imports nothing from upper layers)
    ├── api/
    ├── ui/
    ├── lib/
    ├── config/
    └── types/
```

### Strict FSD Rules for AI Agents (OpenCode & Freebuff)

- Unidirectional Import Rule: app → pages → widgets → features → entities → shared. A module may
  never import from its own layer (no cross-slice imports) or from a higher layer.
- Public API Barrel (`index.ts`): Every slice and `shared` segment must export its public
  interface via `index.ts`. Deep imports (`@/entities/document/ui/DocumentStatusBadge`) are
  forbidden.
- Reads vs. Writes:
  - All `GET` queries (`useQuery`) and query key factories belong in `src/entities/<slice>/`.
  - All state-changing mutations (`useMutation` for `POST`/`PUT`/`DELETE`) and Zod form schemas
    belong in `src/features/<slice>/`.
- Golden Rule (`AGENT.md`): Whenever a folder's contents change, update that folder's `README.md`
  (`Contents`, `Implementation status`, and `Change Log`) in the same turn.

## Contents

| File / Folder | Purpose |
| --- | --- |
| `src/app/` | Application providers, router guards, and Tailwind v4 RTL global styles (`README.md` inside). |
| `src/pages/` | Route-level page slices mapped to the 10 Institutional Diwan modules (`README.md` inside). |
| `src/widgets/` | Composite page sections combining entities and features (`README.md` inside). |
| `src/features/` | CQRS Command mutations, Zod forms, and interactive workflows (`README.md` inside). |
| `src/entities/` | Domain read models, query key factories, `useQuery` hooks, and status badges (`README.md` inside). |
| `src/shared/` | Axios client + RFC 7807 `ProblemDetails` adapter, RTL UI kit, utilities, and constants (`README.md` inside). |
| `src/App.tsx` | Vite template placeholder root component; replaced by the `app` layer's root component once that layer is built. |
| `src/main.tsx` | Vite template entry point that mounts `<App />`; will mount the `app` layer's providers instead. |
| `AGENTS.md` | OpenCode & Freebuff rules for vibe-coding in this client project. |
| `knowledge.md` | Freebuff/Codebuff project knowledge base (commands, endpoints, lookup IDs, FSD constraints). |
| `vite.config.ts` | Vite bundler, `@` path alias (`./src`), dev HTTPS certificate, and backend proxy setup. |
| `package.json` | NPM dependencies and scripts (`dev`, `build`, `lint`, `preview`). |
| `package-lock.json` | Locked dependency tree for reproducible installs. |
| `eslint.config.js` | Flat ESLint config (typescript-eslint, react-hooks, react-refresh). |
| `index.html` | Vite HTML host page; the `dir` attribute and document title live here. |
| `tsconfig.json` / `tsconfig.app.json` / `tsconfig.node.json` | Solution-style TypeScript project references for the app and the build tooling. |
| `institutionaldiwan.client.esproj` | Visual Studio JavaScript project file, so the client loads as a project in the IDE. |
| `.env` | Local dev-server settings (port, host, proxy target) read by `vite.config.ts`; never committed with secrets. |
| `.env.example` | Template for `.env` listing the supported keys. |
| `README.vite-template.md` | The untouched Vite template README, kept as a reference for the stock toolchain. |
| `CHANGELOG.md` | Notes on the Vite template's creation steps. |

## Implementation status

| Layer / Area | State |
| --- | --- |
| FSD Folder Structure & Layer `README.md` Rules | ✅ Configured |
| OpenCode & Freebuff Rules (`AGENTS.md`, `knowledge.md`) | ✅ Configured |
| Required npm packages installed | ❌ Not installed — `node_modules` holds only the Vite template's dependencies |
| `src/shared/` (`api`, `ui`, `lib`, `config`, `types`) | ❌ Not implemented |
| `src/entities/` (`document`, `department`, `user`, `role`, `numbering-registry`, `lookup`) | ❌ Not implemented |
| `src/features/` (`auth`, `document-create`, `department-manage`, `user-manage`, `role-manage`, `document-access-grant`) | ❌ Not implemented |
| `src/widgets/` (`app-shell`, `document-registry-table`, `department-hierarchy-tree`) | ❌ Not implemented |
| `src/pages/` (`login`, `dashboard`, `documents`, `departments`, `users`, `numbering`) | ❌ Not implemented |
| `src/app/` (`providers`, `router`, `styles`) | ❌ Not implemented |
| `vite.config.ts` proxy rewritten for `^/api` and `^/hubs` | ❌ Still points at `^/weatherforecast` |

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-10-22 | opencode | Converted this folder's `README.md` from the Word document it was authored in to markdown, so the `README.md`-per-folder rule in `AGENT.md` holds and the content is diffable and agent-editable. |
| 2026-10-22 | opencode | Restored this folder's documentation as markdown: converted `README.md.docx` and `knowledge.md.docx` to `README.md` and `knowledge.md` so the golden rule in `AGENT.md` (a `README.md` per folder) holds and the content is diffable again; added the missing `AGENTS.md`; re-fenced the npm, `vite.config.ts`, and FSD-tree code blocks; completed the `Contents` table with every file in the folder. |
| 2026-10-19 | agent | Replaced default Vite template README with full Feature-Sliced Design (FSD) architecture guide, required npm package specification, and OpenCode/Freebuff rules. |
