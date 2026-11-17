# InstitutionalDiwan.Server — Web API / Presentation Layer

> **Status:** Implemented — `Program.cs` wires DI, auth, MediatR, request-context logging, action
> auditing, and the exception middleware; the `Auth`, `Documents`, `DocumentNumbers`, `ExternalOrganizations`,
> `InstitutionSettings`, `Users`, `Roles`, `Permissions`, `Departments`, `Employees`,
> `DocumentAccessGrants`, `NumberingRegistries`, `DepartmentRegisters`, `Movements` (routing workflow
> + inbox/outbox), `DocumentFiles` (Module 06 attachments), `DocumentArchive` and `SavedSearches`
> (Module 07), `AuditLogs` (Module 09), and `Lookups`
> controllers serve the live endpoints.

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
| [`Controllers/`](Controllers/README.md) | REST controllers, one per module (plus the generic lookups controller) | ✅ 17 controllers live |
| [`Contracts/`](Contracts/README.md) | HTTP request/response DTO records grouped by module | ✅ Correspondence, Organization, Identity, Numbering, Movements, Attachments, Audit, Lookups |
| [`Middleware/`](Middleware/README.md) | Global exception handling, correlation-id tracking, action-level audit, forced-password recovery gate, security headers | Implemented: request context, global exceptions, action auditing, `ForcedPasswordChangeMiddleware`, `SecurityHeadersMiddleware`. |
| [`Extensions/`](Extensions/README.md) | `Result` → HTTP mapping, DI registration, OpenAPI/Scalar, auth setup, per-address rate limiting | ✅ `ResultExtensions`, `DatabaseExtensions`, `AuthenticationExtensions`, `OpenApiExtensions`, `RateLimitingExtensions` |
| [`Properties/`](Properties/README.md) | Launch profiles plus the `Http-4567` / `Https-5678` publish profiles (SPA build, `dist` → `wwwroot`, Kestrel binding) | ✅ Both publish profiles verified end to end |

## Current `Program.cs`

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.AddInstitutionalJwtSigningKey();   // Jwt:SecretKey from config, or generated once under keys/
builder.Services.AddControllers();
builder.Services.AddApplication();                              // MediatR + validators + behaviors
builder.Services.AddInfrastructure(builder.Configuration);      // DbContext + repositories + seeders + auth services
builder.Services.AddInstitutionalOpenApi();
builder.Services.AddInstitutionalRateLimiting();               // per-address windows on login (30/min) and register (5/hour)
builder.Services.AddInstitutionalAuthentication(builder.Configuration);  // JWT + one policy per permission
builder.Services.AddOpenApi("v1", options => options.AddDocumentTransformer<BearerSecuritySchemeTransformer>());

var app = builder.Build();
if (jwtSigningKey.Generated) app.Logger.LogWarning("generated JWT key at {KeyPath}", jwtSigningKey.KeyPath);

app.UseMiddleware<RequestContextLoggingMiddleware>();   // correlation id + IP + logging scope
app.UseMiddleware<GlobalExceptionHandlingMiddleware>();
app.UseMiddleware<SecurityHeadersMiddleware>();   // nosniff, frame denial, no-referrer, HSTS (non-dev), no-store on /api/auth
app.UseRateLimiter();   // 429 (ProblemDetails) for the anonymous auth endpoints over their window

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}
else
{
    // Redirect only when this instance terminates TLS itself; the plain-HTTP publish profile
    // binds http://0.0.0.0:4567, where a redirect has no target.
    var urls = app.Configuration["Urls"] ?? Environment.GetEnvironmentVariable("ASPNETCORE_URLS") ?? "";
    if (urls.Contains("https://", StringComparison.OrdinalIgnoreCase))
    {
        app.UseHttpsRedirection();
    }
}

await app.InitializeDatabaseAsync();   // migrate (Development) + ordered, environment-gated seeding

