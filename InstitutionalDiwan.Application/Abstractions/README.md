# Application / Abstractions

> **Status:** Partially implemented — `Messaging/` (ICommand/IQuery/ICommandHandler/IQueryHandler),
> `Authentication/` (`IJwtProvider`, `IPasswordHasher`, `IUserContext`), `Authorization/`
> (`IDepartmentScopeEvaluator`, `IDocumentAccessEvaluator`), `Storage/` (`IFileStorageService`,
> `IFileSignatureValidator`, `StoredFile`), `Time/` (`IDateTimeProvider`), `Context/`
> (`IRequestContext`), `Persistence/` (`ITransactionManager`), and `Audit/`
> (`IAuditActionRecorder`, `AuditActionRecord`) are live; the data/caching contracts are still
> target design.

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
| `IFileStorageService` | `Task<StoredFile> SaveAsync(Stream, …, int providerId, string basePath)`, `Task<Stream?> OpenReadAsync(string basePath, string relativePath)`, `Task<string> ComputeSha256Async(Stream)` | Abstracts document scans/attachments (Module 06) and returns the SHA-256 checksum recorded by `DocumentFile.AddVersion`. ✅ Implemented by `Infrastructure/Storage/LocalFileStorageService`. |
| `IFileSignatureValidator` | `Result Validate(ReadOnlySpan<byte> header, string fileName, long size, DocumentFileType type)` | Enforces the file type's extension allow-list and size limit, then checks magic bytes so a renamed executable cannot masquerade as a PDF/image. ✅ Implemented by `Infrastructure/Storage/FileSignatureValidator`. |

### `Caching/`

| Interface | Shape | Notes |
| --- | --- | --- |
| `ICacheService` | `Get/Set/Remove` with TTL | Distributed or in-memory. |
| `ICacheableQuery` | `string CacheKey { get; }`, optional TTL | Marks a query for `QueryCachingPipelineBehavior`. |

### `Clock/`

| Interface | Shape | Notes |
| --- | --- | --- |
| `IDateTimeProvider` | `DateTime UtcNow`, `DateTime ToInstitutionLocal(DateTime utc)`, `int NumberingYear(DateTime utc)` | Lets domain logic and numbering years be tested deterministically. ✅ Implemented by `Infrastructure/Time/DateTimeProvider`. |

### `Context/`

| Interface | Shape | Notes |
| --- | --- | --- |
| `IRequestContext` | `string? CorrelationId { get; }`, `string? IpAddress { get; }` | Ambient request metadata for logging and explicit audit rows; null outside a request. ✅ Implemented by `Infrastructure/Context/HttpRequestContext`. |

### `Persistence/`

| Interface | Shape | Notes |
| --- | --- | --- |
| `ITransactionManager` | `Task<ITransactionScope> BeginTransactionAsync(CancellationToken)` | Opens an explicit database transaction so one command's writes, outbox work, and audit rows commit together. ✅ Implemented by `Infrastructure/Persistence/Transactions/EfTransactionManager`. |
| `ITransactionScope` | `CommitAsync`, `DisposeAsync` | Disposing without commit rolls back. |

### `Audit/`

