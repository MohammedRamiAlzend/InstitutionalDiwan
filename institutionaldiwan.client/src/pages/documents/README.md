# `src/pages/documents` — Correspondence Registry & Document Details Page Slice (`/documents`)
## 1. Purpose
Renders the primary correspondence registry view at `/documents` and individual document detail view at `/documents/:id`, composing `DocumentRegistryTable` (`widgets/document-registry-table`), `CreateDocumentModal` (`features/document-create`), and `DocumentAccessModal` (`features/document-access-grant`).
## 2. Strict FSD Rules
- Allowed Imports: `src/widgets/*`, `src/features/*`, `src/entities/*`, and `src/shared/*`.
- Forbidden Imports: NEVER import from `src/app` or sibling `src/pages/*` slices.
- Public API: Export `DocumentsPage` and `DocumentDetailsPage` only via `src/pages/documents/index.ts`.
## 3. Planned Contents & Segments
- `ui/DocumentsPage.tsx` — Main registry view with header action bar and `DocumentRegistryTable`.
- `ui/DocumentDetailsPage.tsx` — Detailed metadata, numbering badge, and access control list for a single document.
- `index.ts` — Public API barrel file.
## 4. Required Packages
- `react-router-dom`, `@tanstack/react-query`, `lucide-react`
## 5. Implementation Status
- Slice folder and FSD rules established
- Implement `DocumentsPage` and `DocumentDetailsPage`
## 6. Change Log
- 2026-10-22: Converted this `README.md` from the Word document it was authored in to markdown, so the `README.md`-per-folder rule in `AGENT.md` holds and the content is diffable and agent-editable.
- 2026-10-19: Created `pages/documents` slice folder and `README.md`.
