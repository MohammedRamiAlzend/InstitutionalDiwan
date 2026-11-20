# Infrastructure / Storage

> **Status:** Implemented — local file-system storage, streamed SHA-256 checksums, and magic-byte
> validation are live for Module 06. Module 11 (Drive) uses `S3DriveStorageService` against the
> S3-compatible SeaweedFS gateway (presigned URLs, range reads); it needs `Drive:Storage`
> endpoint + keys (user-secrets) to run live.

## Purpose

Implements physical binary file storage and cryptographic verification for Module 06
(Attachments & Files), satisfying `Application/Abstractions/Storage/IFileStorageService` and
`IFileSignatureValidator`.

## Contents

| File | Purpose |
| --- | --- |
| `LocalFileStorageService.cs` | Stores uploads on disk under `{Year}/{Month}/{DocumentId}/{Random}.ext` and computes the **SHA-256 checksum** while streaming the bytes (one pass). Also opens stored files for reading, refusing paths that escape the configured root, and exposes `ComputeSha256Async`. |
| `FileSignatureValidator.cs` | Validates an upload against `DocumentFileType.AllowedExtensionsCsv` and `MaxFileSizeBytes`, then checks the **magic bytes** for PDF, PNG, JPEG, TIFF, ZIP-based Office (`docx`/`xlsx`/`pptx`/`zip`), legacy OLE Office (`doc`/`xls`/`ppt`), and SVG; a renamed executable is rejected with `Attachments.SignatureMismatch`. |
| `DriveStorageOptions.cs` | Binds `Drive:Storage` (Module 11): `Endpoint`, `Bucket`, `AccessKey`, `SecretKey`, `Region`, `ForcePathStyle`. `IsConfigured` requires endpoint/bucket/both keys; keys belong in user-secrets, never committed appsettings. |
| `S3DriveStorageService.cs` | Module 11 object storage over the S3-compatible SeaweedFS gateway (AWSSDK.S3). Signs presigned **PUT** (direct browser upload) and **GET** URLs, reads metadata via HEAD, opens inclusive byte ranges for video, computes streamed SHA-256, copies objects server-side (`CopyAsync`, used by version restore), writes a stream to a new key server-side (`WriteAsync`, used by the OnlyOffice save callback), and deletes idempotently. A presigned PUT cannot carry a size condition, so `maxBytes` is enforced at commit via HEAD. Lazy client throws a clear error when unconfigured; keys are validated against leading `/`, `\`, and `..` segments. |
| `DrivePurgeService.cs` | Module 11 permanent deletion (`IDrivePurgeService`), shared by the manual purge handler and `DriveMaintenanceJob`. Gathers the item tree breadth-first, then deepest-first removes edit sessions **through the change tracker** (a session already tracked in the context must be marked `Deleted`, otherwise it lingers as a live dependent and the version `RemoveRange` throws on the severed required FK), deletes each version's S3 object **before** its row, releases committed bytes from the space quota, and removes the item (permissions/stars/deletion requests cascade). Severs `DriveItem.CurrentVersionId` and saves before deleting versions to avoid the Restrict/Cascade FK cycle. One system- or actor-stamped audit row per tree; a failed S3 delete throws so the transaction rolls back and retries. |

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
| `S3DriveStorageService` | ✅ Implemented (Module 11: presigned PUT/GET, HEAD, range reads, SHA-256, server-side copy + write, idempotent delete; needs `Drive:Storage` config to run live) |
| `DrivePurgeService` | ✅ Implemented (Module 11: ordered tree purge, quota release, audit; shared by manual purge + retention job) |

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-11-20 | Qoder | Drive Phase 5: fixed a real `DrivePurgeService` bug surfaced by the full-lifecycle integration test — edit sessions were deleted with `ExecuteDeleteAsync`, which bypasses the change tracker, so a session already tracked from an earlier edit-config/callback lingered as a live dependent and `RemoveRange(versions)` threw on the severed required `DriveFileVersion`→`DriveEditSession` FK. Sessions are now loaded and removed through the tracker (`RemoveRange` + save) before the version purge. Regression test `Purge_WithTrackedEditSession_RemovesSessionAndVersions`. |
| 2026-11-19 | Qoder | Drive Phase 4: added `S3DriveStorageService.WriteAsync` (server-side `PutObject` of a rewound stream) backing the OnlyOffice save callback, which stores the edited bytes as a new version. |
| 2026-11-19 | Qoder | Drive Phase 2: added `DrivePurgeService` (`IDrivePurgeService`) for ordered permanent tree deletion with quota release and audit, and `S3DriveStorageService.CopyAsync` (server-side object copy) backing version restore. The purge severs `CurrentVersionId` before deleting versions to avoid the Restrict/Cascade FK cycle. |
| 2026-11-19 | Qoder | Drive Phase 1 (`plans/module-11-drive-backend.md`): added `DriveStorageOptions` (`Drive:Storage`) and `S3DriveStorageService` (AWSSDK.S3 4.1.2, path-style SeaweedFS) implementing `IDriveStorageService` — presigned upload/download, HEAD metadata, inclusive byte-range reads, streamed SHA-256, idempotent delete, key validation, and an unconfigured guard. Registered as a singleton in DI. |
| 2026-10-25 | opencode | Implemented Module 06 storage: `LocalFileStorageService` (one-pass SHA-256, `{Year}/{Month}/{DocumentId}` layout, root-escape guard) and `FileSignatureValidator` (extension allow-list, size limit, magic bytes). |
