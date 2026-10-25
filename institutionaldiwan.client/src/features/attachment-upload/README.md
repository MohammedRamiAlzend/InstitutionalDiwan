# `src/features/attachment-upload` — Upload / Archive Document Files (Module 06)

> **Status:** Docs only — API live; code pending.

## Purpose

Multipart upload and archive mutations for document files. Prefer atomic
`POST /api/documents/{documentId}/files` for first upload; use version upload for subsequent
versions. Invalidate `documentFileKeys.byDocument(documentId)` on success.

## Planned Contents

| File | Purpose |
| --- | --- |
| `api/uploadInitial.ts` | Multipart create-with-version |
| `api/uploadVersion.ts` | `POST /api/document-files/{fileId}/versions` |
| `api/archiveFile.ts` | `POST /api/document-files/{fileId}/archive` |
| `model/upload.schema.ts` | Zod: title, fileTypeId, isPrimaryScan, File required |
| `ui/UploadAttachmentDialog.tsx` | Form + progress |
| `ui/ArchiveFileButton.tsx` | Confirm + mutate |
| `index.ts` | Public barrel |

## Domain errors to surface

`Attachments.*` (file type inactive, signature mismatch, size, storage) and access-denied when the
caller lacks MANAGE on the document.

## Implementation status

| Item | State |
| --- | --- |
| Docs | ✅ |
| Code | ❌ |

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-10-25 | Cursor | Added attachment-upload feature docs for live Module 06 endpoints. |
