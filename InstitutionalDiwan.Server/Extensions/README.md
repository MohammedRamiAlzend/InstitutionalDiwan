# Presentation / Extensions

> **Status:** Target design — **no code yet**.

## Purpose

Clean C# extension methods and DI registration helpers that keep `Program.cs` small and the API layer
consistently shaped.

## Planned components

### `ResultExtensions.cs`
Maps `Result` and `Result<T>` to `IActionResult` / `IResult` (RFC 7807 `ProblemDetails`), translating
`ErrorType` to HTTP status codes:

| `ErrorType` | HTTP status |
| --- | --- |
| `Validation` | `400 Bad Request` |
| `Unauthorized` | `401 Unauthorized` |
| `Forbidden` | `403 Forbidden` |
| `NotFound` | `404 Not Found` |
| `Conflict` | `409 Conflict` |
| `Failure` | `500 Internal Server Error` |

### `MigrationExtensions.cs`
Applies pending EF Core migrations and runs the `NewDiwanDbContextSeeder` during development startup —
a single call instead of manual `dotnet ef database update`.

### `SwaggerExtensions.cs`
Configures OpenAPI/Swagger (the host also maps **Scalar** today) with JWT Bearer authentication support
and Arabic/English operation summaries.

### DI registration
An `AddApplication()` / `AddInfrastructure()` pair to register MediatR handlers, pipeline behaviors,
the `DbContext`, repositories, and the abstraction implementations — the missing links that wire
Application and Infrastructure into the host.

## Mapping table used with the domain

`Result`/`Error`/`ErrorType` are defined in
[`../../InstitutionalDiwan.Domain/Common/`](../../InstitutionalDiwan.Domain/README.md), so the mapping
here is the single place that turns domain outcomes into HTTP — controllers should never branch on
error types themselves.

## Implementation status

| Component | State |
| --- | --- |
| `ResultExtensions` | ❌ Not implemented |
| `MigrationExtensions` | ❌ Not implemented |
| `SwaggerExtensions` | ❌ Not implemented |
| `AddApplication` / `AddInfrastructure` DI registration | ❌ Not implemented |

`Program.cs` calls `AddOpenApi()` and `MapScalarApiReference()` directly instead of a
`SwaggerExtensions` helper.
