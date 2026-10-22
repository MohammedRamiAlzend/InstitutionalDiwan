# `src/features/document-access-grant` — Document Access Control & Sharing Feature Slice
## 1. Purpose
Manages document-level access grants (`DocumentAccess`), allowing authorized users to grant or revoke view/edit/forward permissions on confidential or restricted documents to specific departments or users.
## 2. Strict FSD Rules
- Allowed Imports: `src/entities/document`, `src/entities/department`, `src/entities/user`, and `src/shared/*`.
- Forbidden Imports: NEVER import from `src/app`, `src/pages`, `src/widgets`, or sibling `src/features/*` slices.
- Public API: Export `DocumentAccessModal` and access grant mutations via `src/features/document-access-grant/index.ts`.
## 3. Planned Contents & Segments
- `ui/DocumentAccessModal.tsx` — Dialog for viewing current grantees and adding/removing department or user access grants.
- `model/access-grant.schema.ts` — Zod validation schema for access grant payloads.
- `api/use-document-access-mutations.ts` — Mutations for granting/revoking access and invalidating `documentKeys.detail(id)`.
- `index.ts` — Public API barrel file.
## 4. Required Packages
- `@tanstack/react-query`, `react-hook-form`, `zod`, `@hookform/resolvers`, `sonner`
## 5. Implementation Status
- Slice folder and FSD rules established
- Implement `DocumentAccessModal` and access grant mutations
## 6. Change Log
- 2026-10-22: Converted this `README.md` from the Word document it was authored in to markdown, so the `README.md`-per-folder rule in `AGENT.md` holds and the content is diffable and agent-editable.
- 2026-10-19: Created `features/document-access-grant` slice folder and `README.md`.
