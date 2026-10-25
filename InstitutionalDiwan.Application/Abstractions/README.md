# Application / Abstractions

> **Status:** Partially implemented — `Messaging/` (ICommand/IQuery/ICommandHandler/IQueryHandler),
> `Authentication/` (`IJwtProvider`, `IPasswordHasher`, `IUserContext`), and `Authorization/`
> (`IDepartmentScopeEvaluator`, `IDocumentAccessEvaluator`) are live; the data/storage/caching/clock
> contracts are still target design.

## Purpose

This folder defines the interfaces (contracts) the Application layer needs and the Infrastructure
layer implements — an application of the Dependency Inversion Principle. Handlers depend on these
abstractions rather than on concrete infrastructure, which keeps use cases testable and the
technology choices swappable.

## Planned contracts

### `Messaging/`

| Interface | Shape | Notes |
| --- | --- | --- |
| `ICommand` / `ICommand<TResponse>` | `IRequest<Result>` / `IRequest<Result<TResponse>>` | Marker interfaces for write operations. |
| `ICommandHandler<TCommand>` / `ICommandHandler<TCommand, TResponse>` | `IRequestHandler<…, Result>` / `Result<TResponse>` | Handlers always return a `Result`. |
| `IQuery<TResponse>` / `IQueryHandler<TQuery, TResponse>` | `IRequest<Result<TResponse>>` | Read-only; safe to cache. |
| `IDomainEventHandler<TDomainEvent>` | `INotificationHandler<TDomainEvent>` | Reacts to events raised by `AggregateRoot`. |

### `Authentication/`

| Interface | Shape | Notes |
| --- | --- | --- |
| `IUserContext` | `bool IsAuthenticated`, `Guid UserId`, `string Username`, `IReadOnlySet<string> Roles`, `Guid PrimaryDepartmentId`, `int ScopeTypeId`, `IReadOnlySet<string> Permissions` | Ambient current user projected from the JWT claims (`sub`, `unique_name`, `role`, `dept_id`, `scope_type`, one `permission` claim per code). ✅ Implemented (`Infrastructure/Authentication/UserContext.cs`). |
| `IJwtProvider` | `GenerateAccessToken(...)` + permission/role claims | ✅ Implemented; consumed by `LoginCommand` and the auth controller. |
| `IPasswordHasher` | `string Hash(string)`, `bool Verify(string, string)` | ✅ Implemented (PBKDF2 in `Infrastructure/Authentication/PasswordHasher.cs`). |

### `Authorization/`

| Interface | Shape | Notes |
| --- | --- | --- |
| `IDepartmentScopeEvaluator` | `Task<bool> CanAccessDepartmentAsync(Guid departmentId, CancellationToken)` | Tier 2 of the access model: is the target department inside the caller's scope (primary department, active assignments incl. subtree, institution-wide)? ✅ Implemented. |
| `IDocumentAccessEvaluator` | `Task<bool> CanAccessDocumentAsync(Guid documentId, int requiredAccessLevelId, CancellationToken)` | Tier 3: combines explicit grants, authorship, department scope, and confidentiality for VIEW/DOWNLOAD/FORWARD/MANAGE. ✅ Implemented. |

### `Data/`

| Interface | Shape | Notes |
| --- | --- | --- |
| `ISqlConnectionFactory` | `IDbConnection CreateConnection()` | Backs fast, hand-written read queries (Dapper) where EF Core would be heavy: complex correspondence search, hierarchy trees, inbox counters. |

### `Storage/`

| Interface | Shape | Notes |
| --- | --- | --- |
| `IFileStorageService` | `Task<StoredFile> SaveAsync(Stream, …)`, `Task<Stream> OpenReadAsync(string path)`, `Task<string> ComputeSha256Async(Stream)` | Abstracts document scans/attachments (Module 06) and returns the SHA-256 checksum recorded by `DocumentFile.AddVersion`. |

### `Caching/`

| Interface | Shape | Notes |
| --- | --- | --- |
| `ICacheService` | `Get/Set/Remove` with TTL | Distributed or in-memory. |
| `ICacheableQuery` | `string CacheKey { get; }`, optional TTL | Marks a query for `QueryCachingPipelineBehavior`. |

### `Clock/`

| Interface | Shape | Notes |
| --- | --- | --- |
| `IDateTimeProvider` | `DateTime UtcNow`, `DateTime ToInstitutionLocal(DateTime utc)`, `int NumberingYear(DateTime utc)` | Lets domain logic and numbering years be tested deterministically. |

## Contents

| File | Purpose |
| --- | --- |
| `Messaging/ICommand.cs` | `ICommand` / `ICommand<TResponse>` markers over `IRequest<Result>` / `IRequest<Result<TResponse>>`. |
| `Messaging/ICommandHandler.cs` | `ICommandHandler<TCommand>` / `ICommandHandler<TCommand, TResponse>` — handlers always return a `Result`. |
| `Messaging/IQuery.cs` | `IQuery<TResponse>` marker over `IRequest<Result<TResponse>>`. |
| `Messaging/IQueryHandler.cs` | `IQueryHandler<TQuery, TResponse>` for read-only use cases. |
| `Authentication/IJwtProvider.cs` | Issues signed access tokens carrying user id, roles, and permission claims. |
| `Authentication/IPasswordHasher.cs` | `Hash` / `Verify` for user credentials (PBKDF2 implementation in Infrastructure). |
| `Authentication/IUserContext.cs` | Ambient caller identity (authenticated flag, user id, username, role codes, primary department, scope type, permission set) so handlers never touch `HttpContext`; backs `GetUserProfileQuery`. |
| `Authorization/IDepartmentScopeEvaluator.cs` | Answers “may the caller act on this department?”, implemented from the user's scope type, primary department, and active assignments. |
| `Authorization/IDocumentAccessEvaluator.cs` | Answers “may the caller reach this document at this access level?”, implemented from grants, authorship, scope, and confidentiality. |

## Implementation status

| Group | State |
| --- | --- |
| `Messaging/` | ✅ Implemented (ICommand, ICommandHandler, IQuery, IQueryHandler) |
| `Authentication/` | ✅ Implemented (IJwtProvider, IPasswordHasher, IUserContext) |
| `Authorization/` | ✅ Implemented (IDepartmentScopeEvaluator, IDocumentAccessEvaluator) |
| `Data/` | ❌ Not implemented |
| `Storage/` | ❌ Not implemented |
| `Caching/` | ❌ Not implemented |
| `Clock/` | ❌ Not implemented |

> The Infrastructure `Authentication/` folder implements these contracts (`JwtProvider`,
> `PasswordHasher`); `Storage/`, `Outbox/`, `BackgroundJobs/` and `Time/` are still empty — see their
> READMEs for details.

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-10-22 | Buffy | Extended `IUserContext` with `Username` and `Roles` so `GetUserProfileQuery` can project the caller's profile (`GET /api/auth/me`) without reading claims in the controller. |
| 2026-10-19 | Buffy | Added `Authentication/IUserContext` and the `Authorization/` group (`IDepartmentScopeEvaluator`, `IDocumentAccessEvaluator`) — the contracts behind the three-tier access model; implemented in `Infrastructure/Authentication/UserContext.cs` and `Infrastructure/Authorization/`. |
| 2026-10-19 | opencode | Recorded `Messaging/` (`ICommand`, `ICommandHandler`, `IQuery`, `IQueryHandler`) and `Authentication/` (`IJwtProvider`, `IPasswordHasher`) as ✅ implemented with a per-file Contents table; the README previously reported both groups as not implemented. |
