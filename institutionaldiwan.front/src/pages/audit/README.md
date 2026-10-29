# `src/pages/audit` — Tamper-Evident Audit Log (`/audit`)

> **Status:** Implemented — `/audit` with the log panel and the integrity-report panel, route + sidebar active.

## Purpose

Compliance surface for the append-only SHA-256 audit chain: filtered search, integrity reports, and
(for managers) checkpoint + integrity-check actions.

## Critical semantics

- Every `GET /api/audit-logs` **appends a View self-audit row** — debounce filters; avoid aggressive
  refetch intervals.
- `POST .../integrity-checks` returns **HTTP 200** with `isValid: false` when the chain is broken —
  render a failure banner from the body, not a transport error toast.
- Hash columns (`previousHash`, `eventHash`) are `dir="ltr"` monospace.

## Routes & URL state

`/audit?panel=logs|integrity&page=&pageSize=&search=&sort=&dir=&moduleName=&actionTypeId=&fromUtc=&toUtc=`

The page is wrapped in `RequirePermission` for `AUDIT_VIEW`. Two tab cards switch the panel and reset
the query so each grid starts clean.

## Table contract

The log and the integrity-report list both use `shared/ui/data-table` (table ids `audit-logs` and
`audit-integrity-reports`): server-side paging/filtering from the URL, a column chooser persisted in
`localStorage` (`auditLogId` / `occurredAtUtc` stay visible), and permission-filtered row and header
context menus (`AUDIT_MANAGE` items hidden otherwise).

## Contents

| File | Purpose |
| --- | --- |
| `ui/AuditPage.tsx` | Composes `widgets/audit-log-table` (log + integrity panels). |
| `index.ts` | Public barrel. |

## Permissions

- Page / search / reports: `AUDIT_VIEW`
- Checkpoint + run integrity: `AUDIT_MANAGE`

## Implementation status

| Item | State |
| --- | --- |
| Slice docs | ✅ |
| Page code | ✅ |

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-10-25 | Buffy | Documented the shared data-grid contract and context menus for the audit log and integrity reports. |
| 2026-10-25 | Cursor | Added audit page slice docs for Module 09 (self-audit side effect + integrity 200 semantics). |
| 2026-10-29 | Cursor | Implemented the page, the panel tabs, the route, and the sidebar entry; corrected the URL state to the shared `sort`/`dir` contract. |
