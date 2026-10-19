# Presentation / Extensions

> **Status:** Implemented — `ResultExtensions`, `DatabaseExtensions`, `AuthenticationExtensions`, and
> `OpenApiExtensions` are live and called from `Program.cs`.

## Purpose

Clean C# extension methods and DI registration helpers that keep `Program.cs` small and the API layer
consistently shaped.

## Contents

| File | Purpose |
| --- | --- |
| `ResultExtensions.cs` | Maps `Result` / `Result<T>` to `IActionResult`, turning `ErrorType` into an HTTP status code and an RFC 7807 `ProblemDetails` body. The single place where domain outcomes become HTTP. |
| `DatabaseExtensions.cs` | `InitializeDatabaseAsync`: applies pending migrations in Development (other environments only seed when the database is reachable) and then runs the registered `ISeeder`s through `SeederRunner`, ordered by `Order` and filtered by each seeder's declared environments. |
| `AuthenticationExtensions.cs` | `AddInstitutionalAuthentication`: JWT bearer setup (issuer/audience/key from configuration) plus one authorization policy per `Permissions.All` entry and the `PermissionAuthorizationHandler`. |
| `OpenApiExtensions.cs` | `AddInstitutionalOpenApi`: registers the OpenAPI document, the Bearer security scheme transformer, and the Scalar API reference used in Development. |

## Error → HTTP mapping

| `ErrorType` | HTTP status |
| --- | --- |
| `Validation` | `400 Bad Request` |
| `Unauthorized` | `401 Unauthorized` |
| `Forbidden` | `403 Forbidden` |
| `NotFound` | `404 Not Found` |
| `Conflict` | `409 Conflict` |
| `Failure` | `500 Internal Server Error` |

The `ProblemDetails` body carries the domain error code as `Title` and its description as `Detail`
(for example `Lookup.EntryInUse` / 409), so controllers never branch on error types themselves.

## DI registration

- `Application/DependencyInjection.AddApplication()` — MediatR handlers, validators, pipeline behaviors.
- `Infrastructure/DependencyInjection.AddInfrastructure()` — `DbContext`, repositories, seeders, auth services.
- `AuthenticationExtensions` / `OpenApiExtensions` — auth policies and OpenAPI/Scalar above.
- `Program.cs` composes them: `AddControllers` → `AddApplication` → `AddInfrastructure` →
  `AddInstitutionalOpenApi` → `AddInstitutionalAuthentication` → `AddOpenApi("v1", …)`.

## Implementation status

| Component | State |
| --- | --- |
| `ResultExtensions` | ✅ Implemented (ErrorType → status code incl. Conflict → 409) |
| `DatabaseExtensions` | ✅ Implemented (migrate + ordered, environment-gated seeding) |
| `AuthenticationExtensions` | ✅ Implemented (`AddInstitutionalAuthentication`) |
| `OpenApiExtensions` | ✅ Implemented (`AddInstitutionalOpenApi`, Bearer scheme) |
| `AddApplication` / `AddInfrastructure` DI registration | ✅ Implemented (in the Application/Infrastructure projects) |

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-10-19 | opencode | Recorded the four implemented extension files with a Contents table and corrected the implementation status (the README reported every component as not implemented and referred to a `SwaggerExtensions`/`MigrationExtensions` that do not exist). |
| 2026-10-18 | opencode | `DatabaseExtensions.InitializeDatabaseAsync` now runs ordered, environment-aware seeders instead of a fixed sequence. |
