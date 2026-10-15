# InstitutionalDiwan.Server — Web API / Presentation Layer

> **Status:** Default web template. Only `Program.cs` exists; there are no controllers or middleware yet.

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
| `Controllers/` | REST controllers, one per module | ❌ Folder **does not exist yet** |
| [`Contracts/`](Contracts/README.md) | HTTP request/response DTO records grouped by module | ❌ Empty (docs only) |
| [`Middleware/`](Middleware/README.md) | Global exception handling, correlation-id tracking, audit IP capture | ❌ Empty (docs only) |
| [`Extensions/`](Extensions/README.md) | `Result` → HTTP mapping, DI registration, Swagger, auth setup | ❌ Empty (docs only) |

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
| `Program.cs` | ⚠️ Template only |
| Controllers | ❌ None |
| Contracts / Middleware / Extensions | ❌ None (docs only) |
| DI for Application/Infrastructure | ❌ Not wired |
| Authentication / Authorization setup | ❌ Not wired |
| SignalR | ❌ Not wired |
| `InstitutionalDiwan.Server.http` | ⚠️ Still the sample `weatherforecast` request |
