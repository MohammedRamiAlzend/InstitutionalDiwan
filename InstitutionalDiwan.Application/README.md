# InstitutionalDiwan.Application — Application Layer

> **Status:** Scaffolding only — **no code yet**. All subfolders currently contain documentation, not `.cs` files.

## Purpose

Orchestrates the use cases of the 10 InstitutionalDiwan modules using CQRS (Command Query
Responsibility Segregation) and the Result pattern. Handlers load aggregates through
`InstitutionalDiwan.Domain.Repositories`, call intention-revealing domain methods, and persist via
`IUnitOfWork`. The layer is deliberately infrastructure-agnostic.

## Allowed / forbidden dependencies

| | |
| --- | --- |
| **Allowed** | `InstitutionalDiwan.Domain`, `MediatR.Contracts`, `FluentValidation` |
| **Forbidden** | `Microsoft.EntityFrameworkCore`, `Microsoft.AspNetCore.*`, SQL Server drivers, file-system APIs |

Rationale: keeping EF Core and ASP.NET out of this layer means every use case can be executed in a
plain unit test with in-memory/fake repositories.

## Subfolders

| Folder | Responsibility |
| --- | --- |
| [`Abstractions/`](Abstractions/README.md) | Interfaces the infrastructure must implement (messaging, auth, data, storage, caching, clock). |
| [`Behaviors/`](Behaviors/README.md) | MediatR `IPipelineBehavior` classes for validation, logging, transactions, and query caching. |
| [`Exceptions/`](Exceptions/README.md) | Technical/pipeline exceptions (validation fan-out, optimistic-concurrency conflicts). |
| [`Features/`](Features/README.md) | Vertical feature slices per module (Commands, Queries, Validators, DTOs, EventHandlers). |

## Package footprint

`InstitutionalDiwan.Application.csproj` currently references **only** `InstitutionalDiwan.Domain`.
To realize the design it still needs `MediatR` (or `MediatR.Contracts` + a dispatcher),
`FluentValidation`, and a `Microsoft.Extensions.DependencyInjection.Abstractions` reference for a
`DependencyInjection.cs` registration entry point.

## Implementation status

| Item | State |
| --- | --- |
| Commands / Queries / Validators / DTOs / EventHandlers | ❌ None implemented |
| Pipeline behaviors | ❌ None implemented |
| Abstractions | ❌ None implemented |
| `Class1.cs` placeholder | 🗑️ Removed |
| Registered in the web host | ❌ Not wired into `Program.cs` |

See each subfolder's README for the full target design and a per-folder status table.
