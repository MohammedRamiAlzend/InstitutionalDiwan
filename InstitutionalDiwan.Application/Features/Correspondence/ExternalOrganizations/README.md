# Application / Features / Correspondence / ExternalOrganizations — Module 03 external correspondents

> **Status:** Implemented — create, update, list, get by id, deactivate, and activate.

## Purpose

Manages external counterparties (ministries, companies, individuals) that can appear as participants
on incoming/outgoing correspondence. The `Code` is the stable, upper-cased business key and cannot be
changed once created; `ContactInfo` must be blank, an e-mail address, or a phone number (shared
`ExternalOrganizationContact` check). Deactivation is a soft delete: documents keep their participant
rows, the organization simply stops being selectable for new correspondence.

## Contents

| File | Purpose |
| --- | --- |
| `ExternalOrganizationResponse.cs` | Read model returned by every endpoint in the slice (`From(ExternalOrganization)`). |
| `ExternalOrganizationContact.cs` | Shared contact validator: `MailAddress.TryCreate` for e-mail, a generated regex for phone numbers; also used by both validators. |
| `CreateExternalOrganizationCommand.cs` | CQRS command carrying code, Arabic/English names, organization type, and contact info. |
| `CreateExternalOrganizationCommandHandler.cs` | Rejects duplicate/over-long codes (409/400), validates the `organization-types` lookup entry and the contact format, then persists a new active organization. |
| `CreateExternalOrganizationCommandValidator.cs` | FluentValidation rules for code, names, organization type, and contact length. |
| `UpdateExternalOrganizationCommand.cs` | CQRS command carrying everything except the immutable `Code`. |
| `UpdateExternalOrganizationCommandHandler.cs` | Loads the organization (404), revalidates organization type and contact info, applies `ExternalOrganization.Update`, and saves. |
| `UpdateExternalOrganizationCommandValidator.cs` | FluentValidation rules mirroring the create validator for the update payload. |
| `DeactivateExternalOrganizationCommand.cs` / `DeactivateExternalOrganizationCommandHandler.cs` | Soft delete through `ExternalOrganization.Deactivate` (idempotent), 404 when missing. |
| `ActivateExternalOrganizationCommand.cs` / `ActivateExternalOrganizationCommandHandler.cs` | Reactivates a deactivated organization through `ExternalOrganization.Activate` (idempotent). |
| `ListExternalOrganizationsQuery.cs` / `ListExternalOrganizationsQueryHandler.cs` | Returns one page of organizations (`PagedResult<ExternalOrganizationResponse>`) ordered by code, with `Page`/`PageSize`, a code/name `Search`, and `IncludeInactive`. Filtering and paging run in `IExternalOrganizationRepository.ListPagedAsync`. |
| `GetExternalOrganizationByIdQuery.cs` / `GetExternalOrganizationByIdQueryHandler.cs` | Fetches one organization by id (404 when missing). |

## Conventions / Notes

- Writes go through `Permissions.DocumentEdit`, reads through `Permissions.DocumentView` — the
  endpoints live on `ExternalOrganizationsController`.
- Deactivating an organization does **not** cascade to `DocumentParticipant` rows; existing
  correspondence keeps its history.

## Implementation status

| Item | State |
| --- | --- |
| Create / Update commands (+ validators) | ✅ |
| Deactivate / Activate commands | ✅ |
| List (paged + code/name search) / GetById queries | ✅ |
| `ExternalOrganizationsController` CRUD endpoints | ✅ |

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-10-22 | Buffy | Handlers now pass `IUnitOfWork.UtcNow` into `ExternalOrganization.Create`/`Update`/`Activate`/`Deactivate` for deterministic timestamps. |
| 2026-10-22 | opencode | `ListExternalOrganizationsQuery` now returns `PagedResult<ExternalOrganizationResponse>` with `page`, `pageSize`, and a code/name `search`; `ExternalOrganizationRepository.ListPagedAsync` applies the active filter, the search, and `Skip`/`Take` in SQL. |
| 2026-10-19 | Buffy | Added the external-organizations slice (create/update/deactivate/activate/list/get) with code-uniqueness, organization-type, and contact-format validation, plus `ExternalOrganization.Update`/`Activate` in the domain. |
