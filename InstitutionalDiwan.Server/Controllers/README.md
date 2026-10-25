# Presentation / Controllers

> **Status:** Implemented — sixteen thin controllers, one per module plus the generic lookups controller.

## Purpose

Translate HTTP into Application use cases: read the request (and the caller's identity from the
token), send a Command/Query through MediatR `ISender`, and hand the returned `Result` / `Result<T>`
to `ResultExtensions.ToActionResult()` for RFC 7807 responses. No business rules and no `DbContext`
access live here.

## Contents

| File | Route | Authorization | Responsibility |
| --- | --- | --- | --- |
| `AuthController.cs` | `api/auth` | `[AllowAnonymous]` login/register, `[Authorize]` me/change-password | Login, registration, caller profile (`me` → `GetUserProfileQuery`), password change. |
| `DocumentsController.cs` | `api/documents` | `DOCUMENT_VIEW` reads; `DOCUMENT_CREATE` create/register; `DOCUMENT_EDIT` update/participants/cancel | Full lifecycle: immediate create, paged/searchable/filterable list, a department's register view (`GET /api/departments/{departmentId}/documents`), get, details, timeline, `POST drafts`, `PUT {id}`, `POST {id}/register`, `POST {id}/participants`, `POST {id}/cancel`. |
| `DocumentNumbersController.cs` | `api/documents/{documentId}/numbers` | `NUMBERING_VIEW` reads; `NUMBERING_MANAGE` writes | Central number lifecycle (`issue`, `{numberId}/cancel`, `{numberId}/replace`, `history`), the department register write (`department-registration`: typed number + date), and `registry-entries` (central number + every department line). |
| `DepartmentRegistersController.cs` | `api/departments/{departmentId}/register` | `NUMBERING_VIEW` | The department's register book, optionally filtered by numbering year. |
| `MovementsController.cs` | `api/movements` (plus planned document/department/step aliases) | `DOCUMENT_VIEW` reads; `DOCUMENT_EDIT` writes | The routing workflow: open a movement (Central Diwan hub enforced), send, receive (refuses custody until the destination department recorded its register number), complete, reject (routed back to the Central Diwan), return, redirect, close, cancel; plus the movement read endpoints, the department inbox/outbox, and the planned step-scoped route aliases. |
| `DocumentFilesController.cs` | `api/document-files` (plus planned `/api/documents/{documentId}/files` aliases) | `DOCUMENT_VIEW` reads; `DOCUMENT_EDIT` writes | Module 06 attachments: create a file's version chain (metadata-only or atomic first-version upload), upload the next version (multipart; extension/size/magic-byte validation), archive, list by document, download a version, and verify its SHA-256. |
| `ExternalOrganizationsController.cs` | `api/external-organizations` | `DOCUMENT_VIEW` reads; `DOCUMENT_EDIT` writes | External correspondent CRUD: paged/searchable list (incl. `includeInactive`), get, create, update, deactivate, activate. |
| `InstitutionSettingsController.cs` | `api/institution-settings` | `DEPARTMENT_MANAGE` | Reads and replaces the singleton institution settings (Central Diwan guards). |
| `UsersController.cs` | `api/users` | `USER_MANAGE` | User CRUD (paged/searchable list), activate/deactivate, assign/revoke roles, `POST {id}/department-assignments`, `DELETE {id}/department-assignments/{assignmentId}`, `GET {id}/effective-permissions`. |
| `RolesController.cs` | `api/roles` | `ROLE_MANAGE` | Role CRUD, activate, grant/revoke permissions. |
| `PermissionsController.cs` | `api/permissions` | `ROLE_MANAGE` | The permission catalog. |
| `DepartmentsController.cs` | `api/departments` | `DEPARTMENT_MANAGE` | Department CRUD, tree, reparent, soft delete + activate. |
| `EmployeesController.cs` | `api/employees` | `EMPLOYEE_MANAGE` | Employee CRUD (paged/searchable list), deactivate/activate. |
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

## Movements endpoints

