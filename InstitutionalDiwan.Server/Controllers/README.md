# Presentation / Controllers

> **Status:** Implemented — thirteen thin controllers, one per module plus the generic lookups controller.

## Purpose

Translate HTTP into Application use cases: read the request (and the caller's identity from the
token), send a Command/Query through MediatR `ISender`, and hand the returned `Result` / `Result<T>`
to `ResultExtensions.ToActionResult()` for RFC 7807 responses. No business rules and no `DbContext`
access live here.

## Contents

| File | Route | Authorization | Responsibility |
| --- | --- | --- | --- |
| `AuthController.cs` | `api/auth` | `[AllowAnonymous]` login/register, `[Authorize]` me/change-password | Login, registration, caller identity (`me`), password change. |
| `DocumentsController.cs` | `api/documents` | `DOCUMENT_VIEW` reads; `DOCUMENT_CREATE` create/register; `DOCUMENT_EDIT` update/participants/cancel | Full lifecycle: immediate create, list, get, details, timeline, `POST drafts`, `PUT {id}`, `POST {id}/register`, `POST {id}/participants`, `POST {id}/cancel`. |
| `DocumentNumbersController.cs` | `api/documents/{documentId}/numbers` | `NUMBERING_VIEW` reads; `NUMBERING_MANAGE` writes | Central number lifecycle (`issue`, `{numberId}/cancel`, `{numberId}/replace`, `history`), the department register write (`department-registration`: typed number + date), and `registry-entries` (central number + every department line). |
| `DepartmentRegistersController.cs` | `api/departments/{departmentId}/register` | `NUMBERING_VIEW` | The department's register book, optionally filtered by numbering year. |
| `MovementsController.cs` | `api/movements` | `DOCUMENT_EDIT` | `{movementId}/receive` — refuses custody until the destination department recorded its register number. |
| `ExternalOrganizationsController.cs` | `api/external-organizations` | `DOCUMENT_VIEW` reads; `DOCUMENT_EDIT` writes | External correspondent CRUD: list (incl. `includeInactive`), get, create, update, deactivate, activate. |
| `InstitutionSettingsController.cs` | `api/institution-settings` | `DEPARTMENT_MANAGE` | Reads and replaces the singleton institution settings (Central Diwan guards). |
| `UsersController.cs` | `api/users` | `USER_MANAGE` | User CRUD, activate/deactivate, assign/revoke roles, `POST {id}/department-assignments`, `DELETE {id}/department-assignments/{assignmentId}`, `GET {id}/effective-permissions`. |
| `RolesController.cs` | `api/roles` | `ROLE_MANAGE` | Role CRUD, activate, grant/revoke permissions. |
| `PermissionsController.cs` | `api/permissions` | `ROLE_MANAGE` | The permission catalog. |
| `DepartmentsController.cs` | `api/departments` | `DEPARTMENT_MANAGE` | Department CRUD, tree, reparent, soft delete + activate. |
| `EmployeesController.cs` | `api/employees` | `EMPLOYEE_MANAGE` | Employee CRUD, deactivate/activate. |
| `DocumentAccessGrantsController.cs` | `api/documents/{documentId}/access-grants` | `ACCESS_GRANT_MANAGE` | Per-document access grants: list/grant/revoke. |
| `NumberingRegistriesController.cs` | `api/numbering/registries` | `NUMBERING_VIEW` reads; `NUMBERING_MANAGE` writes | Numbering registry list/by-id + create, update, deactivate, activate. |
| `LookupsController.cs` | `api/lookups/{lookup}` | `[Authorize]` reads; `LOOKUP_MANAGE` writes | Generic reference-catalog API over all 33 seeded lookups. |

## Lookups endpoints

| Method | Route | Behavior |
| --- | --- | --- |
| `GET` | `/api/lookups/{lookup}?includeInactive=false` | List a catalog, ordered by id. |
| `GET` | `/api/lookups/{lookup}/{id}` | Fetch one entry (active or inactive). |
| `POST` | `/api/lookups/{lookup}` | Create an entry (`Id`, `Desc`, `DescAr`). |
| `PUT` | `/api/lookups/{lookup}/{id}` | Relabel an entry. |
| `PATCH` | `/api/lookups/{lookup}/{id}/deactivate` | Soft-delete an entry (409 when still referenced). |
| `PATCH` | `/api/lookups/{lookup}/{id}/activate` | Reactivate an entry. |

## Conventions / Notes

- `{lookup}` is a kebab-case slug registered in the Infrastructure `LookupCatalog` (33 slugs, e.g.
  `department-types`, `movement-statuses`, `audit-action-types`); an unknown slug is a 404.
- Class-level `[Authorize]` guards every route; write actions add a policy attribute, and the more
  restrictive policy wins because ASP.NET Core ANDs multiple `[Authorize]` attributes.
- `MediatR` handlers are `internal`; MediatR discovers them by assembly scan, so controllers only
  depend on the public command/query records.

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-10-20 | Buffy | Added the department register routes: `POST /api/documents/{id}/numbers/department-registration` and `GET .../registry-entries` on `DocumentNumbersController`, `DepartmentRegistersController`, and `MovementsController` (`{id}/receive`, gated on a recorded register entry). |
| 2026-10-19 | Buffy | Added `InstitutionSettingsController` (`GET`/`PUT /api/institution-settings`), `ExternalOrganizationsController` (CRUD), `DocumentNumbersController` (issue/cancel/replace/history), the document lifecycle endpoints on `DocumentsController` (drafts, update, register, participants, cancel, details, timeline), department-scope + effective-permissions endpoints on `UsersController`, and registry write endpoints on `NumberingRegistriesController`. |
| 2026-10-19 | opencode | Added `LookupsController` and documented the folder's ten controllers, their routes and their permission policies (this README did not exist before). |
