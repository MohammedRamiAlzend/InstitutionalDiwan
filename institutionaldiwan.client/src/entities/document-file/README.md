# `src/entities/document-file` — Document File Read Model (Module 06)

> **Status:** Docs only — API live; code pending.

## Purpose

Read models and queries for versioned document files: list by document, download metadata, verify
checksum. Writes live in `features/attachment-upload`.

## Planned Contents

| File | Purpose |
| --- | --- |
| `api/listDocumentFiles.ts` | `GET /api/documents/{documentId}/files` |
| `api/verifyChecksum.ts` | `GET /api/document-files/{fileId}/verify` |
| `model/types.ts` | `DocumentFile`, `DocumentFileVersion`, `AttachmentChecksum` |
| `model/queries.ts` | `documentFileKeys` + `useDocumentFiles` / `useVerifyChecksum` |
| `ui/ChecksumBadge.tsx` | Shows match/mismatch for SHA-256 |
| `index.ts` | Public barrel |

## Query keys (contract)

```ts
export const documentFileKeys = {
  all: ['document-files'] as const,
  byDocument: (documentId: string) =>
    [...documentFileKeys.all, 'by-document', documentId] as const,
  verify: (fileId: string) => [...documentFileKeys.all, 'verify', fileId] as const,
};
```

## Implementation status

| Item | State |
| --- | --- |
| Docs | ✅ |
| Code | ❌ |

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-10-25 | Cursor | Added document-file entity slice docs mapped to live Module 06 endpoints. |
