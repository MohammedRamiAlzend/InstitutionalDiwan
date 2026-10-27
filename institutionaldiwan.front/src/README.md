# institutionaldiwan.front / src — Front-End Source Root

> **Status:** Identity Module 01 implemented (2026-10-27): the `app` layer boots the SPA, `shared`
> provides the HTTP client, tokens and core UI primitives, `entities/user` holds the session,
> `features/auth` owns login/logout, and `pages/login` + `pages/dashboard` are live routes.
> Remaining slices are documented but not yet coded.

## Purpose

The root of the front end's source tree. It holds the Vite entry point that boots the React
application, plus the six Feature-Sliced Design layers the SPA is built from.

## Contents

| File / Folder | Purpose |
| --- | --- |
| `main.tsx` | Vite entry point: imports `app/styles/index.css` and mounts the `app` layer root. |
| `app/` | FSD layer 1 — global providers, router and permission guards, theme tokens and global styles. |
| `pages/` | FSD layer 2 — one route page per slice (`login`, `dashboard` implemented). |
| `widgets/` | FSD layer 3 — composite page sections (documented, not implemented). |
| `features/` | FSD layer 4 — write mutations, Zod schemas, interactive forms (`auth` implemented). |
| `entities/` | FSD layer 5 — read models, query key factories, `useQuery` hooks (`user` implemented). |
| `shared/` | FSD layer 6 — HTTP client, RFC 7807 → Arabic message catalog, RTL UI kit, constants. |
| `assets/` | Vite template images (`hero.png`, `react.svg`, `vite.svg`); replaced by the institution's own artwork. |

## Conventions / Notes

- Imports flow strictly downward: `app` → `pages` → `widgets` → `features` → `entities` →
  `shared`. Never import upward and never import across sibling slices on the same layer.
- Every slice and `shared` segment exposes its public interface through its own `index.ts`
  barrel; deep imports are forbidden.
- The `@` path alias points at this folder (`./src`), configured in `vite.config.ts` and
  `tsconfig.app.json`.
- Each layer folder has its own `README.md` describing its rules, its planned slices, and its
  change log.

## Implementation status

| Item | State |
| --- | --- |
| `main.tsx` | ✅ Mounts `@/app` + global stylesheet; template `App.tsx` / `App.css` / `index.css` removed |
| `app/` | ✅ Implemented |
| `pages/` | ✅ `login`, `dashboard` (minimal); remaining slices pending |
| `widgets/` | ❌ Not implemented |
| `features/` | ✅ `auth`; remaining slices pending |
| `entities/` | ✅ `user`; remaining slices pending |
| `shared/` | ✅ Core (`api`, `config`, `lib`, `types`, `ui` primitives) |

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-10-27 | agent | Rewired `main.tsx` to the `app` layer; deleted the Vite template `App.tsx`, `App.css`, and `index.css` now that `app/styles` owns the global stylesheet. |
| 2026-10-26 | agent | Ported this README into the standalone project; renamed the source root, noted the external API, and marked the `@` alias as not configured yet. |
| 2026-10-25 | Cursor | Noted docs are current for Modules 01–06 + 09 while application code remains the Vite template only. |
| 2026-10-22 | opencode | Added the missing `src/README.md`: the entry files the Vite template left behind, the six FSD layer folders, the import-direction rule, and the `@` path alias. |
