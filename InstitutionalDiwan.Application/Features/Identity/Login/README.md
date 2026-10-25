# Application / Features / Identity / Login — sign-in and caller profile

> **Status:** Implemented — credential sign-in, the caller's own profile, and the shared token issuer.

## Purpose

The authentication use cases behind `/api/auth`: verifying credentials and issuing an access token
(`LoginCommand`), and answering "who am I?" for an already-authenticated request
(`GetUserProfileQuery`). The profile is projected from the ambient `IUserContext` — the claims the
host extracted from the presented token — so `/api/auth/me` costs no database round-trip and keeps
the documented token-derived semantics.

## Contents

| File | Purpose |
| --- | --- |
| `LoginCommand.cs` | CQRS command carrying the submitted username and password. |
| `LoginCommandHandler.cs` | Looks the account up by normalized username, verifies the password hash (same failure for unknown user and wrong password so accounts cannot be enumerated), rejects inactive accounts, stamps `LastLoginAt` with `IUnitOfWork.UtcNow`, and issues the token. |
| `LoginCommandValidator.cs` | FluentValidation rules for the credential payload. |
| `LoginResponse.cs` | Result of a successful authentication: access token, expiry, user id, username, must-change-password flag, role codes, and permission codes. |
| `GetUserProfileQuery.cs` | Parameterless query for the caller's profile; the caller is ambient, so the query carries no state. |
| `GetUserProfileQueryHandler.cs` | Projects `IUserContext` (`UserId`, `Username`, `Roles`, `Permissions`) into the response, orders roles/permissions for a stable payload, and returns `Identity.NotAuthenticated` when no principal is attached. |
| `UserProfileResponse.cs` | Wire shape of `GET /api/auth/me`: user id, username, role codes, and permission codes. |
| `README.md` | This file. |

## Conventions / Notes

- The profile is **token-derived** on purpose: it mirrors exactly what the API enforces for this
  request, needs no repository, and cannot 404 for a token that is still valid.
- `IUserContext` is the only source; the handler never reads `HttpContext` or the claims directly.
- Responses are ordered (`Roles` ordinal, `Permissions` ordinal-ignore-case) so clients can rely on a
  deterministic payload.
- Token issuance lives in `Features/Identity/IdentityTokenIssuer.cs`, shared with `ChangePassword`.

## Implementation status

| Item | State |
| --- | --- |
| `LoginCommand` (+ handler/validator) | ✅ |
| `GetUserProfileQuery` (+ handler) | ✅ |
| Refresh tokens / token rotation | ❌ Not implemented |

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-10-22 | Buffy | Added `GetUserProfileQuery`, `GetUserProfileQueryHandler`, and `UserProfileResponse` so `GET /api/auth/me` runs through MediatR instead of assembling claims in `AuthController`; `IUserContext` gained `Username` and `Roles` to make that projection possible. `LoginCommandHandler` now stamps `LastLoginAt` from `IUnitOfWork.UtcNow`. Created this README. |
