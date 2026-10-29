# institutionaldiwan.front / src/shared/api — HTTP Client, Auth Interceptors & ProblemDetails Adapter

> **Status:** Implemented for Module 01 Organization, Module 02 Identity, the Module 03
> Correspondence error catalog (documents, numbering, movements), and the Module 09 audit catalog.
> Attachment codes arrive with Module 06.

## Purpose

Single Axios HTTP client for all `entities/*/api` and `features/*/api` modules: attaches
`Authorization: Bearer <jwt>`, handles `401` session expiry, and normalizes RFC 7807
`ProblemDetails` into `ApiError`. `errorMessages.ts` turns those failures into Arabic messages —
the UI never renders the server's English `detail`.

## Contents

| File | Purpose |
| --- | --- |
| `httpClient.ts` | Axios instance with base URL from `ENV.API_BASE_URL` (`/api`), JWT request interceptor, and an error interceptor that clears the token and redirects to `/login` on `401`. |
| `problemDetails.ts` | `ApiError` (`code`/`detail`/`status`), `toApiError` normalizer, and `isConflictError`. |
| `errorMessages.ts` | `toArabicErrorMessage(error)` maps Identity, Department (including `CentralDiwanProtected`), InstitutionSetting, Lookup, Correspondence, Numbering, Movements, and Audit codes to Arabic, with HTTP/network/generic fallbacks. |
| `tokenStorage.ts` | Read/write/clear helpers for the JWT access token in `localStorage` (`diwan.token`). |
| `signalrClient.ts` | HubConnection factory — **implement only after** the server exposes hubs (not implemented). |
| `index.ts` | Barrel exporting `httpClient`, `ApiError`, `toApiError`, `isConflictError`, `toArabicErrorMessage`, `tokenStorage`. |

## Shared list envelope

```ts
export type PagedResult<T> = {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  hasPrevious: boolean;
  hasNext: boolean;
};
```

Used by documents, users, employees, external organizations, audit logs, and integrity reports.

## Conventions

- Never call `axios`/`fetch` outside this segment — import `httpClient` from `@/shared/api`.
- Export `isConflictError(err, expectedCode?)` for domain `409` codes.
- Multipart uploads (attachments): use `FormData` with the same client; do not set
  `Content-Type` manually (browser sets the boundary).
- Audit integrity POST: treat HTTP 200 + `isValid: false` as a successful RPC.

## Implementation status

| Item | State |
| --- | --- |
| `httpClient.ts`, `problemDetails.ts`, `tokenStorage.ts`, `errorMessages.ts` | Implemented; Organization errors cover central-hub protection, settings missing data, central-department validity/transfer reason, numbering lookups, and responsible employee. Identity adds inactive-role assignment and server-enforced forced-password-change (`403` `Identity.PasswordChangeRequired`) messages; `401` clears the session and redirects to login. Module 03 adds every Correspondence, Numbering, and Movements domain code the registry/details/numbering/routing screens can surface (attachments deferred to Module 06). |
| `signalrClient.ts` | Deferred until hubs exist. |

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-10-29 | agent | Module 09: added the `Audit.*` Arabic catalog (`ChainBroken`, `HashMismatch`, `InvalidCheckpointRange`, `InvalidIntegrityRange`, `NoAuditLogs`, `NothingToCheckpoint`, `PreviousLogNotFound`, `RequiredFieldsMissing`) for the checkpoint and integrity dialogs. |
| 2026-10-28 | Qoder | Module 03 Phase 1: added the full Arabic catalog for `Correspondence.*`, `Numbering.*`, and `Movements.*` so document and routing screens never surface English server detail. |
| 2026-10-28 | agent | Added `Identity.RoleInactive` and `Identity.PasswordChangeRequired` Arabic messages for inactive-role assignment and the server-enforced forced-password-change gate. |
| 2026-10-28 | agent | Document Module 01 Department/InstitutionSetting Arabic errors alongside Module 02 Identity; missing settings remain an error, not an automatic create/update trigger. |
| 2026-10-28 | agent | Added `Identity.NewPasswordSameAsOld` for the change-password form. |
| 2026-10-28 | agent | Added the user-create/update validation codes (`Identity.UsernameTooShort`, `Identity.PasswordEmpty`, `Identity.PasswordTooShort`, `Identity.UserScopeTypeInvalid`) used by the Phase 3 dialogs. |
| 2026-10-28 | agent | Added the employee validation codes (`Identity.EmployeeEmailInvalid`, `Identity.EmployeeDepartmentEmpty`) to the Arabic catalog for the `/employees` admin surface. |
| 2026-10-27 | agent | Extended the Arabic catalog with the remaining Identity validation codes plus Department and Lookup codes so user/role/employee screens never surface English server detail. |
| 2026-10-27 | agent | Implemented the client for Identity Module 01 and added `errorMessages.ts`: the UI renders Arabic messages from the domain-code catalog, never the English server `detail`. |
| 2026-10-25 | Cursor | Documented `PagedResult`, multipart upload rule, audit integrity 200 semantics, and deferred SignalR. |
| 2026-10-22 | opencode | Converted this folder's `README.md` from the Word document it was authored in to markdown. |
| 2026-10-19 | agent | Initialized `src/shared/api` segment and documented `ProblemDetails` and JWT interceptor rules. |
