# Infrastructure / Storage

> **Status:** Target design — **no code yet**.

## Purpose

Implements physical binary file storage and cryptographic verification for Module 06
(Attachments & Files), satisfying `IFileStorageService`.

## Planned components

### `LocalFileStorageService.cs` (`: IFileStorageService`)
Stores uploaded document scans and attachments on disk under a partitioned path
`{Year}/{Month}/{DocumentId}/{VersionId}.ext`, resolved from
`FILE_STORAGE_PROVIDERS.BasePathOrEndpoint`. It computes the **SHA-256 checksum** (64 hex characters)
while streaming the upload and passes it to `DocumentFile.AddVersion(...)`.

### `S3CompatibleStorageService.cs` (optional)
Implements the same contract for MinIO / S3 object storage when `ProviderType == "S3"`.

### `FileSignatureValidator.cs`
Validates file **magic bytes** (PDF, TIFF, PNG, JPEG) against
`DocumentFileType.AllowedExtensionsCsv` and enforces `MaxFileSizeBytes` before anything is persisted.

## Domain contract it satisfies

`DocumentFile.AddVersion(storageProviderCode, relativePath, originalFileName, mimeType, fileSizeBytes,
checksumSha256, uploadedByUserId, changeNotes)` already validates the checksum format (exactly 64 hex
chars), positivity of the size, and version sequencing. The storage service must supply a matching
checksum or the domain will reject the version.

## Selection of provider

`FILE_STORAGE_PROVIDERS` holds `Code`, `ProviderType` (`LOCAL` / `S3` / `MINIO`),
`BasePathOrEndpoint`, `IsDefault`, and `IsActive`. Pick the row with `IsDefault = true` (or resolve by
code) when a file is stored.

## Implementation status

| Component | State |
| --- | --- |
| `LocalFileStorageService` | ❌ Not implemented |
| `S3CompatibleStorageService` | ❌ Not implemented |
| `FileSignatureValidator` | ❌ Not implemented |

The `FILE_STORAGE_PROVIDERS` table and `DocumentFile` invariants are configured, but no storage code
exists and no path/base configuration is present in `appsettings.json` yet.
