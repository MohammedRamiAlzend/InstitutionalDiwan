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
| `InstitutionSettingResponse.cs` | Flat read model for the settings row (`From(InstitutionSetting)`), returned by both endpoints and the logo endpoints; carries `LogoUpdatedAt`. |
| `GetInstitutionSettingsQuery.cs` | CQRS query returning the singleton row; 404 when nothing has been seeded. |
| `GetInstitutionSettingsQueryHandler.cs` | Loads the singleton through `IInstitutionSettingRepository.GetSingletonAsync` and maps it to the response. |
| `UpdateInstitutionSettingsCommand.cs` | CQRS command carrying names, numbering mode/policy, Central Diwan department, optional responsible employee, timezone, and the transfer reason. |
| `UpdateInstitutionSettingsCommandHandler.cs` | Validates Central Diwan existence/type/root/activity, numbering mode + reset policy against the `numbering-modes` / `numbering-reset-policies` lookups, and the optional employee, then applies `InstitutionSetting.Update` and saves. |
| `UpdateInstitutionSettingsCommandValidator.cs` | FluentValidation rules: non-blank Arabic name, numbering mode + reset policy > 0, Central Diwan department required, and bounded lengths including `LogoPath` at 500 characters. |
| `BrandingResponse.cs` | The public identity: Arabic and English name plus `LogoUpdatedAt` (null without a logo; also the cache version of the logo address). |
| `GetBrandingQuery.cs` | Query and handler behind the anonymous `GET /api/branding`; 404 when nothing has been seeded. |
| `GetInstitutionLogoQuery.cs` | Query and handler returning the uploaded logo bytes, media type and time, or `InstitutionSetting.LogoNotFound`. |
| `SetInstitutionLogoCommand.cs` | Command and handler that store or replace the logo (`InstitutionLogo.Create`/`Replace` check size and kind) and stamp `LogoUpdatedAt` on the setting. |
| `RemoveInstitutionLogoCommand.cs` | Command and handler that delete the logo and clear `LogoUpdatedAt`; removing when there is none succeeds unchanged. |

## Conventions / Notes

- The row is a singleton by convention — updates always apply to whatever `GetSingletonAsync`
  returns; there is no create use case (the `InstitutionSettingSeeder` owns first insert).
- Lookup ids are validated through `ILookupRepository` (`numbering-modes`, `numbering-reset-policies`)
  so an inactive or unknown entry is rejected instead of corrupting the configuration.
- Changing the Central Diwan is a structural act: the endpoint sits behind `DEPARTMENT_MANAGE`.
  Department update/move/deactivate commands reject structural changes to the current hub with
  `Department.CentralDiwanProtected` (409); transfer here first to release the former hub.

## Implementation status

| Item | State |
| --- | --- |
| `GetInstitutionSettingsQuery` (+ handler) | ✅ |
| `UpdateInstitutionSettingsCommand` (+ handler, validator) | Implemented; `LogoPath` maximum 500 characters |
| `InstitutionSettingsController` (`GET`/`PUT /api/institution-settings`) | ✅ |

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-11-12 | Claude | Added the institution logo: `GetBrandingQuery`, `GetInstitutionLogoQuery`, `SetInstitutionLogoCommand` and `RemoveInstitutionLogoCommand`; the response carries `LogoUpdatedAt`. |
| 2026-11-11 | Claude |  |
| 2026-11-11 | Claude | The update command and the response carry `ArchiveDiwanDepartmentId`: an active department other than the Central Diwan, or null to clear it. |
| 2026-10-28 | Qoder | Limit `LogoPath` to 500 characters before persistence and document that department writes require a settings transfer before changing the current hub's hierarchy or active state. |
| 2026-10-22 | Buffy | `UpdateInstitutionSettingsCommandHandler` now passes `IUnitOfWork.UtcNow` so `InstitutionSetting.UpdatedAt` comes from the transaction, not the system clock. |
| 2026-10-19 | Buffy | Added the Module 01 institution-settings slice (query + full update command) with Central Diwan root/type/activity validation, lookup validation for numbering mode and reset policy, optional responsible-employee check, and the transfer-reason rule from the domain. |