| Type | Shape | Notes |
| --- | --- | --- |
| `IAuditActionRecorder` | `Task RecordAsync(AuditActionRecord record, CancellationToken)` | Appends one action-level audit row (module, action/entity type, entity id, actor, department, new-values JSON). ✅ Implemented by `Application/Features/Audit/AuditActionRecorder` with an isolated service scope, so it commits independently of the caller's tracked changes and never fails the action it describes. |
| `AuditActionRecord` | `record(ModuleName, ActionTypeId, EntityTypeId, EntityId, ActorUserId, ActorDepartmentId, NewValuesJson)` | Transport shape for the recorder; the entity id is a free-form correlation label such as `Controller.Action`. |

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
| `Storage/IFileStorageService.cs` | Persists and reads document bytes under a provider root, returning the streamed SHA-256 checksum of an upload. |
| `Storage/IFileSignatureValidator.cs` | Validates an upload's extension, size, and magic bytes before any bytes are stored. |
| `Storage/StoredFile.cs` | Value returned by storage after a save: relative path, original name, MIME type, size, checksum, and provider id. |
| `Time/IDateTimeProvider.cs` | UTC now plus institution-local conversion and numbering-year helpers; implemented by Infrastructure `DateTimeProvider` over `IClock`. |
| `Context/IRequestContext.cs` | Correlation id and client IP for the current request, kept host-agnostic; implemented by Infrastructure `HttpRequestContext`. |
| `Persistence/ITransactionManager.cs` | `BeginTransactionAsync` returns an `ITransactionScope`; commit on success and roll back on a failed `Result` or exception. |
| `Audit/IAuditActionRecorder.cs` | `IAuditActionRecorder` + `AuditActionRecord`: the write contract behind action-level audit rows, implemented with an isolated scope so request state cannot leak into the audit commit. |

## Implementation status

| Group | State |
| --- | --- |
| `Messaging/` | ✅ Implemented (ICommand, ICommandHandler, IQuery, IQueryHandler) |
| `Authentication/` | ✅ Implemented (IJwtProvider, IPasswordHasher, IUserContext) |
| `Authorization/` | ✅ Implemented (IDepartmentScopeEvaluator, IDocumentAccessEvaluator) |
| `Data/` | ❌ Not implemented |
| `Storage/` | ✅ Implemented (`IFileStorageService`, `IFileSignatureValidator`, `StoredFile`) |
| `Caching/` | ❌ Not implemented |
| `Clock/` | ✅ Implemented (`IDateTimeProvider` + Infrastructure `DateTimeProvider`) |
| `Context/` | ✅ Implemented (`IRequestContext` + Infrastructure `HttpRequestContext`) |
| `Persistence/` | ✅ Implemented (`ITransactionManager` + Infrastructure `EfTransactionManager`) |
| `Audit/` | ✅ Implemented (`IAuditActionRecorder` + Application `AuditActionRecorder`) |

> The Infrastructure `Authentication/`, `Storage/`, `Time/`, `Context/`, and `Persistence/` folders
> implement these contracts; `Data/` and `Caching/` remain target design. See their READMEs for details.

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-10-26 | Qoder | Added the `Audit/` group (`IAuditActionRecorder`, `AuditActionRecord`) so the web host can append action-level audit rows without depending on persistence, and marked it implemented by `AuditActionRecorder`. |
| 2026-10-25 | opencode | Added `Context/IRequestContext` and `Persistence/ITransactionManager`, and marked `IDateTimeProvider` implemented by Infrastructure `DateTimeProvider` for Module 09. |
| 2026-10-25 | opencode | Added the `Storage/` contracts (`IFileStorageService`, `IFileSignatureValidator`, `StoredFile`) so the Module 06 Attachments handlers depend on abstractions instead of the file system or ASP.NET upload types. |
| 2026-10-22 | Buffy | Extended `IUserContext` with `Username` and `Roles` so `GetUserProfileQuery` can project the caller's profile (`GET /api/auth/me`) without reading claims in the controller. |
| 2026-10-19 | Buffy | Added `Authentication/IUserContext` and the `Authorization/` group (`IDepartmentScopeEvaluator`, `IDocumentAccessEvaluator`) — the contracts behind the three-tier access model; implemented in `Infrastructure/Authentication/UserContext.cs` and `Infrastructure/Authorization/`. |
| 2026-10-19 | opencode | Recorded `Messaging/` (`ICommand`, `ICommandHandler`, `IQuery`, `IQueryHandler`) and `Authentication/` (`IJwtProvider`, `IPasswordHasher`) as ✅ implemented with a per-file Contents table; the README previously reported both groups as not implemented. |
