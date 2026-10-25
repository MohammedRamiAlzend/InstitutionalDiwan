# InstitutionalDiwan.Application — Application Layer

> **Status:** Implemented — `DependencyInjection.cs`, `Abstractions/` (messaging, authentication,
> authorization, storage, time, request context, transactions), `Behaviors/` (validation, logging,
> transactions), and the live feature slices under `Features/` (Lookups, Organization, Identity,
> Correspondence, Numbering, Movements routing workflow + inbox/outbox, Attachments, and Audit) are in
> place.

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
| [`Abstractions/`](Abstractions/README.md) | Interfaces the infrastructure must implement (messaging, auth, authorization, storage, time, request context, transactions) + the planned data/caching contracts. |
| [`Behaviors/`](Behaviors/README.md) | MediatR `IPipelineBehavior` classes — validation, logging, and transaction behaviors are live; caching is planned. |
| [`Common/`](Common/README.md) | Shared response shapes — `PagedResult<T>` is the wire contract for every paged list endpoint. |
| [`Exceptions/`](Exceptions/README.md) | Technical/pipeline exceptions (validation fan-out, optimistic-concurrency conflicts). |
| [`Features/`](Features/README.md) | Vertical feature slices per module (Commands, Queries, Validators, DTOs). |

## Composition root

`DependencyInjection.AddApplication()` registers MediatR handlers from the assembly and applies the
`ValidationPipelineBehavior<,>`, `LoggingPipelineBehavior<,>`, and `TransactionPipelineBehavior<,>`
open behaviors plus every `FluentValidation` validator found by assembly scan; `Program.cs` calls it
alongside `AddInfrastructure()`.

## Implementation status

| Item | State |
| --- | --- |
| Commands / Queries / Validators / DTOs | ✅ Implemented (Lookups, Organization/Departments, Identity/Users·Roles·Permissions·Employees·AccessGrants·Login·Register·ChangePassword, Correspondence/Documents, Movements, Attachments, Audit) |
| Pipeline behaviors | ✅ Validation, logging, and transaction implemented; caching planned |
| Abstractions | ✅ `Messaging/`, `Authentication/`, `Authorization/`, `Storage/`, `Time/`, `Context/`, and `Persistence/` implemented; data/caching planned |
| `DependencyInjection.cs` | ✅ Implemented (`AddApplication`) |
| `Common/PagedResult<T>` | ✅ Implemented (paged list wire shape) |
| `Class1.cs` placeholder | 🗑️ Removed |
| Registered in the web host | ✅ `Program.cs` calls `AddApplication()` |

See each subfolder's README for the full design and per-folder status tables.

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-10-25 | opencode | Added the Module 09 Audit slice (`GetAuditLogsQuery`, checkpoint/integrity commands and reports, `AuditLogAppender`), plus logging/transaction pipeline behaviors and the time, request-context, and transaction abstractions. |
| 2026-10-25 | opencode | Added the Module 06 Attachments slice and the `Abstractions/Storage` contracts; every operation checks document access (VIEW/DOWNLOAD/MANAGE) and uploads carry a streamed SHA-256 checksum. |
| 2026-10-22 | Buffy | Added `Identity/Login/GetUserProfileQuery` (the `/api/auth/me` use case) and `Correspondence/Documents/ListDepartmentDocumentsQuery`; every mutating handler now passes `IUnitOfWork.UtcNow` to the aggregate it touches. |
| 2026-10-22 | opencode | Added `Common/PagedResult<T>` and made the four unbounded list queries return it; filtering, sorting, and paging now run in the repositories. |
| 2026-10-21 | opencode | Completed `Features/Movements/`: the routing workflow commands (open/send/complete/reject/return/redirect/close/cancel), the movement queries, and the department inbox/outbox. |
| 2026-10-20 | Buffy | Added `Features/Movements/` (receipt gated on a recorded department register number) and department register entries under `Features/Numbering/Numbers/` + `Registries/`. |
| 2026-10-19 | opencode | Added the `Features/Lookups/` slice (generic list/get/create/relabel/deactivate/activate over all seeded reference catalogs) and refreshed this README, which still described the layer as "scaffolding only — no code yet". |
