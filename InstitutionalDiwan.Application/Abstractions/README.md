# Application / Abstractions

> **Status:** Target design — **no interfaces implemented yet**. Nothing in this folder exists as code.

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
| `IUserContext` | `Guid UserId`, `Guid PrimaryDepartmentId`, `string ScopeType`, `IReadOnlySet<string> Permissions` | Ambient current user, populated from the HTTP context by the Infrastructure implementation. |
| `IJwtProvider` | `GenerateAccessToken(User)`, `GenerateRefreshToken(User)`, validation helpers | Issues signed JWTs. |
| `IPasswordHasher` | `string Hash(string)`, `bool Verify(string, string)` | PBKDF2 / BCrypt / Argon2id — algorithm chosen in Infrastructure. |

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

## Implementation status

| Group | State |
| --- | --- |
| `Messaging/` | ❌ Not implemented |
| `Authentication/` | ❌ Not implemented |
| `Data/` | ❌ Not implemented |
| `Storage/` | ❌ Not implemented |
| `Caching/` | ❌ Not implemented |
| `Clock/` | ❌ Not implemented |

> The Infrastructure folders named after these contracts (`Authentication/`, `Storage/`, `Time/`)
> are likewise empty — see their READMEs for details.
