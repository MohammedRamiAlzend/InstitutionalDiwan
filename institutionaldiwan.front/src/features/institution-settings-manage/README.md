# institutionaldiwan.front / src/features/institution-settings-manage — Institution Settings Form

> **Status:** Implemented for Module 01 Organization; final browser/build/test verification is pending.

## Purpose

Edits the existing singleton institution settings with an explicit `PUT /api/institution-settings`
under `DEPARTMENT_MANAGE`. The `/institution-settings` page composes this feature; the singleton
read and cache keys remain in `entities/institution-setting`.

## Contents

| File | Purpose |
| --- | --- |
| `model/types.ts` | `UpdateInstitutionSettingsRequest`, including nullable optional values and the central Diwan transfer reason. |
| `model/schemas.ts` | Arabic Zod validation for names, logo path, timezone, numbering selections, central department, and transfer-reason length. |
| `api/institutionSettingMutations.ts` | `useUpdateInstitutionSettingsMutation` sends the update and invalidates `institutionSettingKeys.all` on success. |
| `ui/InstitutionSettingsForm.tsx` | Loads settings, groups fields into institution/central Diwan/numbering/responsible-employee sections, validates transfers, and renders save feedback. |
| `index.ts` | Public exports for the form, mutation, and request type. |

## Conventions / Notes

- Allowed imports: entity public barrels and `shared`; never sibling features or upper layers.
- Fields include Arabic/English names, a logo **path** (not file upload), timezone, numbering mode,
  reset policy, central Diwan department, and optional responsible employee.
- Central Diwan choices enable only active root departments of type `DEPARTMENT_TYPE.CentralDiwan`.
  Changing the current designation reveals and requires a nonblank transfer reason. The backend
  independently validates the department, active numbering lookups, and employee existence.
- Numbering selectors use `numbering-modes` and `numbering-reset-policies` with seeded placeholders.
  Lookup failures show Arabic feedback with retry; the department-list failure is shown inline.
- The shared `EmployeePicker` is searchable/paged. Without `EMPLOYEE_MANAGE` it is disabled and
  issues no employee reads; the loaded responsible-employee value is preserved, not cleared.
- Initial settings loading renders skeletons. Any settings-query error, including
  `InstitutionSetting.NotFound`, renders an Arabic alert and retry **instead of an editable form**.
  An unexpected successful response with no data currently renders nothing. There is no create,
  auto-seed, default-value overwrite, or save-after-failed-load flow.
- Successful saves reset the form from the returned settings, toast in Arabic, and invalidate the
  settings cache. Mutation failures remain visible in Arabic. This is not a claim of dirty-form
  protection: a refreshed settings object also resets the form.

## Implementation status

| Item | State |
| --- | --- |
| Settings read/edit/save and Arabic validation/feedback | Implemented. |
| Central Diwan restrictions and transfer reason | Implemented in form and independently validated by backend. |
| Responsible employee picker and numbering selectors | Implemented; employee reads require their own permission. |
| Missing singleton | Error/retry only; automatic creation is not implemented. |
| Unexpected empty successful read | Returns no form; dedicated empty-state UI is not implemented. |
| Final integration verification | Pending main-agent browser/build/test results. |

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-10-28 | agent | Document the Module 01 settings form, explicit singleton update, picker permissions, and truthful loading/error/missing-data behavior. |
