# Application / Features / Attachments — versioned document files (Module 06)

> **Status:** Implemented — atomic initial upload (file + version 1), later versioned uploads,
> archiving, listing, download, and checksum verification are live, backed by the local file
> storage service.

## Purpose

Module 06 stores every document scan, attachment, signature, or other file as a `DocumentFile`
whose binary content is an immutable, sequential chain of `DocumentFileVersion` rows. Each version
records the provider, relative path, original name, MIME type, size, and a **64-character SHA-256
checksum** computed while the upload streamed to storage. Uploads are validated before anything is
written: the extension must be allowed by the file type, the size must be within its limit, and the
**magic bytes** must match the real format.

The slice also enforces document access through `IDocumentAccessEvaluator`: writes (create, upload,
archive) require `MANAGE`, listing/verification require `VIEW`, and downloading requires `DOWNLOAD`.

## Contents

| File | Purpose |
| --- | --- |
| `CreateDocumentFileCommand.cs` / `…Handler.cs` / `…Validator.cs` | Opens a file's version chain with its metadata after checking the document, file type, and MANAGE access; the first binary is uploaded separately. Kept for compatibility. |
| `CreateDocumentFileWithVersionCommand.cs` / `…Handler.cs` / `…Validator.cs` | The planned initial upload: creates the file and stores version 1 in one use case and one save, so no file can exist without its first version. |
| `UploadDocumentFileVersionCommand.cs` / `…Handler.cs` / `…Validator.cs` | Buffers and validates the upload against the file type, writes it through `IFileStorageService` (SHA-256 computed in the same pass), and appends the next immutable version through `DocumentFile.AddVersion(...)`. |
| `AttachmentUploader.cs` | Shared upload pipeline used by both initial and later uploads: size guard, one buffered copy for magic-byte inspection, default-provider resolution, and streamed storage. |
| `ArchiveDocumentFileCommand.cs` / `…Handler.cs` | Archives a file so no further versions can be uploaded; existing versions stay readable. |
| `ListDocumentFilesQuery.cs` / `…Handler.cs` | A document's files, oldest first, each with its full version history. |
| `DownloadDocumentFileVersionQuery.cs` / `…Handler.cs` | Opens the current (or a named) version's bytes after a DOWNLOAD access check and resolves the provider that stored it. |
| `VerifyAttachmentChecksumQuery.cs` / `…Handler.cs` | Re-reads a stored version, recomputes its SHA-256, and returns both checksums plus `isMatch`. |
| `DocumentFileResponse.cs` | Wire model for a file plus its `DocumentFileVersionResponse` rows. |
| `DocumentFileDownload.cs` | Success payload of the download query: stream, original name, MIME type, size. |
| `AttachmentChecksumResponse.cs` | Recorded vs. recomputed checksum and the derived `IsMatch` flag. |

## Conventions / Notes

- **The planned initial upload is atomic.** `POST /api/documents/{documentId}/files` (multipart)
  uses `CreateDocumentFileWithVersionCommand`: it validates metadata and binary, stores the bytes,
  appends version 1, and saves once. The metadata-only `POST /api/document-files` remains for
  compatibility and still needs a separate first-version upload.
- **Versions are immutable and sequential.** `DocumentFile.AddVersion` numbers them
  (`CurrentVersionNumber + 1`), marks the previous current version as superseded, and rejects an
  archived file. Version numbers are never reused.
- **The domain is the last line of defence for the checksum.** `DocumentFileVersion.Create` only
  accepts exactly 64 hex characters and a positive size; the storage service supplies the value.
- **Validation happens before storage.** `IFileSignatureValidator` checks `AllowedExtensionsCsv`,
  `MaxFileSizeBytes`, and the magic bytes for PDF/TIFF/PNG/JPEG/ZIP-based Office/legacy
  Office/SVG files. A renamed executable therefore cannot be stored as a PDF.
- **The provider is resolved, not hard-coded.** Uploads use the active default
  `FILE_STORAGE_PROVIDERS` row; downloads resolve the provider recorded on the version.
- **Storage errors are domain errors.** A missing file answers `Attachments.FileNotFound`; a missing
  provider answers `Attachments.StorageProviderNotFound`.

## Implementation status

| Item | State |
| --- | --- |
| `CreateDocumentFileCommand` (+ handler, validator) | ✅ |
| `CreateDocumentFileWithVersionCommand` (+ handler, validator) | ✅ |
| `UploadDocumentFileVersionCommand` (+ handler, validator) | ✅ |
| `ArchiveDocumentFileCommand` (+ handler) | ✅ |
| `ListDocumentFilesQuery`, `DownloadDocumentFileVersionQuery`, `VerifyAttachmentChecksumQuery` (+ handlers) | ✅ |
| SHA-256 streaming + magic-byte validation | ✅ (`Infrastructure/Storage/`) |

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-10-25 | opencode | Added the planned atomic initial upload (`CreateDocumentFileWithVersionCommand` + `AttachmentUploader`) and the document-scoped `POST/GET /api/documents/{documentId}/files` routes, while keeping the metadata-only create path. |
| 2026-10-25 | opencode | Implemented the Module 06 slice: create/upload/archive commands, list/download/verify queries, document access checks, and the response models; depends on the new `Abstractions/Storage` contracts. |
