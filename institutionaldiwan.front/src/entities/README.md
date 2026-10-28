# institutionaldiwan.front / src/entities — Business Domain Entities Layer

> **Status:** Module 01 session plus the Module 02 supporting reads (Phase 2, 2026-10-27):
> `user` (session + admin reads), `department`, `role`, `employee`, and `lookup` are implemented;
> the remaining slices are documented but not coded.

## Purpose

`src/entities/` models core institutional concepts on the client. Each slice owns TypeScript
read-model interfaces (matching backend `*Response` DTOs), TanStack Query hooks/key factories, and
read-only presentational UI. Entities never perform write mutations.

## Layer & Import Rules (FSD Strict Hierarchy)

- Allowed: `@/shared/*` only.
- Forbidden: upper layers and sibling entity slices (compose in widgets/features instead).
- Layout: `model/`, `api/`, `ui/`, `index.ts`.
- Deterministic lookup IDs live in `shared/config/lookupIds.ts` so several entities can consume them
  without importing `entities/lookup`.

## Entity Slices & Backend DTO Mapping

| Slice Folder | Backend DTOs & `GET` Endpoints | Key Read Components & Hooks | State |
| --- | --- | --- | --- |
| `user/` | `MeResponse`, `UserResponse`, effective permissions | `useMeQuery` (session), `useUsers`, `useUserById`, `useUserEffectivePermissions`, `UserScopeBadge` | ✅ Implemented |
| `department/` | `DepartmentResponse`, `DepartmentTreeNodeResponse` (`/departments`, `/tree`, `/{id}`) | `useDepartments`, `useDepartmentTree`, `useDepartmentById`, `DepartmentBadge` | ✅ Reads implemented; `toTree`/`toMermaid` pending |
| `role/` | `RoleResponse`, `PermissionResponse` (`/roles`, `/permissions`) | `useRoles`, `useRoleById`, `usePermissionsCatalog`, `RoleBadge` | ✅ Implemented |
| `employee/` | `EmployeeResponse` (paged `/employees`, `/{id}`) | `useEmployees`, `useEmployeeById` | ✅ Reads implemented |
| `lookup/` | `LookupEntryResponse` (`/lookups/{lookup}`) | `useLookupEntries`, `LookupLabel`, seeded fallbacks | ✅ Implemented (four Module 02 catalogs + generic client) |
| `document/` | Documents + access grants | `useDocuments`, `DocumentStatusBadge`, `OfficialNumberTag` | ❌ Planned |
| `document-file/` | Module 06 file + version responses (`GET /api/documents/{id}/files`, verify) | `useDocumentFiles`, `ChecksumBadge`, version list | ❌ Planned (API live) |
| `numbering-registry/` | Registries + counters | `useNumberingRegistries` | ❌ Planned |
| `movement/` | Module 05 movements, inbox/outbox, by-document | `useInbox`, `useOutbox`, `MovementStepBadge` | ❌ Planned (API live) |
| `audit-log/` | Module 09 paged logs + integrity reports | `useAuditLogs`, `HashChainPill`, `IntegrityStatusBadge` | ❌ Planned (API live) |

## Conventions

- Deterministic lookup ID constants live in `shared/config/lookupIds.ts`, re-exported by
  `entities/lookup/model/constants.ts`; never import a sibling entity for them.
- Every slice exports a typed query key factory for feature invalidation.
- Audit list reads are side-effecting on the server (self-audit row) — treat refetch as costly.
- Hierarchy and diagram builders live in the owning entity's `model/` — for example
  `department/model/toMermaid.ts` and `department/model/toTree.ts` for the departments → employees
  tree, or `movement/model/toMermaid.ts` for the document step chain. They are pure functions over
  the entity's read models so widgets can render the same data as a tree, a Mermaid diagram, or a
  chart. Rendering itself belongs to `shared/ui/mermaid-view`, never to an entity.

## Implementation status

| Entity Slice | State |
| --- | --- |
| `user/`, `role/`, `employee/`, `lookup/` | ✅ Implemented |
| `department/` | ✅ Reads implemented; tree/diagram builders pending |
| `document/`, `numbering-registry/` | ❌ Not implemented |
| `document-file/`, `movement/`, `audit-log/` | ❌ Not implemented (backend live; docs ready) |

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-10-27 | agent | Completed Phase 2: implemented `department`, `role`, `employee`, and `lookup` reads and extended `user` with admin list/detail/effective-permission hooks; all shapes verified against the live API. Moved deterministic lookup ids to `shared/config`. |
| 2026-10-27 | agent | Marked the `user/` session parts (`useMeQuery`, session store, `UserScopeBadge`) as implemented; user/employee list hooks remain pending. |
| 2026-10-25 | Buffy | Documented pure `model/` builders (`toTree`, `toMermaid`) so entity read models feed the shared tree/Mermaid visualization without owning UI. |
| 2026-10-25 | Cursor | Added `document-file`, `movement`, and `audit-log` entity slices mapped to live Modules 05/06/09 APIs. |
| 2026-10-22 | opencode | Converted this folder's `README.md` from the Word document it was authored in to markdown. |
| 2026-10-19 | agent | Initialized `src/entities` FSD layer folder and documented domain entity slices, query key conventions, and lookup handling. |