app.UseAuthentication();
app.UseMiddleware<AuditActionMiddleware>();   // one Operation audit row per authenticated /api action
app.UseMiddleware<ForcedPasswordChangeMiddleware>();   // only me/change-password or anonymous while forced
app.UseAuthorization();
app.MapControllers();
app.Run();
```

Wired today: authentication/authorization policies, request-context logging, the global exception
middleware, action-level auditing, MediatR, FluentValidation, the `DbContext`, SignalR
(`/hubs/notifications`), static-file hosting of the published SPA, and the Application/Infrastructure
DI entry points. Not yet wired: CORS.

## Hosting

Two modes are supported out of the box.

**Development (local, API only).** `dotnet run` uses `Properties/launchSettings.json`:
`http://localhost:5028` and `https://localhost:7126`, Scalar at `/scalar/v1`. `UseDefaultFiles` /
`UseStaticFiles` / `MapFallbackToFile("/index.html")` are wired, so anything present in `wwwroot`
is served, but in day-to-day development the SPA runs on Vite (`:5173`) and proxies `/api` and
`/hubs` to this host.

**Publish (API + SPA in one process).** The folder publish profiles in
[`Properties/`](Properties/README.md) build `institutionaldiwan.front`, copy `dist/` into `wwwroot`,
and seed the Kestrel binding:

| Profile | Kestrel binding | Output folder | Certificate |
| --- | --- | --- | --- |
| `Http-4567` | `http://0.0.0.0:4567` | `publish/Http-4567/` | — |
| `Https-5678` | `https://0.0.0.0:5678` | `publish/Https-5678/` | PFX via `ASPNETCORE_Kestrel__Certificates__Default__*` |

```bash
dotnet publish InstitutionalDiwan.Server -c Release -p:PublishProfile=Http-4567
dotnet publish InstitutionalDiwan.Server -c Release -p:PublishProfile=Https-5678
```

