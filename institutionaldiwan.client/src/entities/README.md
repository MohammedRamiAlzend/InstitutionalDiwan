# institutionaldiwan.client / src/entities — Business Domain Entities Layer
> **Status:** Scaffolding & Rules — FSD entity slices mapped to backend Domain aggregates and read queries; code pending.
## Purpose
`src/entities/` models the core institutional domain concepts on the client. Each slice owns the TypeScript read-model interfaces (matching the backend `*Response` DTOs), TanStack Query `useQuery` hooks and query-key factories (`GET` endpoints), and pure read-only presentational components (status badges, entity cards, summary rows, lookup label resolvers). Entities must never perform write mutations or know about feature-specific workflows.
## Layer & Import Rules (FSD Strict Hierarchy)
`entities` sits near the bottom of the FSD hierarchy (app → pages → widgets → features → entities → shared):
- Allowed imports: `@/shared/*` only.
- Forbidden imports: Cannot import from `@/app/*`, `@/pages/*`, `@/widgets/*`, `@/features/*`, or another entity slice in `@/entities/*` (if an entity view needs data from two entities, compose them in `features/` or `widgets/`, or use `@/entities/<other>/@x/<current>` explicit cross-import notation only when strictly required by domain nesting).
- Internal segment layout per entity slice (`entities/<slice>/`):
   - `model/` — TypeScript DTO interfaces, query key factories (`documentKeys.all`, `documentKeys.detail(id)`), and `useQuery` hooks.
   - `api/` — Read-only `GET` client functions using `@/shared/api`.
   - `ui/` — Presentational components (`DocumentStatusBadge`, `DepartmentBadge`, `UserScopeBadge`, `RolePermissionsList`).
   - `index.ts` — Public API barrel.
## Required Packages
| Package | Version / Role | Used in `src/entities/` for |
| --- | --- | --- |
| `@tanstack/react-query` | `^5.x` | `useQuery`, `queryOptions`, query key factories, and lookup caching (`staleTime: Infinity` for reference lookups) |
| `zustand` | `^5.x` | Lightweight client store in `entities/user` for current authenticated user token & decoded claims |
## Planned Entity Slices & Backend DTO Mapping
| Slice Folder | Backend DTOs & `GET` Endpoints | Key Read Components & Hooks | State |
| --- | --- | --- | --- |
| `document/` | `DocumentResponse`, `DocumentAccessGrantResponse` (`GET /api/documents`, `GET /api/documents/{id}`, `GET /api/documents/{id}/access-grants`) | `useDocuments`, `useDocumentById`, `useDocumentAccessGrants`, `DocumentStatusBadge`, `DocumentDirectionBadge`, `OfficialNumberTag` | ❌ Planned |
| `department/` | `DepartmentResponse`, `DepartmentTreeNodeResponse` (`GET /api/departments`, `GET /api/departments/{id}`, `GET /api/departments/tree`) | `useDepartments`, `useDepartmentTree`, `DepartmentNodeCard` | ❌ Planned |
| `user/` | `MeResponse`, `UserResponse`, `EmployeeResponse` (`GET /api/auth/me`, `GET /api/users`, `GET /api/users/{id}`, `GET /api/employees`, `GET /api/employees/{id}`) | `useCurrentUser`, `useUsers`, `useEmployees`, `UserAvatar`, `UserScopeBadge` | ❌ Planned |
| `role/` | `RoleResponse`, `PermissionResponse` (`GET /api/roles`, `GET /api/roles/{id}`, `GET /api/permissions`) | `useRoles`, `useRoleById`, `usePermissionsCatalog`, `RoleBadge`, `PermissionCodeChip` | ❌ Planned |
| `numbering-registry/` | `NumberingRegistryResponse`, `NumberingCounterResponse` (`GET /api/numbering/registries`, `GET /api/numbering/registries/{id}`) | `useNumberingRegistries`, `useNumberingRegistryById`, `RegistryScopeCard` | ❌ Planned |
| `movement/` | Module 05 `DocumentMovement`, `DocumentMovementStep`, `DocumentMovementEvent` read models | `useDocumentMovements`, `MovementStepBadge` | ❌ Planned |
| `lookup/` | All 34 integer-keyed reference tables (`LookupEntity`: `id`, `desc`, `descAr`, `isActive`) | `useLookupMap`, `<LookupLabel lookup="documentTypes" id={doc.documentTypeId} />` resolving Arabic (`descAr`) or English (`desc`) labels | ❌ Planned |
## Conventions / Vibe-Coding Notes (OpenCode & Freebuff)
- Deterministic Lookup IDs: Mirror the backend's deterministic integer IDs from `InstitutionalDiwan.Domain` in `entities/lookup/model/constants.ts` (e.g., `DocumentDirection = { Incoming: 1, Outgoing: 2, Internal: 3, All: 4 }`, `DocumentStatusType = { Draft: 1, Registered: 2, InMovement: 3, Archived: 4, Canceled: 5 }`) as a fallback until `/api/lookups/{lookup}` is wired.
- Query Key Discipline: Every entity slice must export a typed query key factory (e.g., `departmentKeys = { all: ['departments'] as const, tree: () => [...departmentKeys.all, 'tree'] as const, detail: (id: string) => [...departmentKeys.all, id] as const }`) so `features/*` mutations can invalidate cache keys predictably.
- Golden Rule: Update this `README.md` and the touched entity slice's `README.md` in the same turn whenever files change.
## Implementation status
| Entity Slice | State |
| --- | --- |
| `document/`, `department/`, `user/`, `role/`, `numbering-registry/`, `lookup/` | ❌ Not implemented |
| `movement/` | ❌ Not implemented |
## Change Log
| Date | Author | Change |
| --- | --- | --- |
| 2026-10-22 | opencode | Converted this folder's `README.md` from the Word document it was authored in to markdown, so the `README.md`-per-folder rule in `AGENT.md` holds and the content is diffable and agent-editable. |
| 2026-10-19 | agent | Initialized `src/entities` FSD layer folder and documented domain entity slices, query key conventions, and lookup handling. |
