# institutionaldiwan.front / src/entities/institution-setting — Institution Settings Read Model

> **Status:** Implemented for Module 01 Organization; final integration verification is pending.

## Purpose

Owns the typed read of the singleton institution settings through `GET /api/institution-settings`
(`DEPARTMENT_MANAGE`). Writes and the editable form belong to `features/institution-settings-manage`.

## Contents

| File | Purpose |
| --- | --- |
| `model/types.ts` | `InstitutionSettingResponse`: institution names, logo path, timezone, numbering mode/reset policy, central department, responsible employee, and update timestamp. |
| `api/institutionSettingApi.ts` | Fetches the existing singleton through the shared HTTP client. |
| `model/queries.ts` | `institutionSettingKeys.all/detail()` and `useInstitutionSettings`, with a 60-second stale time and optional `enabled` flag. |
| `index.ts` | Public exports for the response type, query keys, and read hook. |

## Conventions / Notes

- Imports only `shared` and packages; no sibling entity imports or mutations.
- Missing settings are an API failure (`InstitutionSetting.NotFound`), not default form data.
  Neither the query nor its consumer creates a row or silently replaces settings after a failed read.
- The settings feature invalidates `institutionSettingKeys.all` after a successful explicit update.

## Implementation status

| Item | State |
| --- | --- |
| Singleton read, response type, query keys, public barrel | Implemented. |
| Missing-data recovery | Error passed to the form for Arabic feedback and retry; no automatic initialization. |
| Final browser/build/test verification | Pending main-agent verification. |

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-10-28 | agent | Document the Module 01 singleton read slice and missing-settings semantics so consumers do not infer an initialization or overwrite flow. |
