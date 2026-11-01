# `src/entities/document-file` — Document File Read Model (Module 06)

> **Status:** ✅ Reads implemented (2026-11-01, Module 06 Phases 0, 2 and 4) — list, download
> (including a named version), on-demand checksum verify, query keys/hooks, two badges, and the two
> helpers the grid and the dialogs share (`downloadFileVersion`, `currentVersionOf`). Consumed by
> `widgets/document-attachments-panel` and `features/attachment-upload` on the document-details
> «الملفات» tab.

## Purpose

Read models and queries for versioned document files: list by document, download a version, verify a
stored checksum, and present the archive/integrity state. Writes live in
`features/attachment-upload`; the grid and row actions live in
`widgets/document-attachments-panel`.

## Contents

| File | Purpose |
| --- | --- |
| `api/documentFileApi.ts` | `listByDocument(documentId)` → `GET /api/documents/{documentId}/files`; `download(fileId, versionId?)` → `GET /api/document-files/{fileId}/download` as `{ blob, contentDisposition }`; `verify(fileId, versionId?)` → `GET /api/document-files/{fileId}/verify`. |
| `api/downloadFileVersion.ts` | `downloadFileVersion(file, version?)` — streams one version (the current one when omitted) to disk and returns the saved name; the name precedence (header → `originalFileName` → title) lives here so the grid and the versions dialog cannot disagree. |
| `model/types.ts` | `DocumentFileResponse`, `DocumentFileVersionResponse`, `AttachmentChecksumResponse` — camelCase mirrors of the Application contracts. |
| `model/queries.ts` | `documentFileKeys` factory, `useDocumentFiles`, `useVerifyChecksum`. |
| `model/versions.ts` | `currentVersionOf(file)` — `versions.find(isCurrent) ?? versions[0]`, `undefined` for an empty history. |
| `ui/ChecksumBadge.tsx` | «بصمة مطابقة» / «بصمة غير مطابقة» / «لم يُتحقق منها» from a nullable `isMatch`. |
| `ui/FileArchivedBadge.tsx` | «نشط» / «مؤرشف». Archiving stops new versions but never hides the file or its downloads. |
| `index.ts` | Public barrel — import from `@/entities/document-file`, never from a subpath. |

## Query keys (contract)

```ts
export const documentFileKeys = {
  all: ['document-files'] as const,
  byDocument: (documentId: string) =>
    [...documentFileKeys.all, 'by-document', documentId] as const,
  verify: (fileId: string, versionId?: string | null) =>
    [...documentFileKeys.all, 'verify', fileId, versionId ?? 'current'] as const,
};
```

Mutations in `features/attachment-upload` invalidate `documentFileKeys.byDocument(documentId)`.
`verify` keys are deliberately separate so an integrity check never invalidates or refetches the list.

## Backend contract notes

- The list route is unpaged and unfiltered: **archived files are included**, files come oldest first
  (`createdAt`), and each file's `versions` come newest first (`versionNumber`), so `versions[0]` is
  the current version. Grids therefore use `shared/ui/data-table` in `client` mode.
- `GET /api/document-files/by-document/{documentId}` returns the identical payload; this slice uses
  the document-scoped route only (`knowledge.md` — "Prefer the document-scoped routes for the UI").
- Reads need `DOCUMENT_VIEW` **plus** document access level VIEW (verify) or DOWNLOAD (download);
  the permission alone is not sufficient and the caller's access level is not exposed to the client.
  Download is therefore offered to every viewer and its `403` is translated to Arabic on failure
  rather than predicted up front.
- `download` returns the bytes as a `Blob` plus the raw `Content-Disposition`. `downloadFileVersion`
  resolves the file name — header first, then `originalFileName`, then the title — with
  `parseContentDisposition` and `saveBlobAsFile` from `@/shared/lib`; callers pass the version they
  want and translate the thrown `ApiError` themselves.
- `DocumentFileVersionResponse.relativePath` is the server's internal storage path. It is typed for
  fidelity but must never be rendered or copied into the UI.
- `useVerifyChecksum` is disabled unless the caller passes `enabled: true`, because the handler
  re-reads and re-hashes the stored bytes. `features/attachment-upload/ui/VerifyChecksumDialog.tsx`
  passes `enabled: open`, so the read happens only while that dialog is shown.

## Implementation status

| Item | State |
| --- | --- |
| Docs | ✅ |
| Types, API client, query keys/hooks | ✅ Module 06 Phase 0 |
| `ChecksumBadge`, `FileArchivedBadge` | ✅ Module 06 Phase 0; `FileArchivedBadge` mounted in Phase 2, `ChecksumBadge` mounted by the Phase 4 verify and upload dialogs |
| Download read | ✅ Module 06 Phase 2; `downloadFileVersion` shared with the versions dialog in Phase 4 |
| Version list UI / grid | ✅ Module 06 Phase 2 (`widgets/document-attachments-panel`); the per-file versions dialog landed in Phase 4 |

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-11-01 | agent | Module 06 Phase 4: extracted `currentVersionOf` into `model/versions.ts` and added `api/downloadFileVersion.ts`, so the grid, the versions dialog, and the two upload dialogs resolve the current version and the saved file name through one pair of functions instead of three copies. |
| 2026-11-01 | agent | Module 06 Phase 2: added `documentFileApi.download(fileId, versionId?)`, returning `{ blob, contentDisposition }` through `shared/api`'s `downloadBlob` so the widget can save the bytes under the server's own file name. Documented that the per-document DOWNLOAD denial cannot be predicted client-side and is surfaced as an Arabic `403` instead. |
| 2026-11-01 | agent | Module 06 Phase 0: implemented the read slice — `documentFileApi` (document-scoped list + on-demand verify), the three response types, `documentFileKeys` with a `versionId`-aware `verify` key, `useDocumentFiles` (30-second `staleTime`, previous-data placeholder) and `useVerifyChecksum` (opt-in `enabled`), plus `ChecksumBadge` and `FileArchivedBadge`. Renamed the documented `listDocumentFiles.ts`/`verifyChecksum.ts` pair to a single `documentFileApi.ts` and the `DocumentFile`/`DocumentFileVersion`/`AttachmentChecksum` types to `*Response`, matching the convention in `entities/document`. |
| 2026-10-25 | Cursor | Added document-file entity slice docs mapped to live Module 06 endpoints. |
