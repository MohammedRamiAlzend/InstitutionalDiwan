# `src/features/document-create` — Document Registration & Issuing Feature Slice
## 1. Purpose
Handles the creation and registration of Incoming (وارد), Outgoing (صادر), and Internal (داخلي) institutional documents (`POST /api/documents`), including automatic sequential numbering and initial access grants.
## 2. Strict FSD Rules
- Allowed Imports: `src/entities/document`, `src/entities/department`, `src/entities/lookup`, `src/entities/numbering-registry`, and `src/shared/*`.
- Forbidden Imports: NEVER import from `src/app`, `src/pages`, `src/widgets`, or sibling `src/features/*` slices.
- Public API: Export `CreateDocumentForm`, `CreateDocumentModal`, and `useCreateDocumentMutation` only via `src/features/document-create/index.ts`.
## 3. Planned Contents & Segments
- `ui/CreateDocumentForm.tsx` — Multi-field RTL form with dynamic direction, type, security level, priority, and department selectors.
- `ui/CreateDocumentModal.tsx` — Dialog wrapper for creating a document from the registry page.
- `model/create-document.schema.ts` — Zod validation schema matching `CreateDocumentRequestDto`.
- `api/use-create-document.ts` — `useMutation` calling `POST /api/documents` and invalidating `documentKeys.lists()`.
- `index.ts` — Public API barrel file.
## 4. Required Packages
- `@tanstack/react-query`, `react-hook-form`, `zod`, `@hookform/resolvers`, `sonner`
## 5. Implementation Status
- Slice folder and FSD rules established
- Implement `CreateDocumentForm` and `useCreateDocumentMutation`
## 6. Change Log
- 2026-10-22: Converted this `README.md` from the Word document it was authored in to markdown, so the `README.md`-per-folder rule in `AGENT.md` holds and the content is diffable and agent-editable.
- 2026-10-19: Created `features/document-create` slice folder and `README.md`.
