# institutionaldiwan.client / src/entities — Business Domain Entities Layer

> **Status:** Scaffolding & Rules — FSD entity slices mapped to backend Domain aggregates and read
> queries; code pending. Includes planned slices for document files, movements, and audit logs.

## Purpose

`src/entities/` models core institutional concepts on the client. Each slice owns TypeScript
read-model interfaces (matching backend `*Response` DTOs), TanStack Query hooks/key factories, and
read-only presentational UI. Entities never perform write mutations.

## Layer & Import Rules (FSD Strict Hierarchy)

- Allowed: `@/shared/*` only.
- Forbidden: upper layers and sibling entity slices (compose in widgets/features instead).
- Layout: `model/`, `api/`, `ui/`, `index.ts`.

## Planned Entity Slices & Backend DTO Mapping

| Slice Folder | Backend DTOs & `GET` Endpoints | Key Read Components & Hooks | State |
| --- | --- | --- | --- |
| `document/` | Documents + access grants | `useDocuments`, `DocumentStatusBadge`, `OfficialNumberTag` | ❌ Planned |
| `document-file/` | Module 06 file + version responses (`GET /api/documents/{id}/files`, verify) | `useDocumentFiles`, `ChecksumBadge`, version list | ❌ Planned (API live) |
| `department/` | Departments + tree | `useDepartments`, `useDepartmentTree` | ❌ Planned |
| `user/` | `MeResponse`, users, employees | `useCurrentUser`, `UserScopeBadge` | ❌ Planned |
| `role/` | Roles + permissions catalog | `useRoles`, `PermissionCodeChip` | ❌ Planned |
| `numbering-registry/` | Registries + counters | `useNumberingRegistries` | ❌ Planned |
| `movement/` | Module 05 movements, inbox/outbox, by-document | `useInbox`, `useOutbox`, `MovementStepBadge` | ❌ Planned (API live) |
| `audit-log/` | Module 09 paged logs + integrity reports | `useAuditLogs`, `HashChainPill`, `IntegrityStatusBadge` | ❌ Planned (API live) |
| `lookup/` | All 34 lookup catalogs | `useLookupMap`, `<LookupLabel />` | ❌ Planned |

## Conventions

- Deterministic lookup ID constants may live in `entities/lookup/model/constants.ts` as a fallback.
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
| `document/`, `department/`, `user/`, `role/`, `numbering-registry/`, `lookup/` | ❌ Not implemented |
| `document-file/`, `movement/`, `audit-log/` | ❌ Not implemented (backend live; docs ready) |

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-10-25 | Buffy | Documented pure `model/` builders (`toTree`, `toMermaid`) so entity read models feed the shared tree/Mermaid visualization without owning UI. |
| 2026-10-25 | Cursor | Added `document-file`, `movement`, and `audit-log` entity slices mapped to live Modules 05/06/09 APIs. |
| 2026-10-22 | opencode | Converted this folder's `README.md` from the Word document it was authored in to markdown. |
| 2026-10-19 | agent | Initialized `src/entities` FSD layer folder and documented domain entity slices, query key conventions, and lookup handling. |
