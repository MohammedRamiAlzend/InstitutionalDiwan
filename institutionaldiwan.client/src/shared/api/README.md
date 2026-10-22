# institutionaldiwan.client / src/shared/api — HTTP Client, Auth Interceptors & ProblemDetails Adapter
> **Status:** Scaffolding & Rules — API client rules defined; code pending.
## Purpose
Provides the single Axios/fetch HTTP client used across all `entities/*/api` and `features/*/api` modules, attaches the `Authorization: Bearer <jwt>` header, handles `401 Unauthorized` session expiry, and normalizes `InstitutionalDiwan.Server` RFC 7807 `ProblemDetails` responses (`400`, `403`, `404`, `409`, `500`) into a strongly typed `ApiError`.
## Contents
| File | Purpose |
| --- | --- |
| `httpClient.ts` | Axios instance with base URL `/api`, JWT request interceptor, and `ProblemDetails` response error interceptor. |
| `problemDetails.ts` | Defines `ProblemDetails` interface (`type`, `title` = domain `Error.Code`, `status`, `detail` = `Error.Description`) and `ApiError` class. |
| `tokenStorage.ts` | Safe read/write/clear helpers for the JWT access token and expiration timestamp. |
| `signalrClient.ts` | Factory for `@microsoft/signalr` `HubConnectionBuilder` with automatic reconnect and JWT access token factory. |
| `index.ts` | Public barrel exporting `httpClient`, `ApiError`, `isConflictError`, `tokenStorage`, and `createHubConnection`. |
## Conventions / Notes
- Never call `axios` or `fetch` directly outside `src/shared/api/`—always import `httpClient` from `@/shared/api`.
- Conflict (`409`) helper: Export `isConflictError(err, expectedCode?)` so feature mutations (`CreateDocument`, `CreateDepartment`, `MoveDepartment`, `CreateUser`, `CreateRole`) can easily match domain conflict codes like `Numbering.OfficialNumberTaken` or `Department.CyclicHierarchy`.
## Implementation status
| Item | State |
| --- | --- |
| `httpClient.ts` & `problemDetails.ts` | ❌ Not implemented |
| `tokenStorage.ts` & `signalrClient.ts` | ❌ Not implemented |
## Change Log
| Date | Author | Change |
| --- | --- | --- |
| 2026-10-22 | opencode | Converted this folder's `README.md` from the Word document it was authored in to markdown, so the `README.md`-per-folder rule in `AGENT.md` holds and the content is diffable and agent-editable. |
| 2026-10-19 | agent | Initialized `src/shared/api` segment and documented `ProblemDetails` and JWT interceptor rules. |
