# institutionaldiwan.front / src/shared/config/labels — Editable UI Labels Catalog

> **Status:** Phases 1 + 3 (`common.ts` + `nav.ts` + `pages.ts` + `errors.ts`); per-domain area files land in Phase 4.

## Purpose

The built-in Arabic defaults for every editable UI label, per the UI labels plan
(`plans/ui-labels-edit-mode.md`). The app renders fully from this catalog with no server and no
overrides; the server (`UI_LABEL_OVERRIDES`) stores only what an administrator changed. Keeping the
defaults in code means a failed or absent labels fetch never breaks the interface.

## Contents

| File | Purpose |
| --- | --- |
| `common.ts` | `COMMON_LABELS`: shared chrome reused across surfaces — generic actions (save/cancel/delete/close/confirm/retry), the unified `data-table` tools (search, columns, sort, pagination, export), `confirm-dialog` defaults, the shell menu + label-edit banner, the account menu, the settings dialog, and the generic error fallbacks (`common.error.*`). Editing one renames it everywhere — intentional term unification. |
| `nav.ts` | `NAV_LABELS`: one key per sidebar group and item, plus the shell chrome around the nav (tagline, main-menu/expand-menu/collapse-menu aria-labels). Keys mirror `shell-nav.ts` ids. |
| `pages.ts` | `PAGE_LABELS`: static page titles and descriptions (`page.<slug>.title` / `.description`) rendered through `PageHeader`'s `titleKey`/`descriptionKey`. Dynamic titles (a document's subject, a person's name) and feature-flag-composed descriptions stay in code. |
| `errors.ts` | `ERROR_LABELS`: the full backend error catalog (~358 codes) keyed `error.<code-lowercased>`. `shared/api/errorMessages.ts` maps each code to its key and resolves it through `t()`, so error messages are renamable like any other label. |
| `index.ts` | Aggregates the area files into the frozen `LABEL_DEFAULTS` and derives the `LabelKey` union (`keyof typeof LABEL_DEFAULTS`); an unknown key is a build error. |

## Conventions / Notes

- Key form: `<area>.<surface>.<element>`, lowercase Latin segments separated by single dots/dashes
  (matches the server's `UiLabelOverride` key pattern). Placeholders use `{name}` and are filled by
  `t(key, params)` / `useLabel(key, params)` in `shared/lib/labels.ts`.
- A new area file (`documents.ts`, `organization.ts`, …) is spread into `LABEL_DEFAULTS` here as its
  migration phase lands; no other wiring is needed.
- Resolution order at runtime: server override → built-in default. See `shared/lib/labels.ts`.

## Implementation status

| Item | State |
| --- | --- |
| `common.ts`, `nav.ts`, `LABEL_DEFAULTS`, `LabelKey` | ✅ Implemented; consumed by the sidebar proof (`widgets/app-shell`). |
| Phase 3 common chrome + `pages.ts` + `errors.ts` | ✅ Implemented; consumed by `data-table`, `confirm-dialog`, the shell/header/settings, `PageHeader` (18 pages), and `errorMessages.ts`. |
| Per-domain area catalogs (documents, organization, …) | ⏳ Phase 4. |

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-11-17 | Qoder | UI labels Phase 3: expanded `common.ts` (actions, table tools, shell menu + banner, account menu, settings dialog, `common.error.*` fallbacks); added `pages.ts` (static page titles/descriptions) and `errors.ts` (full ~358-code error catalog); all spread into `LABEL_DEFAULTS`. |
| 2026-11-17 | Qoder | Created the catalog for UI labels Phase 1: `common.ts` + `nav.ts` frozen defaults aggregated into `LABEL_DEFAULTS` with the derived `LabelKey`. |