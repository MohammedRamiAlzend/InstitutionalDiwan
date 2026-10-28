# institutionaldiwan.front / src/shared/api — HTTP Client, Auth Interceptors & ProblemDetails Adapter

> **Status:** Implemented for Identity Module 01 and the Module 02 admin foundation.

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
| `errorMessages.ts` | Arabic message catalog keyed by domain error code (all Identity codes — including the user, role, and employee validation codes — plus the Department and Lookup codes used by identity pickers), with Arabic HTTP-status fallbacks and a generic fallback — `toArabicErrorMessage(error)`. |
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
| `httpClient.ts`, `problemDetails.ts`, `tokenStorage.ts`, `errorMessages.ts` | ✅ Implemented |
| `signalrClient.ts` | ❌ Deferred until hubs exist |

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-10-28 | agent | Added `Identity.NewPasswordSameAsOld` for the change-password form. |
| 2026-10-28 | agent | Added the user-create/update validation codes (`Identity.UsernameTooShort`, `Identity.PasswordEmpty`, `Identity.PasswordTooShort`, `Identity.UserScopeTypeInvalid`) used by the Phase 3 dialogs. |
| 2026-10-28 | agent | Added the employee validation codes (`Identity.EmployeeEmailInvalid`, `Identity.EmployeeDepartmentEmpty`) to the Arabic catalog for the `/employees` admin surface. |
| 2026-10-27 | agent | Extended the Arabic catalog with the remaining Identity validation codes plus Department and Lookup codes so user/role/employee screens never surface English server detail. |
| 2026-10-27 | agent | Implemented the client for Identity Module 01 and added `errorMessages.ts`: the UI renders Arabic messages from the domain-code catalog, never the English server `detail`. |
| 2026-10-25 | Cursor | Documented `PagedResult`, multipart upload rule, audit integrity 200 semantics, and deferred SignalR. |
| 2026-10-22 | opencode | Converted this folder's `README.md` from the Word document it was authored in to markdown. |
| 2026-10-19 | agent | Initialized `src/shared/api` segment and documented `ProblemDetails` and JWT interceptor rules. |
