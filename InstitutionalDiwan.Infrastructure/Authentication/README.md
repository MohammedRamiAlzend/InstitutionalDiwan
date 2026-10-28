# Infrastructure / Authentication

> **Status:** Implemented — `PasswordHasher`, `JwtOptions`, `JwtProvider`, and `UserContext` exist;
> `JwtBearerOptionsSetup` is superseded by `Server/Extensions/AuthenticationExtensions`.

## Purpose

Concrete implementations for authenticating users (Module 02, Identity & Access). These classes satisfy
the contracts in [`../../InstitutionalDiwan.Application/Abstractions/`](../../InstitutionalDiwan.Application/Abstractions/README.md)
(`IJwtProvider`, `IPasswordHasher`, `IUserContext`).

## Contents

| File | Purpose |
| --- | --- |
| `PasswordHasher.cs` | Hashes and verifies passwords with PBKDF2 and constant-time comparison. |
| `JwtOptions.cs` | Binds the existing JWT issuer, audience, signing key, and token lifetime settings. |
| `JwtProvider.cs` | Issues signed access tokens with identity, roles, permissions, and a keyed session stamp. |
| `JwtSessionStamp.cs` | Canonically HMACs current identity/profile/password state and effective active role/permission sets; validates stamps in constant time without exposing credentials. |
| `UserContext.cs` | Projects the authenticated request principal into the application caller context. |

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
`must_change_password`, plus role and permission claims and the mandatory `session_stamp`.
Only the HMAC is emitted: password hashes and signing secrets never become claims or logs.
Refresh-token issuance/rotation is **not** implemented yet.

### `JwtSessionStamp.cs`
Uses the existing configured signing key with HMAC-SHA256 over a versioned, length-prefixed payload:
user id/name, password hash, employee/primary-department/scope, active/forced-change flags,
creation/update ticks (rounded to SQL Server `datetime2(3)` storage precision), and ordinal-sorted
distinct active role and permission sets. `LastLoginAt` is excluded so another login does not revoke
existing sessions. Storage rounding keeps fresh password-change tokens valid after database reload. The host re-reads current user and
active roles in `OnTokenValidated`, rejecting missing/inactive users and absent, malformed, duplicate,
or mismatched stamps; there is no legacy fallback. Password reset/change, profile/scope edits, and
role/permission revocations therefore invalidate old JWTs without a migration or key change.

JWT bearer setup lives in `Server/Extensions/AuthenticationExtensions.cs`; there is no separate
`JwtBearerOptionsSetup.cs`. Existing sessions must sign in again after deployment.

### `UserContext.cs` (`: IUserContext`)
Implemented. Reads the ambient principal through `IHttpContextAccessor` and projects the claims
`JwtProvider` issues: `UserId` (`sub` / `NameIdentifier`), `Username` (`unique_name` → `Name`),
`Roles` (the `role` claims), `PrimaryDepartmentId` (`dept_id`), `ScopeTypeId` (`scope_type`), and
the `Permission` set (one `permission` claim per granted code). An unauthenticated request yields
`IsAuthenticated = false`, `UserId = Guid.Empty`, an empty username, and empty role/permission
sets — it fails safe instead of throwing.

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
| `JwtSessionStamp` + live bearer validation | Implemented (shared keyed stamp; validation in `Server/Extensions/AuthenticationExtensions.cs`) |
| `UserContext` | ✅ Implemented |

`AddInfrastructure()` registers `IPasswordHasher`, `IJwtProvider`, `IUserContext` (+
`AddHttpContextAccessor`) and binds `JwtOptions` from `appsettings.json`; JWT bearer authentication
and the one-policy-per-permission authorization setup live in `Server/Extensions/AuthenticationExtensions.cs`.

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-10-28 | Qoder | Add shared keyed session stamps over account/profile/password and active authorization state; exclude LastLoginAt, emit no credentials, and reject unstamped legacy JWTs without migrations or signing-key changes. |
| 2026-10-22 | Buffy | `UserContext` now also exposes `Username` and `Roles` (from `unique_name`/`Name` and the `role` claims) for `GetUserProfileQuery`. |
| 2026-10-19 | Buffy | Added `UserContext` (`: IUserContext`) reading `sub` / `dept_id` / `scope_type` / `permission` claims through `IHttpContextAccessor`; registered together with `AddHttpContextAccessor` in `Infrastructure/DependencyInjection.cs`. |
| 2026-10-18 | opencode | Implemented `PasswordHasher` (PBKDF2), `JwtOptions`, and `JwtProvider` (access-token claims). |
