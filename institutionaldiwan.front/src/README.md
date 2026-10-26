# institutionaldiwan.front / src — Front-End Source Root

> **Status:** Docs ported from the previous client project on 2026-10-26 and aligned with the
> external API's Modules 01–06 + 09. Only the Vite template's entry files exist as code. Every FSD
> layer folder below is documented; no feature application code yet.

## Purpose

The root of the front end's source tree. It holds the Vite entry point that boots the React
application, plus the six Feature-Sliced Design layers the SPA is built from. When the `app`
layer is implemented, `main.tsx` will mount that layer's root component instead of the placeholder
`App` component, and this file will describe the two.

## Contents

| File / Folder | Purpose |
| --- | --- |
| `main.tsx` | Vite entry point: creates the React root and mounts `<App />`; will mount the `app` layer's providers and router. |
| `App.tsx` | Vite template placeholder that renders a welcome heading; replaced by the `app` layer's root component. |
| `App.css` | Vite template stylesheet for the placeholder `App`; removed with the placeholder. |
| `index.css` | Vite template global stylesheet; replaced by the `app/styles` Tailwind v4 entry point. |
| `assets/` | Vite template images (`hero.png`, `react.svg`, `vite.svg`); replaced by the institution's own artwork. |
| `app/` | FSD layer 1 — global providers, router and permission guards, global styles. |
| `pages/` | FSD layer 2 — one route page per slice. |
| `widgets/` | FSD layer 3 — composite page sections. |
| `features/` | FSD layer 4 — write mutations, Zod schemas, interactive forms and dialogs. |
| `entities/` | FSD layer 5 — read models, query key factories, `useQuery` hooks, read-only badges. |
| `shared/` | FSD layer 6 — HTTP client, RFC 7807 adapter, RTL UI kit, utilities, constants. |

## Conventions / Notes

- Imports flow strictly downward: `app` → `pages` → `widgets` → `features` → `entities` →
  `shared`. Never import upward and never import across sibling slices on the same layer.
- Every slice and `shared` segment exposes its public interface through its own `index.ts`
  barrel; deep imports are forbidden.
- The `@` path alias must point at this folder (`./src`), so an import of `@/shared` resolves to
  `src/shared`. It is not configured in the stock `vite.config.ts` yet.
- Each layer folder has its own `README.md` describing its rules, its planned slices, and its
  change log.

## Implementation status

| Item | State |
| --- | --- |
| `main.tsx`, `App.tsx`, `App.css`, `index.css` | ⚠️ Vite template placeholders |
| `app/` | ❌ Not implemented |
| `pages/` | ❌ Not implemented |
| `widgets/` | ❌ Not implemented |
| `features/` | ❌ Not implemented |
| `entities/` | ❌ Not implemented |
| `shared/` | ❌ Not implemented |

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-10-26 | agent | Ported this README into the standalone project; renamed the source root, noted the external API, and marked the `@` alias as not configured yet. |
| 2026-10-25 | Cursor | Noted docs are current for Modules 01–06 + 09 while application code remains the Vite template only. |
| 2026-10-22 | opencode | Added the missing `src/README.md`: the entry files the Vite template left behind, the six FSD layer folders, the import-direction rule, and the `@` path alias. |
