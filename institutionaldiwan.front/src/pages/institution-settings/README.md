# institutionaldiwan.front / src/pages/institution-settings — Institution Settings Route

> **Status:** Implemented for Module 01 Organization; final browser verification is pending.

## Purpose

Provides `/institution-settings` as a thin route page inside `widgets/app-shell`.
The page gates access with `DEPARTMENT_MANAGE`, renders the Arabic page header, and delegates
settings reads, validation, and explicit updates to `features/institution-settings-manage`.
This is institution configuration, separate from the shell's local theme/density dialog.

## Contents

| File | Purpose |
| --- | --- |
| `ui/InstitutionSettingsPage.tsx` | Composes `RequirePermission`, `PageHeader`, and `InstitutionSettingsForm`. |
| `index.ts` | Exports `InstitutionSettingsPage` for the application router. |

## Conventions / Notes

- Imports lower-layer public barrels only; no raw HTTP calls or mutations in the page.
- Route constant: `ROUTES.INSTITUTION_SETTINGS`. The organization sidebar group exposes it only
  to callers with `DEPARTMENT_MANAGE`; authentication is enforced by the enclosing router guard.
- The feature renders initial loading skeletons and Arabic settings-query errors with retry.
  Missing settings do not produce a default editable record, automatic creation, or overwrite.
  An unexpected empty successful response currently leaves the form absent.

## Implementation status

| Item | State |
| --- | --- |
| Page, public export, shell route, permission-filtered navigation | Implemented. |
| Form composition and missing-permission state | Implemented through the feature and shared auth guard. |
| Final browser/build/test verification | Pending main-agent verification. |

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-10-28 | agent | Document the new Module 01 settings route, its permission boundary, and separation from local display settings. |