| Method | Route | Permission | Behavior |
| --- | --- | --- | --- |
| `POST` | `/api/movements` | `DOCUMENT_EDIT` | Route a document to another department by opening a movement. The source department is the caller's own primary department. |
| `GET` | `/api/movements/{movementId}` | `DOCUMENT_VIEW` | One movement with its steps and audit trail. |
| `GET` | `/api/movements/by-document/{documentId}` | `DOCUMENT_VIEW` | Every movement of a document, newest first. |
| `GET` | `/api/documents/{documentId}/movements` | `DOCUMENT_VIEW` | Planned alias of the document movement history. |
| `GET` | `/api/movements/inbox?departmentId=` | `DOCUMENT_VIEW` | The documents waiting in a department's inbox. Defaults to the caller's own primary department. |
| `GET` | `/api/departments/{departmentId}/inbox` | `DOCUMENT_VIEW` | Planned alias of the department inbox. |
| `GET` | `/api/movements/outbox?departmentId=` | `DOCUMENT_VIEW` | The documents a department sent out and that are still elsewhere. Defaults to the caller's own primary department. |
| `GET` | `/api/departments/{departmentId}/outbox` | `DOCUMENT_VIEW` | Planned alias of the department outbox. |
| `POST` | `/api/movements/{movementId}/send` | `DOCUMENT_EDIT` | Send the current step to the destination department. |
| `POST` | `/api/movements/{movementId}/receive` | `DOCUMENT_EDIT` | Receive the current step; refused until the destination department recorded its register number. |
| `POST` | `/api/movements/{movementId}/complete` | `DOCUMENT_EDIT` | Complete the current step. |
| `POST` | `/api/movements/{movementId}/reject` | `DOCUMENT_EDIT` | Reject the current step with a required reason; the rejection is routed back to the Central Diwan for administrative resolution. |
| `POST` | `/api/movements/{movementId}/return` | `DOCUMENT_EDIT` | Return the document to the sending department with a required reason. |
| `POST` | `/api/movements/{movementId}/steps/{stepId}/receive` | `DOCUMENT_EDIT` | Planned step-scoped alias for receive; the named step must be current. |
| `POST` | `/api/movements/{movementId}/steps/{stepId}/complete` | `DOCUMENT_EDIT` | Planned step-scoped alias for complete; the named step must be current. |
| `POST` | `/api/movements/{movementId}/steps/{stepId}/reject` | `DOCUMENT_EDIT` | Planned step-scoped alias for reject; the named step must be current. |
| `POST` | `/api/movements/{movementId}/steps/{stepId}/return` | `DOCUMENT_EDIT` | Planned step-scoped alias for return; the named step must be current. |
| `POST` | `/api/movements/{movementId}/redirect` | `DOCUMENT_EDIT` | Redirect the document to another department without closing the movement. |
| `POST` | `/api/movements/{movementId}/close` | `DOCUMENT_EDIT` | Close the movement and release the document. |
| `POST` | `/api/movements/{movementId}/cancel` | `DOCUMENT_EDIT` | Cancel the movement with a required reason and release the document. |

## Document file endpoints (Module 06)

| Method | Route | Permission | Behavior |
| --- | --- | --- | --- |
| `POST` | `/api/document-files` | `DOCUMENT_EDIT` | Open a file's version chain (`documentId`, `fileTypeId`, `title`, `isPrimaryScan`). Requires MANAGE access to the document. |
| `POST` | `/api/documents/{documentId}/files` | `DOCUMENT_EDIT` | Planned atomic initial upload (multipart `file`, `fileTypeId`, `title`, optional `isPrimaryScan`/`changeNotes`); creates the file and version 1 in one save. |
| `POST` | `/api/document-files/{fileId}/versions` | `DOCUMENT_EDIT` | Upload the next immutable version (multipart `file` + optional `changeNotes`). Validates extension, size, and magic bytes; stores the bytes with a streamed SHA-256 checksum. |
| `POST` | `/api/document-files/{fileId}/archive` | `DOCUMENT_EDIT` | Close the chain; existing versions stay readable. |
| `GET` | `/api/document-files/by-document/{documentId}` | `DOCUMENT_VIEW` | The document's files with their full version history. |
| `GET` | `/api/documents/{documentId}/files` | `DOCUMENT_VIEW` | Planned alias of the document file list. |
| `GET` | `/api/document-files/{fileId}/download?versionId=` | `DOCUMENT_VIEW` | Stream one version (current by default) under its original name/MIME type; requires DOWNLOAD access. |
| `GET` | `/api/document-files/{fileId}/verify?versionId=` | `DOCUMENT_VIEW` | Recompute the stored SHA-256 and return both checksums plus `isMatch`. |

