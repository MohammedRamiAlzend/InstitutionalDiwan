# AGENTS.md — Rules for AI Agents Working in `institutionaldiwan.client`

> This file is the client-side companion to the repository-root `AGENT.md`. Read this file **before**
> changing anything under `institutionaldiwan.client/`. Keep [`knowledge.md`](knowledge.md) in sync
> with the live backend whenever Modules or permissions change.

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
  numbers, dates, hash hex) is rendered `dir="ltr"` inside an RTL page. Use logical CSS properties
  (`ps-*`/`pe-*`, `ms-*`/`me-*`) so a single stylesheet serves both directions.
- **One slice per route.** `pages/<slice>/` holds exactly one route page; keep it under ~120
  lines and push composite sections into `widgets/` and interactive forms into `features/`.
- **URL-driven filters.** Table filters, search terms, and active tabs belong in URL search
  params (`useSearchParams`), not local `useState`, so views are bookmarkable and shareable.
- **Server state is React Query's job.** Do not copy server data into `useState`; use the entity
  hooks. Local `useState` is for view state only.
- **Errors come from the API.** The backend answers RFC 7807 `ProblemDetails`; surface
  `error.detail` to the user and branch on `error.code` (the domain error code), never on the
  HTTP status alone. Exception: `POST /api/audit-logs/integrity-checks` returns **200** with
  `isValid: false` when the chain is broken — read the body, do not treat it as a transport error.
- **Permissions are exact strings.** The codes in `src/shared/config/permissions.ts` must match
  `InstitutionalDiwan.Domain/Common/Permissions.cs` exactly. There are **16** of them:
  `DOCUMENT_VIEW`, `DOCUMENT_CREATE`, `DOCUMENT_EDIT`, `DOCUMENT_DELETE`, `DOCUMENT_APPROVE`,
  `NUMBERING_VIEW`, `NUMBERING_MANAGE`, `DEPARTMENT_MANAGE`, `ROLE_MANAGE`, `USER_MANAGE`,
  `EMPLOYEE_MANAGE`, `ACCESS_GRANT_MANAGE`, `REPORT_VIEW`, `LOOKUP_MANAGE`,
  `AUDIT_VIEW`, `AUDIT_MANAGE`.
- **Do not invent backend routes.** Live vs pending modules are listed in `knowledge.md`. Never call
  Modules 07 / 08 / 10 until those APIs exist.
- **No deep imports, no `any`.** `npm run build` runs `tsc -b` — it must pass with no errors,
  and `npm run lint` must stay clean.

## First-slice file contracts (copy shapes, then implement)

When creating the first real TypeScript files, match these public shapes so agents stay consistent.

### `src/shared/config/permissions.ts`

```ts
export const Permissions = {
  DocumentView: 'DOCUMENT_VIEW',
  DocumentCreate: 'DOCUMENT_CREATE',
  DocumentEdit: 'DOCUMENT_EDIT',
  DocumentDelete: 'DOCUMENT_DELETE',
  DocumentApprove: 'DOCUMENT_APPROVE',
  NumberingView: 'NUMBERING_VIEW',
  NumberingManage: 'NUMBERING_MANAGE',
  DepartmentManage: 'DEPARTMENT_MANAGE',
  RoleManage: 'ROLE_MANAGE',
  UserManage: 'USER_MANAGE',
  EmployeeManage: 'EMPLOYEE_MANAGE',
  AccessGrantManage: 'ACCESS_GRANT_MANAGE',
  ReportView: 'REPORT_VIEW',
  LookupManage: 'LOOKUP_MANAGE',
  AuditView: 'AUDIT_VIEW',
  AuditManage: 'AUDIT_MANAGE',
} as const;

export type PermissionCode = (typeof Permissions)[keyof typeof Permissions];

export const AllPermissions: readonly PermissionCode[] = Object.values(Permissions);
```

### `src/shared/api` — `ApiError` + query-key factory pattern

```ts
export class ApiError extends Error {
  constructor(
    public readonly code: string,   // ProblemDetails.title
    public readonly detail: string, // ProblemDetails.detail
    public readonly status: number,
  ) {
    super(detail);
  }
}

// entities/document/model/queries.ts
export const documentKeys = {
  all: ['documents'] as const,
  list: (params: object) => [...documentKeys.all, 'list', params] as const,
  detail: (id: string) => [...documentKeys.all, 'detail', id] as const,
};
```

### Entity barrel

Every new entity/feature slice ships `index.ts` that re-exports only the public hooks/components —
never deep paths from outside the slice.

## UI / state contract (every interactive screen)

| State | Required behavior |
| --- | --- |
| Loading | Region skeleton / spinner; never a blank content area |
| Empty | Arabic-first empty copy; optional primary CTA |
| Error | Show `ApiError.detail`; offer retry when idempotent |
| Forbidden | Dedicated “no permission” state for missing route policy |
| Success mutation | Toast (`sonner`) + invalidate the owning entity query keys |

## Before you finish

- [ ] `npm run build` passes (`tsc -b && vite build`).
- [ ] `npm run lint` passes.
- [ ] Every touched folder's `README.md` reflects the new `Contents`.
- [ ] A `Change Log` row was appended to each touched folder's `README.md`.
- [ ] The new public exports are reachable through the slice's `index.ts` barrel.
- [ ] If endpoints or permissions changed, `knowledge.md` was updated in the same turn.

## Where to look

- `knowledge.md` — as-is/to-be, live endpoint list (incl. Attachments + Audit), lookup slugs, errors.
- `README.md` — FSD map, package install commands, UI/design contract, implementation status.
- `src/<layer>/README.md` — rules and planned slices for that layer.
