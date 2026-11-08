# institutionaldiwan.front / src/shared/api — HTTP Client, Auth Interceptors & ProblemDetails Adapter

> **Status:** Implemented for Module 01 Organization, Module 02 Identity, the Module 03
> Correspondence error catalog (documents, numbering, movements), the Module 06 attachment error
> catalog and transport (multipart upload + blob download), and the Module 09 audit catalog.

## Purpose

Single Axios HTTP client for all `entities/*/api` and `features/*/api` modules: attaches
`Authorization: Bearer <jwt>`, handles `401` session expiry, and normalizes RFC 7807
`ProblemDetails` into `ApiError`. `errorMessages.ts` turns those failures into Arabic messages —
the UI never renders the server's English `detail`. Also owns the two binary transports attachments
need: `uploadForm` (multipart with progress) and `downloadBlob` (binary read plus the
`Content-Disposition` header the caller parses for the file name).

## Contents

| File | Purpose |
| --- | --- |
| `httpClient.ts` | Axios instance with base URL from `ENV.API_BASE_URL` (`/api`), JWT request interceptor, and an `async` error interceptor that clears the token and redirects to `/login` on `401`. Also exports `uploadForm<T>(url, form, { onProgress })` for multipart posts and `downloadBlob(url, params?)` for `responseType: 'blob'` reads. |
| `problemDetails.ts` | `ApiError` (`code`/`detail`/`status`), `toApiError` normalizer, `toApiErrorAsync` for the unread-`Blob` body a binary request leaves behind, and `isConflictError`. |
| `errorMessages.ts` | `toArabicErrorMessage(error)` maps Identity, Department (including `CentralDiwanProtected`), InstitutionSetting, Lookup, Correspondence, Numbering, Movements, Attachments, and Audit codes to Arabic, with HTTP/network/generic fallbacks. |
| `tokenStorage.ts` | Read/write/clear helpers for the JWT access token in `localStorage` (`diwan.token`). |
| `signalrClient.ts` | HubConnection factory — **implement only after** the server exposes hubs (not implemented). |
| `index.ts` | Barrel exporting `httpClient`, `uploadForm`, `downloadBlob`, `ApiError`, `toApiError`, `toApiErrorAsync`, `isConflictError`, `toArabicErrorMessage`, `tokenStorage`, and the `UploadOptions` / `BlobDownload` types. |

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
- Multipart uploads (attachments): go through `uploadForm(url, formData, { onProgress })`. Never set
  `Content-Type` manually — the browser owns the boundary — and never hand-roll
  `onUploadProgress`; the helper already reports whole percentages and skips events without a
  known total.
- Binary reads (downloads): go through `downloadBlob(url, params?)` and hand the returned
  `contentDisposition` to `parseContentDisposition` from `@/shared/lib`. Build the URL from the
  endpoint only — a response's storage path is an internal detail and must never reach the user.
- Blob-response failures: the interceptor awaits `toApiErrorAsync`, so callers still get an
  `ApiError` with the real domain code. Call it directly only when catching an axios error outside
  the interceptor.
- Audit integrity POST: treat HTTP 200 + `isValid: false` as a successful RPC.

## Implementation status

| Item | State |
| --- | --- |
| `httpClient.ts`, `problemDetails.ts`, `tokenStorage.ts`, `errorMessages.ts` | Implemented; Organization errors cover central-hub protection, settings missing data, central-department validity/transfer reason, numbering lookups, and responsible employee. Identity adds inactive-role assignment and server-enforced forced-password-change (`403` `Identity.PasswordChangeRequired`) messages; `401` clears the session and redirects to login. Module 03 adds every Correspondence, Numbering, and Movements domain code the registry/details/numbering/routing screens can surface. Module 06 adds all 22 `Attachments.*` codes, including `ChangeNotesTooLong`, which exists only as a validator code and not in `DomainErrors.cs` but still reaches the client as a `ProblemDetails.title`. |
| Multipart upload / blob download helpers | ✅ Implemented (Module 06 Phase 1): `uploadForm<T>` reports whole-percent progress and skips events with no known total; `downloadBlob` returns the raw blob alongside the `content-disposition` header; and the error interceptor now awaits `toApiErrorAsync`, so a `ProblemDetails` body that arrives as an unread `Blob` still yields the real domain code instead of `Unknown`. The `Content-Disposition` parsing itself lives in `shared/lib/download.ts`. |
| `signalrClient.ts` | Deferred until hubs exist. |

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-11-08 | Claude | Added Arabic messages for `Numbering.VisitDateDuplicate` and `Numbering.VisitYearMismatch`. |
| 2026-11-04 | Claude | Added Arabic messages for `Numbering.DepartmentRegistrationOwnDepartmentOnly` and `Numbering.DepartmentRegistryMismatch`. |
| 2026-11-04 | Claude | Registry filtering by dynamic field values (plan `plans/document-registry-field-filters.md`): added the six Arabic `DocumentFields.Filter*` / `TooManyFilters` messages. |
| 2026-11-04 | Claude | Module 10 notifications front end (plan `plans/module-10-notifications-frontend.md`): added the Arabic `Notifications.*` error catalog. |
| 2026-11-03 | Claude | Added the Arabic `DocumentFields.*` error catalog (40 codes, matched one-to-one with `DomainErrors.DocumentFields`). |
| 2026-11-03 | agent | Module 08: added the Arabic `Relations.*` error messages. |
| 2026-11-03 | agent | Module 08: added the Arabic `Relations.*` error messages. |
| 2026-11-03 | agent | Module 07: added the Arabic `Archiving.*`, `Correspondence.OnlyRegisteredArchivable`, `Correspondence.DocumentArchived` and `Common.ConcurrencyConflict` messages. |
| 2026-11-01 | agent | Module 06 Phase 1: added `uploadForm` and `downloadBlob` to `httpClient.ts`, added `toApiErrorAsync` to `problemDetails.ts`, and made the response interceptor `async` so it can await that normalizer. A `responseType: 'blob'` request leaves the `ProblemDetails` body as an unread `Blob`, which the old sync path treated as a truthy object with no `title` and degraded every domain failure — archived file, unknown version, no download access — into the generic «حدث خطأ غير متوقع». |
| 2026-11-01 | agent | Module 06 Phase 0: added the `Attachments.*` Arabic catalog (22 codes: title, file type, empty/oversized file, disallowed extension, signature mismatch, archived file, version, storage provider, checksum) so the upload, versions, archive, and verify screens never surface the English server `detail`. No client change yet — multipart and blob helpers stay pending for Phase 1. |
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