## Paged list endpoints

`GET /api/documents`, `/api/users`, `/api/employees`, and `/api/external-organizations` accept
`page` (1-based, default 1), `pageSize` (default 20, clamped to 1–100), and `search`, and return:

```json
{ "items": [ ... ], "page": 1, "pageSize": 20, "totalCount": 42, "totalPages": 3,
  "hasPrevious": false, "hasNext": true }
```

`/api/documents` additionally accepts `sortBy` (`createdAt` default, `title`, `registrationDate`,
`officialNumber`), `sortDescending`, `statusId`, `owningDepartmentId`, `directionId`, and
`documentTypeId`. `/api/external-organizations` accepts `includeInactive`. `GET /api/departments/{departmentId}/documents`
is the same document page narrowed to one department's register and is authorized against the
caller's organizational scope (403 `Correspondence.DepartmentOutOfScope`). Bounded reference data
(lookups, permissions, roles, departments, registries, per-document grants/movements) still returns a
plain JSON array.

## Conventions / Notes

- `{lookup}` is a kebab-case slug registered in the Infrastructure `LookupCatalog` (33 slugs, e.g.
  `department-types`, `movement-statuses`, `audit-action-types`); an unknown slug is a 404.
- Class-level `[Authorize]` guards every route; write actions add a policy attribute, and the more
  restrictive policy wins because ASP.NET Core ANDs multiple `[Authorize]` attributes.
- `MediatR` handlers are `internal`; MediatR discovers them by assembly scan, so controllers only
  depend on the public command/query records.
- Every endpoint carries an `[EndpointSummary]`, an `[EndpointDescription]`, and a `<param>` block
  with a concrete example for each route/query parameter — `OpenApiDocumentationTests` fails the
  build otherwise. `add-endpoint-docs.mjs` at the repo root (run with Node) fills those in for a new
  endpoint and is idempotent: it skips anything already documented.

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-10-25 | opencode | Added the planned route aliases: document-scoped movement history, department-scoped inbox/outbox, step-scoped receive/complete/reject/return, and atomic `POST /api/documents/{documentId}/files`. Rejections now route to the Central Diwan and ordinary → ordinary movements are refused. |
| 2026-10-25 | opencode | Added `DocumentFilesController` for Module 06: create, upload version (multipart), archive, list by document, download, and checksum verification. |
| 2026-10-22 | Buffy | `AuthController.Me` now sends `GetUserProfileQuery` instead of reading claims inline, and `DocumentsController` gained `GET /api/departments/{departmentId}/documents` (one department's paged register, scope-checked). |
| 2026-10-22 | opencode | Turned the four unbounded list endpoints into paged queries: `GET /api/documents` (paging, search, sort, and status/department/direction/type filters), `GET /api/users` and `GET /api/employees` (paging + search), and `GET /api/external-organizations` (paging + search + `includeInactive`). All four now return `PagedResult<T>`. |
| 2026-10-21 | opencode | Rewrote `MovementsController` into the full routing workflow: open/send/receive/complete/reject/return/redirect/close/cancel, the movement read endpoints (`GET {movementId}`, `GET by-document/{documentId}`), and the department inbox and outbox (`GET inbox`, `GET outbox`). |
| 2026-10-20 | Buffy | Added the department register routes: `POST /api/documents/{id}/numbers/department-registration` and `GET .../registry-entries` on `DocumentNumbersController`, `DepartmentRegistersController`, and `MovementsController` (`{id}/receive`, gated on a recorded register entry). |
| 2026-10-19 | Buffy | Added `InstitutionSettingsController` (`GET`/`PUT /api/institution-settings`), `ExternalOrganizationsController` (CRUD), `DocumentNumbersController` (issue/cancel/replace/history), the document lifecycle endpoints on `DocumentsController` (drafts, update, register, participants, cancel, details, timeline), department-scope + effective-permissions endpoints on `UsersController`, and registry write endpoints on `NumberingRegistriesController`. |
| 2026-10-19 | opencode | Added `LookupsController` and documented the folder's ten controllers, their routes and their permission policies (this README did not exist before). |
