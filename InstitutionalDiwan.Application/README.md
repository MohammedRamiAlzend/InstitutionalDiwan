# InstitutionalDiwan.Application — Application Layer

> **Status:** Implemented — `DependencyInjection.cs`, `Abstractions/` (messaging + authentication),
> `Behaviors/ValidationPipelineBehavior.cs`, and the live feature slices under `Features/`
> (Lookups, Organization, Identity, Correspondence, Numbering, Movements routing workflow + inbox/outbox) are in place.

## Purpose

Orchestrates the use cases of the 10 InstitutionalDiwan modules using CQRS (Command Query
Responsibility Segregation) and the Result pattern. Handlers load aggregates through
`InstitutionalDiwan.Domain.Repositories`, call intention-revealing domain methods, and persist via
`IUnitOfWork`. The layer is deliberately infrastructure-agnostic.

## Allowed / forbidden dependencies

| | |
| --- | --- |
| **Allowed** | `InstitutionalDiwan.Domain`, `MediatR`, `FluentValidation`, `Microsoft.Extensions.DependencyInjection.Abstractions` |
| **Forbidden** | `Microsoft.EntityFrameworkCore`, `Microsoft.AspNetCore.*`, SQL Server drivers, file-system APIs |

Rationale: keeping EF Core and ASP.NET out of this layer means every use case can be executed in a
plain unit test with in-memory/fake repositories.

## Subfolders

| Folder | Responsibility |
| --- | --- |
| [`Abstractions/`](Abstractions/README.md) | Interfaces the infrastructure must implement (messaging, auth) + the planned data/storage/caching/clock contracts. |
| [`Behaviors/`](Behaviors/README.md) | MediatR `IPipelineBehavior` classes — `ValidationPipelineBehavior` is live; logging/transaction/caching are planned. |
| [`Common/`](Common/README.md) | Shared response shapes — `PagedResult<T>` is the wire contract for every paged list endpoint. |
| [`Exceptions/`](Exceptions/README.md) | Technical/pipeline exceptions (validation fan-out, optimistic-concurrency conflicts). |
| [`Features/`](Features/README.md) | Vertical feature slices per module (Commands, Queries, Validators, DTOs). |

## Composition root

`DependencyInjection.AddApplication()` registers MediatR handlers from the assembly and applies the
`ValidationPipelineBehavior<,>` open behavior plus every `FluentValidation` validator found by
assembly scan; `Program.cs` calls it alongside `AddInfrastructure()`.

## Implementation status

| Item | State |
| --- | --- |
| Commands / Queries / Validators / DTOs | ✅ Implemented (Lookups, Organization/Departments, Identity/Users·Roles·Permissions·Employees·AccessGrants·Login·Register·ChangePassword, Correspondence/Documents) |
| Pipeline behaviors | 🟡 `ValidationPipelineBehavior` implemented; logging/transaction/caching planned |
| Abstractions | 🟡 `Messaging/` + `Authentication/` implemented; data/storage/caching/clock planned |
| `DependencyInjection.cs` | ✅ Implemented (`AddApplication`) |
| `Common/PagedResult<T>` | ✅ Implemented (paged list wire shape) |
| `Class1.cs` placeholder | 🗑️ Removed |
| Registered in the web host | ✅ `Program.cs` calls `AddApplication()` |

See each subfolder's README for the full design and per-folder status tables.

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-10-22 | Buffy | Added `Identity/Login/GetUserProfileQuery` (the `/api/auth/me` use case) and `Correspondence/Documents/ListDepartmentDocumentsQuery`; every mutating handler now passes `IUnitOfWork.UtcNow` to the aggregate it touches. |
| 2026-10-22 | opencode | Added `Common/PagedResult<T>` and made the four unbounded list queries return it; filtering, sorting, and paging now run in the repositories. |
| 2026-10-21 | opencode | Completed `Features/Movements/`: the routing workflow commands (open/send/complete/reject/return/redirect/close/cancel), the movement queries, and the department inbox/outbox. |
| 2026-10-20 | Buffy | Added `Features/Movements/` (receipt gated on a recorded department register number) and department register entries under `Features/Numbering/Numbers/` + `Registries/`. |
| 2026-10-19 | opencode | Added the `Features/Lookups/` slice (generic list/get/create/relabel/deactivate/activate over all seeded reference catalogs) and refreshed this README, which still described the layer as "scaffolding only — no code yet". |
