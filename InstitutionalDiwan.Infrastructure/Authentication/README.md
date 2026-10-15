# Infrastructure / Authentication

> **Status:** Partially implemented — `PasswordHasher`, `JwtOptions`, and `JwtProvider` exist; `JwtBearerOptionsSetup` and `UserContext` are still missing.

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
Not implemented. Ambients for `UserId`, `PrimaryDepartmentId`, `ScopeType`, `Permissions`,
`CorrelationId`, and `IpAddress` extracted from the HTTP context are still missing.

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
| `JwtBearerOptionsSetup` | ❌ Not implemented |
| `UserContext` | ❌ Not implemented |

`AddInfrastructure()` registers `IPasswordHasher`, `IJwtProvider`, and `JwtOptions` from
`appsettings.json`, but `Program.cs` still does **not** add JWT bearer authentication or
authorization policies, so the API is not yet protected by tokens today.
