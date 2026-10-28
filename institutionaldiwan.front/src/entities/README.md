# institutionaldiwan.front / src/entities — Business Domain Entities Layer

> **Status:** Module 01 Organization reads (`department`, `institution-setting`), Module 02
> Identity reads (`user`, `role`, `employee`), lookup catalogs, the reusable employee picker, and
> Module 03 document reads are implemented. Remaining entity slices (`document-file`, movement,
> `numbering-registry`, `audit-log`) are documentation only.

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

## Contents — Entity Slices & Backend DTO Mapping

| Slice Folder | Backend DTOs & `GET` Endpoints | Key Read Components & Hooks | State |
| --- | --- | --- | --- |
| `user/` | `MeResponse`, `UserResponse`, effective permissions | `useMeQuery` (session), `useUsers`, `useUserById`, `useUserEffectivePermissions`, `UserScopeBadge` | ✅ Implemented |
| `department/` | `DepartmentResponse`, `DepartmentTreeNodeResponse` (`/departments`, `/tree`, `/{id}`) | `useDepartments`, `useDepartmentTree`, `useDepartmentById`, `DepartmentBadge`, `departmentTreeToMermaid` | Reads and department-only Mermaid builder implemented; the widget composes lazy employee branches. |
| `institution-setting/` | `InstitutionSettingResponse` (`/institution-settings`) | `useInstitutionSettings`, `institutionSettingKeys` | Singleton read implemented; missing settings remain an error. |
| `role/` | `RoleResponse`, `PermissionResponse` (`/roles`, `/permissions`) | `useRoles`, `useRoleById`, `usePermissionsCatalog`, `RoleBadge` | Implemented. |
| `employee/` | `EmployeeResponse` (paged `/employees`, `/{id}`) | `useEmployees`, `useEmployeeById`, `EmployeePicker` | Reads and reusable paged/searchable picker implemented. |
| `lookup/` | `LookupEntryResponse` (`/lookups/{lookup}`) | `useLookupEntries`, `LookupLabel`, seeded fallbacks | Named Organization/Identity catalogs (incl. numbering mode/reset policy) plus the five Module 03 document catalogs; generic reads implemented. |
| `document/` | `DocumentResponse`, `DocumentDetailResponse`, timeline, access grants (`/documents`, `/documents/{id}`) | `useDocuments`, `useDocumentById`, `useDocumentDetails`, `useDocumentTimeline`, `useDocumentAccessGrants`, `DocumentStatusBadge`, `DocumentDirectionBadge`, `OfficialNumberTag` | ✅ Reads and presentational UI implemented; grants UI planned with Phase 4. |
| `document-file/` | Module 06 file + version responses (`GET /api/documents/{id}/files`, verify) | `useDocumentFiles`, `ChecksumBadge`, version list | ❌ Planned (API live) |
| `numbering-registry/` | Registries + counters | `useNumberingRegistries` | ❌ Planned |
| `movement/` | Module 05 movements, inbox/outbox, by-document | `useInbox`, `useOutbox`, `MovementStepBadge` | ❌ Planned (API live) |
| `audit-log/` | Module 09 paged logs + integrity reports | `useAuditLogs`, `HashChainPill`, `IntegrityStatusBadge` | ❌ Planned (API live) |

## Conventions

- Deterministic lookup ID constants live in `shared/config/lookupIds.ts`, re-exported by
  `entities/lookup/model/constants.ts`; never import a sibling entity for them.
- Every slice exports a typed query key factory for feature invalidation.
- Audit list reads are side-effecting on the server (self-audit row) — treat refetch as costly.
- Pure domain diagram builders live in the owning entity's `model/`, currently
  `department/model/toMermaid.ts` (departments only). `department/model/toTree.ts` does not exist:
  the hierarchy widget renders API tree nodes and composes lazy employee rosters across entities.
  Movement diagram builders remain planned. Rendering belongs to shared UI/widgets, not data models.

## Implementation status

| Entity Slice | State |
| --- | --- |
| `user/`, `role/`, `employee/`, `lookup/` | ✅ Implemented |
| `department/` | Reads and department-only Mermaid builder implemented; lazy employee composition lives in the widget. |
| `institution-setting/` | Singleton read, query keys, and response type implemented. |
| `employee/` picker and numbering lookups | Implemented for Module 01 forms; final verification pending. |
| `document/` | ✅ Read models, query keys/hooks, status/direction badges, and the official-number tag. |
| `numbering-registry/` | Planned only; no frontend implementation. |
| `document-file/`, `movement/`, `audit-log/` | Not implemented (backend live; docs ready). |

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-10-28 | Qoder | Module 03 Phase 2: extended `lookup` with the document direction/type/status/confidentiality/priority catalogs, their seeded Arabic rows and `DOCUMENT_TYPE_DIRECTION`. |
| 2026-10-28 | Qoder | Module 03 Phase 1: implemented the `document` entity reads (list/by-id/details/timeline/grants), query keys, status/direction badges, and the official-number tag. |
| 2026-10-28 | agent | Document institution-setting reads, the reusable employee picker, numbering catalogs, and widget-owned lazy employee composition for Module 01. |
| 2026-10-28 | agent | Added `department/model/toMermaid.ts` (pure `DepartmentTreeNodeResponse[]` → Mermaid `flowchart TD`, label sanitizing, inactive marker) for the Module 01 hierarchy widget; `toTree` still waits on employee data. |
| 2026-10-27 | agent | Completed Phase 2: implemented `department`, `role`, `employee`, and `lookup` reads and extended `user` with admin list/detail/effective-permission hooks; all shapes verified against the live API. Moved deterministic lookup ids to `shared/config`. |
| 2026-10-27 | agent | Marked the `user/` session parts (`useMeQuery`, session store, `UserScopeBadge`) as implemented; user/employee list hooks remain pending. |
| 2026-10-25 | Buffy | Documented pure `model/` builders (`toTree`, `toMermaid`) so entity read models feed the shared tree/Mermaid visualization without owning UI. |
| 2026-10-25 | Cursor | Added `document-file`, `movement`, and `audit-log` entity slices mapped to live Modules 05/06/09 APIs. |
| 2026-10-22 | opencode | Converted this folder's `README.md` from the Word document it was authored in to markdown. |
| 2026-10-19 | agent | Initialized `src/entities` FSD layer folder and documented domain entity slices, query key conventions, and lookup handling. |
