# InstitutionalDiwan.Server — Web API / Presentation Layer

> **Status:** Implemented — `Program.cs` wires DI, auth, MediatR, and the exception middleware;
> `Auth`, `Documents`, `Users`, `Roles`, `Permissions`, `Departments`, `Employees`,
> `DocumentAccessGrants`, and `NumberingRegistries` controllers serve the live endpoints.

## Purpose

Exposes the InstitutionalDiwan system over HTTP/REST (and, later, SignalR) while staying **thin**.
Controllers must not contain business logic or query `DbContext` directly — they send a Command/Query
via `ISender` (MediatR) and map the returned `Result` / `Result<T>` to a standardized HTTP response or
RFC 7807 `ProblemDetails`.

The project is named `NewDiwan.Presentation` in the older design notes; the actual assembly is
`InstitutionalDiwan.Server`.

## Subfolders

| Folder | Responsibility | Code status |
| --- | --- | --- |
| `Controllers/` | REST controllers, one per module | ✅ `AuthController`, `DocumentsController`, `UsersController`, `RolesController`, `PermissionsController`, `DepartmentsController`, `EmployeesController`, `DocumentAccessGrantsController`, `NumberingRegistriesController` |
| [`Contracts/`](Contracts/README.md) | HTTP request/response DTO records grouped by module | 🟡 `CreateDocumentRequest` + Identity + Organization/`Departments` records live |
| [`Middleware/`](Middleware/README.md) | Global exception handling, correlation-id tracking, audit IP capture | 🟡 `GlobalExceptionHandlingMiddleware` live (409 on unique-key violations) |
| [`Extensions/`](Extensions/README.md) | `Result` → HTTP mapping, DI registration, Swagger, auth setup | ✅ `ResultExtensions` (ErrorType → status code incl. Conflict → 409), auth/Swagger setup |

## Current `Program.cs`

The pipeline today is the default template:

```csharp
builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();
app.UseDefaultFiles();
app.MapStaticAssets();
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();   // Scalar UI
}
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.MapFallbackToFile("/index.html");   // SPA fallback
app.Run();
```

Notably **not** present yet: authentication/authorization policies, CORS, the global exception
middleware, correlation-id middleware, MediatR, FluentValidation, the `DbContext` registration, and
the Application/Infrastructure DI entry points.

## Hosting & the SPA

The project is an ASP.NET Core SPA host: `SpaProxyServerUrl` is `https://localhost:61325`, launched
with `npm run dev` from `../institutionaldiwan.client`, which is skipped only when the front end is
built. HTTP profiles listen on `http://localhost:5028` and `https://localhost:7126`.

> The front-end client is **out of scope** for the current backend work.

## Package footprint

`InstitutionalDiwan.Server.csproj` references `Microsoft.AspNetCore.OpenApi`, `Microsoft.AspNetCore.SpaProxy`,
and `Scalar.AspNetCore`, and project-references Application, Infrastructure, and the client `.esproj`.

## Implementation status

| Item | State |
| --- | --- |
| `Program.cs` | ✅ Wired (DI, MediatR, FluentValidation, auth, exception middleware, SPA host) |
| Controllers | ✅ `Auth`, `Documents` (incl. 409 on duplicate official number), `Users`, `Roles` (CRUD + activate + grant/revoke permission), `Permissions` (catalog), `Departments` (CRUD + reparent + tree, soft delete), `Employees` (CRUD + deactivate/activate), `DocumentAccessGrants` (list/grant/revoke per document), `NumberingRegistries` |
| Contracts / Middleware / Extensions | 🟡 Partially implemented |
| DI for Application/Infrastructure | ✅ Wired |
| Authentication / Authorization setup | ✅ JWT + permission policies |
| SignalR | ❌ Not wired |
| `InstitutionalDiwan.Server.http` | ⚠️ Still the sample `weatherforecast` request |

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-10-18 | opencode | Completed `UsersController` (`GET` list/by-id, `POST` create, `PUT` update, `DELETE` deactivate, `POST /activate`, `POST`/`DELETE …/roles/{roleId}`), all protected by `USER_MANAGE`; added `CreateUserRequest`/`UpdateUserRequest`/`AssignUserRoleRequest` contracts. |
| 2026-10-18 | opencode | Added `EmployeesController` (`GET` list/by-id, `POST` create, `PUT` update, `DELETE` deactivate, `POST /activate`) protected by `EMPLOYEE_MANAGE`, and `DocumentAccessGrantsController` (`GET`/`POST`/`DELETE` under `/api/documents/{documentId}/access-grants`) protected by `ACCESS_GRANT_MANAGE`; registered `IEmployeeRepository`/`IDocumentAccessGrantRepository`. |
| 2026-10-18 | opencode | Added `RolesController` (`GET` list/by-id, `POST` create, `PUT` update, `DELETE` soft delete, `POST /activate`, `POST`/`DELETE …/permissions`) and `PermissionsController` (`GET` catalog), all protected by `ROLE_MANAGE`. |
| 2026-10-18 | opencode | Added `PUT /api/departments/{id}/parent` to reparent a department; the handler validates the whole subtree so a move cannot create a hierarchy cycle (409 `Department.CyclicHierarchy`). |
| 2026-10-18 | opencode | Added `DepartmentsController` (`GET` list/tree/by-id, `POST` create, `PUT` update, `DELETE` soft delete, `POST /activate`) protected by `DEPARTMENT_MANAGE`, and `NumberingRegistriesController` (`GET` list/by-id) protected by `NUMBERING_VIEW`. |
| 2026-10-18 | opencode | `POST /api/documents` now requires `OfficialNumberText` + `NumberingYear` (409 Conflict when the pair is taken); `GlobalExceptionHandlingMiddleware` maps SQL unique-key violations to 409; refreshed stale status tables. |
