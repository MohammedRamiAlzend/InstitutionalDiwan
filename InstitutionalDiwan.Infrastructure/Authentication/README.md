# Infrastructure / Authentication

> **Status:** Implemented — `PasswordHasher`, `JwtOptions`, `JwtProvider`, and `UserContext` exist;
> `JwtBearerOptionsSetup` is superseded by `Server/Extensions/AuthenticationExtensions`.

## Purpose

Concrete implementations for authenticating users (Module 02, Identity & Access). These classes satisfy
the contracts in [`../../InstitutionalDiwan.Application/Abstractions/`](../../InstitutionalDiwan.Application/Abstractions/README.md)
(`IJwtProvider`, `IPasswordHasher`, `IUserContext`).

## Components

### `PasswordHasher.cs` (`: IPasswordHasher`)
Implemented. PBKDF2 (HMAC-SHA256) with a self-describing stored value of the form
`PBKDF2&<iterations>$&<base64-salt>$&<base64-key>`. Verification uses constant-time comparison.

### `JwtOptions.cs` (`JwtOptions.SectionName = "Jwt"`)
Implemented. Strongly-typed binding for `Issuer`, `Audience`, `SecretKey`, and
`AccessTokenExpirationMinutes`. The signing key must be at least 32 bytes; supply it from
user-secrets / environment / a secret store, not from the repo.

### `JwtProvider.cs` (`: IJwtProvider`)
Implemented. Generates signed JWT **access tokens** carrying:
`sub` (UserId), `dept_id` (PrimaryDepartmentId), `scope_type` (ScopeType),
`must_change_password`, plus role and permission claims.
Refresh-token issuance/rotation is **not** implemented yet.

### `JwtBearerOptionsSetup.cs`
Not implemented. ASP.NET Core JWT bearer validation setup (validation parameters, clock skew,
challenge behavior) is still missing.

### `UserContext.cs` (`: IUserContext`)
Implemented. Reads the ambient principal through `IHttpContextAccessor` and projects the claims
`JwtProvider` issues: `UserId` (`sub` / `NameIdentifier`), `PrimaryDepartmentId` (`dept_id`),
`ScopeTypeId` (`scope_type`), and the `Permission` set (one `permission` claim per granted code).
An unauthenticated request yields `IsAuthenticated = false`, `UserId = Guid.Empty`, and an empty
permission set — it fails safe instead of throwing.

## Design notes

- Never log or persist raw tokens, secrets, or password hashes.
- `UserContext` must fail safe: an unauthenticated request yields an empty/denied context rather than
  throwing into business logic.
- The permission set it exposes is the flattened union of the user's active roles' permissions; the
  authoritative evaluation lives in [`../Authorization/`](../Authorization/README.md).
- The DI entry point that registers these services lives in
  [`../DependencyInjection.cs`](../DependencyInjection.cs), not in this folder.

## Implementation status

| Component | State |
| --- | --- |
| `PasswordHasher` | ✅ Implemented |
| `JwtOptions` | ✅ Implemented |
| `JwtProvider` | ✅ Implemented |
| `JwtBearerOptionsSetup` | ❌ Not implemented (equivalent setup lives in `Server/Extensions/AuthenticationExtensions.cs`) |
| `UserContext` | ✅ Implemented |

`AddInfrastructure()` registers `IPasswordHasher`, `IJwtProvider`, `IUserContext` (+
`AddHttpContextAccessor`) and binds `JwtOptions` from `appsettings.json`; JWT bearer authentication
and the one-policy-per-permission authorization setup live in `Server/Extensions/AuthenticationExtensions.cs`.

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-10-19 | Buffy | Added `UserContext` (`: IUserContext`) reading `sub` / `dept_id` / `scope_type` / `permission` claims through `IHttpContextAccessor`; registered together with `AddHttpContextAccessor` in `Infrastructure/DependencyInjection.cs`. |
| 2026-10-18 | opencode | Implemented `PasswordHasher` (PBKDF2), `JwtOptions`, and `JwtProvider` (access-token claims). |
