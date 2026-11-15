# Application / Features / Identity / Register — self-service sign-up

> **Status:** Implemented — self-registration creates an **inactive** account that an administrator
> must activate before it can sign in.

## Purpose

The use case behind `POST /api/auth/register`: a visitor picks a username and password and gets an
account with the standard `USER` role. The account is created **inactive** (`User.IsActive = false`)
as a security measure — nobody can sign in with a self-registered account until an administrator
activates it, so an open registration endpoint cannot be used to gain access without review.

## Contents

| File | Purpose |
| --- | --- |
| `RegisterCommand.cs` | CQRS command carrying the requested username and password. |
| `RegisterCommandValidator.cs` | FluentValidation rules for the sign-up payload. |
| `RegisterCommandHandler.cs` | Rejects a taken username (case-insensitive, `Identity.UsernameTaken`), requires the seeded `USER` role (`Identity.RoleNotFound`), hashes the password, and creates the account with `IsActive = false` (pending administrator activation) and no department yet (`primaryDepartmentId: Guid.Empty`). |
| `RegisterResponse.cs` | Result of a successful sign-up: user id, normalized username, must-change-password flag, and role codes. |
| `README.md` | This file. |

## Conventions / Notes

- The username is trimmed and lowercased, and uniqueness is compared case-insensitively, matching
  the login normalization.
- Self-registered accounts always get the `USER` role only — never a privileged role.
- Rate limiting applies at the endpoint (`RateLimitPolicies.Register`, 5/hour per IP); see the
  `Server` layer.

## Implementation status

| Item | State |
| --- | --- |
| `RegisterCommand` (+ handler/validator/response) | ✅ |
| Inactive-until-activated accounts | ✅ |
| Administrator activation flow | ❌ Not implemented (activation is done directly on the user record) |

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-11-15 | Qoder | Security (M1): self-registration now creates the account with `IsActive = false`, so a self-registered user cannot sign in until an administrator activates the account. |
