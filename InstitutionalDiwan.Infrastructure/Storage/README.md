# Infrastructure / Storage

> **Status:** Implemented — local file-system storage, streamed SHA-256 checksums, and magic-byte
> validation are live. S3-compatible storage is still optional/planned.

## Purpose

Implements physical binary file storage and cryptographic verification for Module 06
(Attachments & Files), satisfying `Application/Abstractions/Storage/IFileStorageService` and
`IFileSignatureValidator`.

## Contents

| File | Purpose |
| --- | --- |
| `LocalFileStorageService.cs` | Stores uploads on disk under `{Year}/{Month}/{DocumentId}/{Random}.ext` and computes the **SHA-256 checksum** while streaming the bytes (one pass). Also opens stored files for reading, refusing paths that escape the configured root, and exposes `ComputeSha256Async`. |
| `FileSignatureValidator.cs` | Validates an upload against `DocumentFileType.AllowedExtensionsCsv` and `MaxFileSizeBytes`, then checks the **magic bytes** for PDF, PNG, JPEG, TIFF, ZIP-based Office (`docx`/`xlsx`/`pptx`/`zip`), legacy OLE Office (`doc`/`xls`/`ppt`), and SVG; a renamed executable is rejected with `Attachments.SignatureMismatch`. |

## Selected provider

`FILE_STORAGE_PROVIDERS` holds `Code`, `ProviderType` (`LOCAL` / `S3` / `MINIO`),
`BasePathOrEndpoint`, `IsDefault`, and `IsActive`. The upload handler resolves the active row with
`IsDefault = true` (falling back to the first active row); downloads resolve the provider recorded on
the `DocumentFileVersion`. A relative `BasePathOrEndpoint` such as the seeded `storage/documents` is
anchored at the application directory.

## Path layout and safety

```
{BasePathOrEndpoint}/{yyyy}/{MM}/{DocumentId}/{random-guid}{ext}
```

`OpenReadAsync` resolves the relative path under the root and rejects any path that would escape it
(`UnauthorizedAccessException`), so a tampered database row cannot read arbitrary files.

## Domain contract it satisfies

`DocumentFile.AddVersion(storageProviderId, relativePath, originalFileName, mimeType, fileSizeBytes,
checksumSha256, uploadedByUserId, changeNotes)` validates the provider id, path, positive size,
64-hex checksum, and version sequencing. The storage service returns exactly that checksum; the
domain remains the final guard.

## Implementation status

| Component | State |
| --- | --- |
| `LocalFileStorageService` | ✅ Implemented (streamed SHA-256, safe root resolution) |
| `FileSignatureValidator` | ✅ Implemented (extension, size, magic bytes) |
| `S3CompatibleStorageService` | ❌ Not implemented (planned for MinIO/S3; MinIO/S3 provider rows are seeded but no service resolves them yet) |

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-10-25 | opencode | Implemented Module 06 storage: `LocalFileStorageService` (one-pass SHA-256, `{Year}/{Month}/{DocumentId}` layout, root-escape guard) and `FileSignatureValidator` (extension allow-list, size limit, magic bytes). |
