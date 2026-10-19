# Application / Features / Organization / InstitutionSettings — Module 01 singleton configuration

> **Status:** Implemented — read and full-update slices for the single `InstitutionSetting` row.

## Purpose

Holds the use cases that read and replace the institution-wide configuration: the Central Diwan
(root) department, its responsible employee, the default numbering mode and reset policy, and the
default timezone. `GET` returns the singleton; `PUT` applies a full replacement after validating the
designated Central Diwan department (exists, active, `CENTRAL_DIWAN` type, no parent) and the
referenced lookups. The one domain-owned rule — moving the Central Diwan requires a documented
transfer reason — lives in `InstitutionSetting.Update` and is surfaced as
`InstitutionSetting.CentralDiwanTransferReasonRequired` (400).

## Contents

| File | Purpose |
| --- | --- |
| `InstitutionSettingResponse.cs` | Flat read model for the settings row (`From(InstitutionSetting)`), returned by both endpoints. |
| `GetInstitutionSettingsQuery.cs` | CQRS query returning the singleton row; 404 when nothing has been seeded. |
| `GetInstitutionSettingsQueryHandler.cs` | Loads the singleton through `IInstitutionSettingRepository.GetSingletonAsync` and maps it to the response. |
| `UpdateInstitutionSettingsCommand.cs` | CQRS command carrying names, numbering mode/policy, Central Diwan department, optional responsible employee, timezone, and the transfer reason. |
| `UpdateInstitutionSettingsCommandHandler.cs` | Validates Central Diwan existence/type/root/activity, numbering mode + reset policy against the `numbering-modes` / `numbering-reset-policies` lookups, and the optional employee, then applies `InstitutionSetting.Update` and saves. |
| `UpdateInstitutionSettingsCommandValidator.cs` | FluentValidation rules: non-blank Arabic name, numbering mode + reset policy > 0, Central Diwan department required, bounded lengths. |

## Conventions / Notes

- The row is a singleton by convention — updates always apply to whatever `GetSingletonAsync`
  returns; there is no create use case (the `InstitutionSettingSeeder` owns first insert).
- Lookup ids are validated through `ILookupRepository` (`numbering-modes`, `numbering-reset-policies`)
  so an inactive or unknown entry is rejected instead of corrupting the configuration.
- Changing the Central Diwan is a structural act: the endpoint sits behind `DEPARTMENT_MANAGE`.

## Implementation status

| Item | State |
| --- | --- |
| `GetInstitutionSettingsQuery` (+ handler) | ✅ |
| `UpdateInstitutionSettingsCommand` (+ handler, validator) | ✅ |
| `InstitutionSettingsController` (`GET`/`PUT /api/institution-settings`) | ✅ |

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-10-19 | Buffy | Added the Module 01 institution-settings slice (query + full update command) with Central Diwan root/type/activity validation, lookup validation for numbering mode and reset policy, optional responsible-employee check, and the transfer-reason rule from the domain. |
