# AGENTS.md — Rules for AI Agents Working in `institutionaldiwan.client`

> This file is the client-side companion to the repository-root `AGENT.md`. Read it **before**
> changing anything under `institutionaldiwan.client/`.

## The one rule everything else follows

**Code and docs change together.** Whenever a folder's contents change, that folder's `README.md`
must be updated in the same turn — the `Contents` table, the `Implementation status` table, and a
new `Change Log` row. A change is not "done" until both happened.

## Feature-Sliced Design (FSD v2.1) is not negotiable

The six layers live in `src/` and imports may only ever flow **downward**:

```
app → pages → widgets → features → entities → shared
```

- Never import upward (`entities` may not import from `features`).
- Never import across sibling slices on the same layer (`entities/document` may not import from
  `entities/department`).
- Every slice and every `shared` segment exposes its public interface through its own
  `index.ts` barrel. Deep imports such as `@/entities/document/ui/DocumentStatusBadge` are
  forbidden — import `@/entities/document` instead.
- `shared` imports nothing from the layers above it.

## Reads live in `entities`, writes live in `features`

| Kind | Belongs to | Files |
| --- | --- | --- |
| `GET` request + `useQuery` + query-key factory | `src/entities/<slice>/` | `api/`, `model/queries.ts` |
| Read-only presentation of a domain object | `src/entities/<slice>/ui/` | badges, cards |
| `POST`/`PUT`/`DELETE` request + `useMutation` + Zod schema | `src/features/<slice>/` | `api/`, `model/*.schema.ts` |
| Interactive UI for a write | `src/features/<slice>/ui/` | forms, dialogs |

A feature that mutates must invalidate the entity query keys it affects — use the entity's query
key factory, never a hand-written string.

## Conventions

- **Arabic first.** The document root is `dir="rtl"`. Latin-only content (codes, ids, official
  numbers, dates) is rendered `dir="ltr"` inside an RTL page. Use logical CSS properties
  (`ps-*`/`pe-*`, `ms-*`/`me-*`) so a single stylesheet serves both directions.
- **One slice per route.** `pages/<slice>/` holds exactly one route page; keep it under ~120
  lines and push composite sections into `widgets/` and interactive forms into `features/`.
- **URL-driven filters.** Table filters, search terms, and active tabs belong in URL search
  params (`useSearchParams`), not local `useState`, so views are bookmarkable and shareable.
- **Server state is React Query's job.** Do not copy server data into `useState`; use the entity
  hooks. Local `useState` is for view state only.
- **Errors come from the API.** The backend answers RFC 7807 `ProblemDetails`; surface
  `error.detail` to the user and branch on `error.code` (the domain error code), never on the
  HTTP status alone.
- **Permissions are exact strings.** The codes in `src/shared/config/permissions.ts` must match
  `InstitutionalDiwan.Domain/Common/Permissions.cs` exactly. There are 14 of them:
  `DOCUMENT_VIEW`, `DOCUMENT_CREATE`, `DOCUMENT_EDIT`, `DOCUMENT_DELETE`, `DOCUMENT_APPROVE`,
  `NUMBERING_VIEW`, `NUMBERING_MANAGE`, `DEPARTMENT_MANAGE`, `ROLE_MANAGE`, `USER_MANAGE`,
  `EMPLOYEE_MANAGE`, `ACCESS_GRANT_MANAGE`, `REPORT_VIEW`, `LOOKUP_MANAGE`.
- **No deep imports, no `any`.** `npm run build` runs `tsc -b` — it must pass with no errors,
  and `npm run lint` must stay clean.

## Before you finish

- [ ] `npm run build` passes (`tsc -b && vite build`).
- [ ] `npm run lint` passes.
- [ ] Every touched folder's `README.md` reflects the new `Contents`.
- [ ] A `Change Log` row was appended to each touched folder's `README.md`.
- [ ] The new public exports are reachable through the slice's `index.ts` barrel.

## Where to look

- `knowledge.md` — the commands, the live endpoint list, the lookup slugs, and the error shape.
- `README.md` — the FSD layer map, the required npm packages, and the `vite.config.ts` proxy.
- `src/<layer>/README.md` — the rules and the planned slices of that layer.
