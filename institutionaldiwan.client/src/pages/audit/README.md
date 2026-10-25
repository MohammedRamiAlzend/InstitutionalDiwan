# `src/pages/audit` — Tamper-Evident Audit Log (`/audit`)

> **Status:** Docs only — Module 09 API live; page code pending.

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

`/audit?page=&pageSize=&moduleName=&actionTypeId=&fromUtc=&toUtc=&search=&sortBy=&sortDescending=&panel=logs|integrity`

## Planned Contents

| File | Purpose |
| --- | --- |
| `ui/AuditPage.tsx` | Composes `widgets/audit-log-table` + integrity panel. |
| `index.ts` | Public barrel. |

## Permissions

- Page / search / reports: `AUDIT_VIEW`
- Checkpoint + run integrity: `AUDIT_MANAGE`

## Implementation status

| Item | State |
| --- | --- |
| Slice docs | ✅ |
| Page code | ❌ |

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-10-25 | Cursor | Added audit page slice docs for Module 09 (self-audit side effect + integrity 200 semantics). |
