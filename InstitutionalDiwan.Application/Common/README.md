# Application / Common — shared response contracts

> **Status:** Implemented — `PagedResult<T>` is the wire shape of every paged list endpoint.

## Purpose

Holds the small, cross-cutting types the feature slices share but that do not belong to any one
module. Keeping them here lets the four paged list queries agree on a single JSON envelope instead of
each inventing its own.

## Contents

| File | Purpose |
| --- | --- |
| `PagedResult.cs` | `PagedResult<T>(Items, Page, PageSize, TotalCount)` plus the computed `TotalPages`, `HasPrevious`, and `HasNext`. Serializes as `{ "items", "page", "pageSize", "totalCount", "totalPages", "hasPrevious", "hasNext" }`. |

## Conventions / Notes

- A paged query returns `Result<PagedResult<TResponse>>`; the corresponding repository returns
  `PagedList<TEntity>` (`InstitutionalDiwan.Domain/Common/PagedList.cs`) so the total count travels
  with the page and the handler never issues a second query.
- Paging input is `PageRequest` (`InstitutionalDiwan.Domain/Common/PageRequest.cs`), which clamps the
  page to ≥1 and the page size to 1–100 and exposes `Skip`/`Take`.
- Do **not** page bounded reference data (lookups, permissions, roles, departments, registries,
  per-document grants/movements); those endpoints keep returning a plain array.

## Implementation status

| Item | State |
| --- | --- |
| `PagedResult<T>` | ✅ |
| Shared `PageRequest` / `PagedList<T>` (Domain/Common) | ✅ |

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-10-22 | opencode | Created the folder with `PagedResult<T>` and adopted it in `ListDocumentsQuery`, `ListUsersQuery`, `ListEmployeesQuery`, and `ListExternalOrganizationsQuery`. |
