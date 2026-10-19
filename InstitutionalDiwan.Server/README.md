# InstitutionalDiwan.Server — Web API / Presentation Layer

> **Status:** Implemented — `Program.cs` wires DI, auth, MediatR, and the exception middleware; the
> `Auth`, `Documents`, `Users`, `Roles`, `Permissions`, `Departments`, `Employees`,
> `DocumentAccessGrants`, `NumberingRegistries`, and `Lookups` controllers serve the live endpoints.

## Purpose

Exposes the InstitutionalDiwan system over HTTP/REST (and, later, SignalR) while staying **thin**.
Controllers must not contain business logic or query `DbContext` directly — they send a Command/Query
via `ISender` (MediatR) and map the returned `Result` / `Result<T>` to a standardized HTTP response or
RFC 7807 `ProblemDetails`.

Earlier design notes called the project `NewDiwan.Presentation`; the actual assembly and namespace are
`InstitutionalDiwan.Server`.

## Subfolders

| Folder | Responsibility | Code status |
| --- | --- | --- |
| [`Controllers/`](Controllers/README.md) | REST controllers, one per module (plus the generic lookups controller) | ✅ 10 controllers live |
| [`Contracts/`](Contracts/README.md) | HTTP request/response DTO records grouped by module | ✅ Correspondence, Organization, Identity, Lookups |
| [`Middleware/`](Middleware/README.md) | Global exception handling, correlation-id tracking, audit IP capture | 🟡 `GlobalExceptionHandlingMiddleware` live (409 on unique-key violations) |
| [`Extensions/`](Extensions/README.md) | `Result` → HTTP mapping, DI registration, OpenAPI/Scalar, auth setup | ✅ `ResultExtensions`, `DatabaseExtensions`, `AuthenticationExtensions`, `OpenApiExtensions` |

## Current `Program.cs`

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddApplication();                              // MediatR + validators + behaviors
builder.Services.AddInfrastructure(builder.Configuration);      // DbContext + repositories + seeders + auth services
builder.Services.AddInstitutionalOpenApi();
builder.Services.AddInstitutionalAuthentication(builder.Configuration);  // JWT + one policy per permission
builder.Services.AddOpenApi("v1", options => options.AddDocumentTransformer<BearerSecuritySchemeTransformer>());

var app = builder.Build();

app.UseMiddleware<GlobalExceptionHandlingMiddleware>();
app.UseDefaultFiles();
app.MapStaticAssets();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}
else
{
    app.UseHttpsRedirection();
}

await app.InitializeDatabaseAsync();   // migrate (Development) + ordered, environment-gated seeding

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapFallbackToFile("/index.html");  // SPA fallback
app.Run();
```

Wired today: authentication/authorization policies, the global exception middleware, MediatR,
FluentValidation, the `DbContext`, and the Application/Infrastructure DI entry points. Not yet wired:
CORS, correlation-id middleware, SignalR.

## Hosting & the SPA

The project is an ASP.NET Core SPA host: `SpaProxyServerUrl` is `https://localhost:61325`, launched
with `npm run dev` from `../institutionaldiwan.client`, which is skipped only when the front end is
built. HTTP profiles listen on `http://localhost:5028` and `https://localhost:7126`.

> The front-end client is **out of scope** for the current backend work.

## Package footprint

`InstitutionalDiwan.Server.csproj` references `Microsoft.AspNetCore.Authentication.JwtBearer`,
`Microsoft.AspNetCore.OpenApi`, `Microsoft.AspNetCore.SpaProxy`, `Microsoft.EntityFrameworkCore.Design`,
and `Scalar.AspNetCore`, and project-references Application, Infrastructure, and the client `.esproj`.

## Implementation status

| Item | State |
| --- | --- |
| `Program.cs` | ✅ Wired (DI, MediatR, FluentValidation, auth, exception middleware, startup seeding, SPA host) |
| Controllers | ✅ `Auth`, `Documents` (incl. 409 on duplicate official number), `Users`, `Roles` (CRUD + activate + grant/revoke permission), `Permissions` (catalog), `Departments` (CRUD + reparent + tree, soft delete), `Employees` (CRUD + deactivate/activate), `DocumentAccessGrants` (list/grant/revoke per document), `NumberingRegistries`, `Lookups` (generic reference catalogs: list/get/create/relabel/activate/deactivate) |
| Contracts / Middleware / Extensions | 🟡 Contracts complete for the live modules; `GlobalExceptionHandlingMiddleware` in place; extensions implemented |
| DI for Application/Infrastructure | ✅ Wired |
| Authentication / Authorization setup | ✅ JWT + permission policies |
| SignalR | ❌ Not wired |
| `InstitutionalDiwan.Server.http` | ⚠️ Still the sample `weatherforecast` request |

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-10-19 | opencode | `BearerSecuritySchemeTransformer` now guards a null `document.Paths` (clears the pre-existing `CS8602` nullable warning so the solution builds with 0 warnings). |
| 2026-10-19 | opencode | Added `LookupsController` (`GET/POST/PUT/PATCH` under `/api/lookups/{lookup}`, reads authenticated, writes guarded by the new `LOOKUP_MANAGE` permission) and the `Contracts/Lookups` records; replaced the README's stale "default template" `Program.cs` snippet and status tables with the real pipeline. |
| 2026-10-18 | opencode | Completed `UsersController` (`GET` list/by-id, `POST` create, `PUT` update, `DELETE` deactivate, `POST /activate`, `POST`/`DELETE …/roles/{roleId}`), all protected by `USER_MANAGE`; added `CreateUserRequest`/`UpdateUserRequest`/`AssignUserRoleRequest` contracts. |
| 2026-10-18 | opencode | Added `EmployeesController` (`GET` list/by-id, `POST` create, `PUT` update, `DELETE` deactivate, `POST /activate`) protected by `EMPLOYEE_MANAGE`, and `DocumentAccessGrantsController` (`GET`/`POST`/`DELETE` under `/api/documents/{documentId}/access-grants`) protected by `ACCESS_GRANT_MANAGE`; registered `IEmployeeRepository`/`IDocumentAccessGrantRepository`. |
| 2026-10-18 | opencode | Added `RolesController` (`GET` list/by-id, `POST` create, `PUT` update, `DELETE` soft delete, `POST /activate`, `POST`/`DELETE …/permissions`) and `PermissionsController` (`GET` catalog), all protected by `ROLE_MANAGE`. |
| 2026-10-18 | opencode | Added `PUT /api/departments/{id}/parent` to reparent a department; the handler validates the whole subtree so a move cannot create a hierarchy cycle (409 `Department.CyclicHierarchy`). |
| 2026-10-18 | opencode | Added `DepartmentsController` (`GET` list/tree/by-id, `POST` create, `PUT` update, `DELETE` soft delete, `POST /activate`) protected by `DEPARTMENT_MANAGE`, and `NumberingRegistriesController` (`GET` list/by-id) protected by `NUMBERING_VIEW`. |
| 2026-10-18 | opencode | `POST /api/documents` now requires `OfficialNumberText` + `NumberingYear` (409 Conflict when the pair is taken); `GlobalExceptionHandlingMiddleware` maps SQL unique-key violations to 409; refreshed stale status tables. |
