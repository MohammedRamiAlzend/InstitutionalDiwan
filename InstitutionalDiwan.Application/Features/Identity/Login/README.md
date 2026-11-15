# Application / Features / Identity / Login — sign-in and caller profile

> **Status:** Implemented — credential sign-in, the caller's own profile, and the shared token issuer.

## Purpose

The authentication use cases behind `/api/auth`: verifying credentials and issuing an access token
(`LoginCommand`), and answering "who am I?" for an already-authenticated request
(`GetUserProfileQuery`). The identity is projected from the ambient `IUserContext` — the claims the
host extracted from the presented token — and the reachable departments come from
`IDepartmentScopeEvaluator`, the same live scope the API enforces.

## Contents

| File | Purpose |
| --- | --- |
| `LoginCommand.cs` | CQRS command carrying the submitted username and password. |
| `LoginCommandHandler.cs` | Looks the account up by normalized username, verifies the password hash (same failure for unknown user and wrong password so accounts cannot be enumerated, and an unknown username still burns one dummy PBKDF2 verification so timing cannot tell them apart), rejects inactive accounts, upgrades legacy hashes silently on success (`NeedsRehash` → `User.UpgradePasswordHash`, skipped for forced-change accounts), stamps `LastLoginAt` with `IUnitOfWork.UtcNow`, and issues the token. Failed attempts run through `ILoginAttemptTracker`; after five failures in a window the account is locked out (`Identity.LoginThrottled`, 429) and a successful login clears the counter. |
| `LoginCommandValidator.cs` | FluentValidation rules for the credential payload. |
| `LoginResponse.cs` | Result of a successful authentication: access token, expiry, user id, username, must-change-password flag, role codes, and permission codes. |
| `GetUserProfileQuery.cs` | Parameterless query for the caller's profile; the caller is ambient, so the query carries no state. |
| `GetUserProfileQueryHandler.cs` | Projects `IUserContext` (`UserId`, `Username`, `Roles`, `Permissions`) and the evaluator's reachable departments into the response, orders the collections for a stable payload, and returns `Identity.NotAuthenticated` when no principal is attached. |
| `UserProfileResponse.cs` | Wire shape of `GET /api/auth/me`: user id, username, role codes, permission codes, and `AccessibleDepartmentIds` (null for an institution-wide scope). |
| `README.md` | This file. |

## Conventions / Notes

- Roles and permissions are **token-derived** on purpose: they mirror exactly what the API enforces
  for this request and cannot 404 for a token that is still valid. Department assignments and
  subtrees are not in the token, so `AccessibleDepartmentIds` is read live through the evaluator.
- The handler never reads `HttpContext` or the claims directly.
- Responses are ordered (`Roles` ordinal, `Permissions` ordinal-ignore-case, departments by id) so
  clients can rely on a deterministic payload.
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
| 2026-11-15 | Qoder | Security: hashes stored under an older PBKDF2 work factor are silently upgraded on a successful login (`NeedsRehash` → `User.UpgradePasswordHash`, without touching `UpdatedAt`/`MustChangePassword`), and unknown usernames burn one dummy verification so login timing cannot enumerate accounts. |
| 2026-11-15 | Qoder | Security: `LoginCommandHandler` now throttles failed attempts through `ILoginAttemptTracker` — five failures within the window lock the username out and return 429 `Identity.LoginThrottled`; failed attempts count for unknown usernames too so the lockout cannot serve as an account-enumeration oracle. |
| 2026-11-11 | Claude |  |
| 2026-11-04 | Claude | `UserProfileResponse` (`GET /api/auth/me`) gained `primaryDepartmentId` so the SPA can fix the department of a register-number entry to the caller's own. |
| 2026-11-03 | Claude | Added `AccessibleDepartmentIds` to the profile, read through `IDepartmentScopeEvaluator`, so the SPA can offer only the movement actions on the caller's side of the current step. `/api/auth/me` now makes a database read. |
| 2026-10-22 | Buffy | Added `GetUserProfileQuery`, `GetUserProfileQueryHandler`, and `UserProfileResponse` so `GET /api/auth/me` runs through MediatR instead of assembling claims in `AuthController`; `IUserContext` gained `Username` and `Roles` to make that projection possible. `LoginCommandHandler` now stamps `LastLoginAt` from `IUnitOfWork.UtcNow`. Created this README. |
