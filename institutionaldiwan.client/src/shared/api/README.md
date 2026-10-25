# institutionaldiwan.client / src/shared/api — HTTP Client, Auth Interceptors & ProblemDetails Adapter

> **Status:** Scaffolding & Rules — API client rules defined; code pending.

## Purpose

Single Axios HTTP client for all `entities/*/api` and `features/*/api` modules: attaches
`Authorization: Bearer <jwt>`, handles `401` session expiry, and normalizes RFC 7807
`ProblemDetails` into `ApiError`.

## Contents (planned)

| File | Purpose |
| --- | --- |
| `httpClient.ts` | Axios instance with base URL `/api`, JWT request interceptor, ProblemDetails error interceptor. |
| `problemDetails.ts` | `ProblemDetails` interface + `ApiError` class (see root `AGENTS.md`). |
| `tokenStorage.ts` | Read/write/clear helpers for the JWT access token. |
| `signalrClient.ts` | HubConnection factory — **implement only after** the server exposes hubs. |
| `index.ts` | Exports `httpClient`, `ApiError`, `isConflictError`, `tokenStorage`. |

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
| `httpClient.ts` & `problemDetails.ts` | ❌ Not implemented |
| `tokenStorage.ts` | ❌ Not implemented |
| `signalrClient.ts` | ❌ Deferred until hubs exist |

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-10-25 | Cursor | Documented `PagedResult`, multipart upload rule, audit integrity 200 semantics, and deferred SignalR. |
| 2026-10-22 | opencode | Converted this folder's `README.md` from the Word document it was authored in to markdown. |
| 2026-10-19 | agent | Initialized `src/shared/api` segment and documented `ProblemDetails` and JWT interceptor rules. |