Each output folder also gets a launcher — `start-http.cmd` / `start-https.cmd` — that exports the
runtime environment (binding, `Jwt__SigningKeyPath` under `%ProgramData%`, and for HTTPS the
certificate from `%ProgramData%\InstitutionalDiwan\diwan.pfx`) and starts the app; details in
[`Properties/README.md`](Properties/README.md#start-scripts).

The SPA is compiled with the relative `/api` and `/hubs` bases, so it talks to the same origin it is
served from — no CORS policy is needed. A plain `dotnet publish` without a profile stays API-only.

`Jwt:SecretKey` is resolved at startup: an explicit value (`Jwt__SecretKey`, user-secrets) wins,
otherwise a random key is generated once and stored at `Jwt:SigningKeyPath`
(default `keys/jwt-signing.key` next to the app) so restarts and redeployments keep the same signer.
The key is never part of the publish output; back up that folder with the deployment. Set the
production connection string in the service environment.

## Package footprint

`InstitutionalDiwan.Server.csproj` references `Microsoft.AspNetCore.Authentication.JwtBearer`,
`Microsoft.AspNetCore.OpenApi`, `Microsoft.EntityFrameworkCore.Design`, and `Scalar.AspNetCore`, and
project-references Application and Infrastructure.

## Implementation status

| Item | State |
| --- | --- |
| `Program.cs` | ✅ Wired (DI, MediatR, FluentValidation, auth, action auditing, exception middleware, startup seeding, API host) |
| Controllers | ✅ `Auth`, `Documents` (drafts, update, register, participants, cancel, details, timeline, plus create with official number), `DocumentNumbers` (issue/cancel/replace/history + department register entries), `DepartmentRegisters` (register book), `Movements` (routing workflow — open/send/receive/complete/reject/return/redirect/close/cancel — plus the movement reads and the department inbox/outbox), `ExternalOrganizations` (CRUD), `InstitutionSettings` (GET/PUT), `Users` (CRUD + roles + department-assignments + effective-permissions), `Roles` (CRUD + activate + grant/revoke permission), `Permissions` (catalog), `Departments` (CRUD + reparent + tree, soft delete), `Employees` (CRUD + deactivate/activate), `DocumentAccessGrants` (list/grant/revoke per document), `NumberingRegistries` (CRUD + activate/deactivate), `DocumentFiles` (Module 06: create/upload/archive/list/download/verify), `AuditLogs` (Module 09: search/checkpoint/integrity), `Lookups` (generic reference catalogs), `UiLabels` (editable UI terminology: read for all, write/reset superadmin-only) |
| Contracts / Middleware / Extensions | ✅ Contracts complete for the live modules; request-context + global exception middleware in place; extensions implemented |
| DI for Application/Infrastructure | ✅ Wired |
| Authentication / Authorization setup | Implemented: live JWT account/session-stamp validation, permission policies, the `UI_LABELS_MANAGE` policy (SUPER_ADMIN role + `superadmin` account name), and endpoint-aware forced-password recovery enforcement. |
| JWT signing key | ✅ Configured key wins; otherwise generated once and persisted at `Jwt:SigningKeyPath` (`keys/jwt-signing.key`), never shipped in the publish output |
| SignalR | ✅ `/hubs/notifications` (`NotificationsHub`), token via `access_token`, wired in `Program.cs`; single instance, no backplane |
| Publish profiles | ✅ `Properties/PublishProfiles/Http-4567.pubxml` and `Https-5678.pubxml` — SPA build + `dist` → `wwwroot` + Kestrel binding; verified end to end |
| `InstitutionalDiwan.Server.http` | ✅ Sample `GET /api/audit-logs` request (replace `@token` with a valid JWT) |

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-11-17 | Qoder | UI labels, Phase 0 (`plans/ui-labels-edit-mode.md`): `UiLabelsController` + `Ui/SetUiLabelOverrideRequest` + the `UI_LABELS_MANAGE` policy registered in `AuthenticationExtensions` (SUPER_ADMIN role and the `superadmin` account name, so the right is not delegable through role management). |
| 2026-11-15 | opencode | Publishing: each profile now also writes `start-http.cmd` / `start-https.cmd` into the output folder (environment, binding, `Jwt__SigningKeyPath` under `%ProgramData%`, certificate loaded from `%ProgramData%\InstitutionalDiwan\diwan.pfx` for HTTPS). |
| 2026-11-15 | opencode | Auth: `Jwt:SecretKey` is now optional at startup — `AddInstitutionalJwtSigningKey` uses the configured key when present, otherwise generates a 64-byte random key once and persists it at `Jwt:SigningKeyPath` (default `keys/jwt-signing.key`, git-ignored), so the published artifact carries no signing secret and redeployments do not rotate it. |
| 2026-11-15 | opencode | Publishing: added the `Http-4567` (`http://0.0.0.0:4567`) and `Https-5678` (`https://0.0.0.0:5678`) folder publish profiles. On publish they run `npm ci`/`npm run build`, copy `institutionaldiwan.front/dist` into `wwwroot`, add it to the publish output, and seed `appsettings.Production.json` with the Kestrel `Urls` binding; the SPA is compiled with the relative `/api` and `/hubs` bases. `UseHttpsRedirection` now only runs when the instance has an `https://` binding. Documented in `Properties/README.md` and refreshed the stale Hosting/SignalR sections. |
| 2026-11-15 | Qoder | Security: added `SecurityHeadersMiddleware` (nosniff, frame denial, no-referrer, HSTS outside Development, no-store on `/api/auth`) and suppressed the Kestrel `Server` header. |
| 2026-11-15 | Qoder | Security: added per-address rate limiting on the anonymous auth endpoints (`RateLimitingExtensions`) — login 30/min and register 5/hour per address, excess rejected with a 429 `ProblemDetails`; `UseRateLimiter` sits right after the exception middleware. |
| 2026-11-13 | Claude | Targets `net11.0`: JwtBearer, OpenApi and EF Core Design on 11.0.0, `Scalar.AspNetCore` on 2.21.0. The API starts, migrates and serves `/openapi/v1.json` and `/scalar/v1`. |
| 2026-11-03 | Claude | Module 10: added `NotificationsController`, the SignalR hub `/hubs/notifications` (token via `access_token` query string, connection closed when the token expires) and the `IRealtimeNotifier` binding. The hub is single-instance: no backplane, so a multi-instance deployment needs Redis or Azure SignalR and one outbox processor. |
| 2026-11-03 | Claude | Module 07 Archiving, Phase 3: archive and saved search endpoints, and concurrency conflicts now answer 409. |
| 2026-11-02 | Claude | Dynamic document fields, Phase 3: `DocumentFieldsController` with 14 routes, field contracts, `fieldValues` on document creation. |
| 2026-10-28 | Qoder | Wire forced-password enforcement between action auditing and authorization; add live JWT session revocation with no legacy stamp fallback, migrations, or signing-key configuration edits. |
| 2026-10-26 | Qoder | Removed the SPA integration: dropped `SpaRoot`/`SpaProxy` and the client project reference from the csproj, removed the SPA proxy hosting startup from the launch profiles, and deleted `UseDefaultFiles`, `MapStaticAssets`, and `MapFallbackToFile` from `Program.cs`. The project is now an API-only host; the front end lives in the standalone `institutionaldiwan.front` project. |
| 2026-10-25 | Buffy | Replaced the leftover template `weatherforecast` sample request with a real `GET /api/audit-logs` request. |
| 2026-10-25 | opencode | Added the Module 09 `AuditLogsController`, audit contracts, and `RequestContextLoggingMiddleware`; updated the `Program.cs` excerpt and status tables. |
| 2026-10-25 | opencode | Added `DocumentFilesController` and the Attachments contracts, exposing Module 06: create file, upload a validated version with streamed SHA-256, archive, list by document, download, and checksum verification. |
| 2026-10-21 | opencode | Rewrote `MovementsController` into the full routing workflow: `POST /api/movements`, `GET /api/movements/{movementId}`, `GET /api/movements/by-document/{documentId}`, `GET /api/movements/inbox`, `GET /api/movements/outbox`, and the `{movementId}/send|receive|complete|reject|return|redirect|close|cancel` transitions. Reads take `DOCUMENT_VIEW`, writes `DOCUMENT_EDIT`; the inbox/outbox default to the caller's own primary department and accept another one only inside the caller's organizational scope (403 otherwise). |
| 2026-10-20 | Buffy | Exposed department register numbering: `POST /api/documents/{id}/numbers/department-registration`, `GET .../registry-entries`, `GET /api/departments/{id}/register`, and `POST /api/movements/{id}/receive` (409 unless the department recorded its register number first). |
| 2026-10-19 | Buffy | Completed the Module 01–04 endpoints: added `InstitutionSettingsController`, `ExternalOrganizationsController`, `DocumentNumbersController`, the document lifecycle routes on `DocumentsController`, department-scope + effective-permissions routes on `UsersController`, registry write routes on `NumberingRegistriesController`, and their `Contracts/` records; each controller declares its `NUMBERING_MANAGE` / `DOCUMENT_EDIT` / `DEPARTMENT_MANAGE` policy explicitly. |
| 2026-10-19 | opencode | `BearerSecuritySchemeTransformer` now guards a null `document.Paths` (clears the pre-existing `CS8602` nullable warning so the solution builds with 0 warnings). |
| 2026-10-19 | opencode | Added `LookupsController` (`GET/POST/PUT/PATCH` under `/api/lookups/{lookup}`, reads authenticated, writes guarded by the new `LOOKUP_MANAGE` permission) and the `Contracts/Lookups` records; replaced the README's stale "default template" `Program.cs` snippet and status tables with the real pipeline. |
| 2026-10-18 | opencode | Completed `UsersController` (`GET` list/by-id, `POST` create, `PUT` update, `DELETE` deactivate, `POST /activate`, `POST`/`DELETE …/roles/{roleId}`), all protected by `USER_MANAGE`; added `CreateUserRequest`/`UpdateUserRequest`/`AssignUserRoleRequest` contracts. |
| 2026-10-18 | opencode | Added `EmployeesController` (`GET` list/by-id, `POST` create, `PUT` update, `DELETE` deactivate, `POST /activate`) protected by `EMPLOYEE_MANAGE`, and `DocumentAccessGrantsController` (`GET`/`POST`/`DELETE` under `/api/documents/{documentId}/access-grants`) protected by `ACCESS_GRANT_MANAGE`; registered `IEmployeeRepository`/`IDocumentAccessGrantRepository`. |
| 2026-10-18 | opencode | Added `RolesController` (`GET` list/by-id, `POST` create, `PUT` update, `DELETE` soft delete, `POST /activate`, `POST`/`DELETE …/permissions`) and `PermissionsController` (`GET` catalog), all protected by `ROLE_MANAGE`. |
| 2026-10-18 | opencode | Added `PUT /api/departments/{id}/parent` to reparent a department; the handler validates the whole subtree so a move cannot create a hierarchy cycle (409 `Department.CyclicHierarchy`). |
| 2026-10-18 | opencode | Added `DepartmentsController` (`GET` list/tree/by-id, `POST` create, `PUT` update, `DELETE` soft delete, `POST /activate`) protected by `DEPARTMENT_MANAGE`, and `NumberingRegistriesController` (`GET` list/by-id) protected by `NUMBERING_VIEW`. |
| 2026-10-18 | opencode | `POST /api/documents` now requires `OfficialNumberText` + `NumberingYear` (409 Conflict when the pair is taken); `GlobalExceptionHandlingMiddleware` maps SQL unique-key violations to 409; refreshed stale status tables. |
