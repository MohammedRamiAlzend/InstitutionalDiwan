# InstitutionalDiwan.Server — Web API / Presentation Layer

> **Status:** Implemented — `Program.cs` wires DI, auth, MediatR, and the exception middleware;
> `Auth`, `Documents`, and `Users` controllers serve the live endpoints.

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
| `Controllers/` | REST controllers, one per module | ✅ `AuthController`, `DocumentsController`, `UsersController` |
| [`Contracts/`](Contracts/README.md) | HTTP request/response DTO records grouped by module | 🟡 `CreateDocumentRequest` + Identity/Users records live |
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
| Controllers | ✅ `Auth`, `Documents` (incl. 409 on duplicate official number), `Users` |
| Contracts / Middleware / Extensions | 🟡 Partially implemented |
| DI for Application/Infrastructure | ✅ Wired |
| Authentication / Authorization setup | ✅ JWT + permission policies |
| SignalR | ❌ Not wired |
| `InstitutionalDiwan.Server.http` | ⚠️ Still the sample `weatherforecast` request |

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-10-18 | opencode | `POST /api/documents` now requires `OfficialNumberText` + `NumberingYear` (409 Conflict when the pair is taken); `GlobalExceptionHandlingMiddleware` maps SQL unique-key violations to 409; refreshed stale status tables. |
