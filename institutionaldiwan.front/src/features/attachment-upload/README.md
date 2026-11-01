# `src/features/attachment-upload` — Upload / Version / Archive Document Files (Module 06)

> **Status:** ✅ Atomic upload implemented (2026-11-01, Module 06 Phase 3) and version upload,
> archiving, and the checksum-verify dialog implemented (2026-11-01, Module 06 Phase 4). Neither
> phase was browser-verified — the user waived both passes; the validation rules were exercised in
> Node instead.

## Purpose

Writes to a document's file chains: the atomic first upload (`POST /api/documents/{documentId}/files`
creates the file **and** version 1 in one request), the next version of an existing file
(`POST /api/document-files/{fileId}/versions`), and archiving (`POST /api/document-files/{fileId}/archive`).
It also hosts the two read-shaped dialogs over a file's versions and its checksum verification,
because both are actions on the same rows. Everything the server rejects about the bytes (extension,
size) is pre-checked here first, so a doomed file never leaves the browser.

## Layer & Import Rules (FSD Strict Hierarchy)

- Allowed: `@/entities/*`, `@/shared/*`. Never `@/widgets/*`, `@/pages/*`, `@/app/*`, or a sibling
  feature.
- The uploaded file's bytes go through `uploadForm` from `@/shared/api` — never a hand-rolled
  `onUploadProgress`, and never an explicit `Content-Type` (the browser owns the multipart boundary).
- Public surface is `index.ts` only.

## Contents

| File | Purpose |
| --- | --- |
| `api/uploadInitial.ts` | Builds the `FormData` for the atomic upload (`file`, `fileTypeId`, `title`, `isPrimaryScan`, `changeNotes` only when non-empty) and posts it through `uploadForm<DocumentFileResponse>`. |
| `api/uploadVersion.ts` | Builds the `FormData` for a new version (`file` and `changeNotes` only — the type, title, and primary-scan flag belong to the file) and posts it to `/document-files/{fileId}/versions`. |
| `api/archiveFile.ts` | `POST /document-files/{fileId}/archive` through `httpClient`; returns the archived file. |
| `api/attachmentMutations.ts` | `useUploadInitialMutation(documentId)`, `useUploadVersionMutation(fileId)`, `useArchiveFileMutation()`. Each invalidates `documentFileKeys.byDocument(...)` on success — using the `documentId` in the response — so the grid refetches. |
| `model/upload.schema.ts` | `uploadAttachmentSchema` + `UploadAttachmentFormValues` for the first upload (title ≤ 300, type required, then the file pre-checks); `buildUploadVersionSchema(fileTypeId)` + `UploadVersionFormValues` for a new version, where the type is a parameter rather than a field; `fileRejection(file, fileTypeId)` is the single rule table both share (empty file, request cap, type extension list, type size limit). |
| `model/useLocalChecksum.ts` | `useLocalChecksum()` — the digest of the picked file with `isHashing` / `error` state and a token guard, plus `clear()` which also discards a hash still running. Shared by the two upload dialogs and the verify dialog. |
| `ui/UploadAttachmentDialog.tsx` | The first-upload form: live `document-file-types` select, `accept` and the printed limits that follow the chosen type, file picker, «مسح ضوئي أساسي» checkbox, optional change notes, local SHA-256 with a «جارٍ حساب البصمة» indicator, an upload progress bar, and — after success — the `ChecksumBadge` comparing local against stored. |
| `ui/UploadVersionDialog.tsx` | The next-version form: no type or title fields, the file's own limits and type label shown read-only, «الإصدار التالي: N+1» in the description and on the submit button, the same local-digest and progress handling, and an archived file answered in Arabic (`Attachments.FileArchived`) before any request is sent. |
| `ui/FilePickSummary.tsx` | The picked file's name, size, and local digest (or the hashing spinner, or the reason it could not be computed) — one view for both upload dialogs. |
| `ui/DocumentVersionsDialog.tsx` | The file's whole version history, newest first as the list route returns it: version number, الحالي/مُستبدَل badge, original name, size, short checksum, uploader, upload date, change notes, and a download button per version. |
| `ui/ArchiveFileDialog.tsx` | `ConfirmDialog` wording the consequence: archiving stops new versions, leaves reading and downloading intact, and cannot be undone from this UI. Stays open on failure so the reason is readable. |
| `ui/VerifyChecksumDialog.tsx` | Runs `useVerifyChecksum` while open and names the three digests — المسجّلة (at upload) / المحسوبة من القرص (now) / المحلية (from a file picked on this device) — with three labelled comparisons; the server's `isMatch` is the first of them. |
| `index.ts` | Public barrel: the five dialogs, the three mutation hooks, and their types. |

## Behaviour

- Each pre-check message names the backend code it mirrors — `(Attachments.ExtensionNotAllowed)`,
  `(Attachments.FileTooLarge)`, and so on — so a client-side rejection and the server's own rejection
  read identically. `withCode` in the schema is the only place that formats this.
- Size limits come from `DOCUMENT_FILE_TYPE_LIMITS` in `@/shared/config`, a client-side **mirror** of
  the seeded values (the lookup endpoint returns ids and labels only). The server stays authoritative:
  if the mirror drifts, the upload is rejected there with the same Arabic wording.
- An **unknown** file type id yields `null` limits, which means "no pre-check", never "strictest
  limits" — guessing would hard-block an upload the server would accept.
- Only the stored limit is quoted in the too-large message. `formatFileSize` rounds, so
  a file one byte over 20 MB would otherwise read as «20 م.ب يتجاوز 20 م.ب».
- Magic bytes are **not** checked here. The server's signature test (`Attachments.SignatureMismatch`)
  is the only real check, so a renamed `.exe` passes the pre-check and is refused in the request.
- `isPrimaryScan` is always sent, as `true`/`false` text, because the server parameter is
  non-nullable.
- The local digest is computed once per picked file and guarded by a token, so a fast file swap can
  never have an older digest land after a newer one. A file input cannot be assigned a value, so
  clearing a pick means clearing the element itself — otherwise re-picking the same file would not
  fire `change`.
- Comparison after success is `versions.find(isCurrent) ?? versions[0]` against the local digest,
  case-insensitively. When no local digest exists (insecure context, or the file was empty) the
  result shows `ChecksumBadge` in its "not verified" state rather than a false mismatch.
- A failure is **not** toasted separately: the axios interceptor normalizes it to an `ApiError` and
  the dialog renders `toArabicErrorMessage` in a form-level banner, which keeps server-side rejections
  (`FileTypeInactive`, `SignatureMismatch`, access denials) next to the field they concern.
- A new version sends **only** the bytes and the notes. The file type, title, and primary-scan flag are
  properties of the file, and the version number is assigned by the server — the dialog prints
  `currentVersionNumber + 1` as an expectation, never as an input.
- `changeNotes` is omitted when blank on both endpoints: the server parameter is nullable, so an empty
  string would be stored where "no note" was meant.
- An archived file is refused in Arabic **before** a request is sent (`Attachments.FileArchived`). The
  row menu already hides the action for an archived row, so this is the stale-grid guard — the row was
  archived elsewhere after it was rendered.
- Archiving is idempotent on the server (an already-archived file returns success, not a conflict) and
  there is **no** un-archive endpoint, which is why the confirmation says plainly what stays readable
  and what is gone for good.
- The versions dialog is a stack of cards, not a `DataTable`: a modal needs neither URL state nor a
  column chooser, and each version needs its own download button.
- The verify dialog enables its query only while open, so the server re-read of the stored bytes
  happens when the user asks for it, and every reopen refetches. Picking a file is optional and adds
  the third digest; without it the two local comparisons say so instead of showing a false mismatch.
- Downloading one version goes through `downloadFileVersion` in `@/entities/document-file`, so the grid
  and the versions dialog resolve the saved name by the same precedence.

## Implementation status

| Item | State |
| --- | --- |
| Atomic upload transport + mutation | ✅ Implemented (Phase 3) |
| Pre-upload zod validation with the limit mirror | ✅ Implemented (Phase 3) |
| Upload dialog with progress + checksum compare | ✅ Implemented (Phase 3) |
| `uploadVersion` / `archiveFile` transports + mutations | ✅ Implemented (Phase 4) |
| `UploadVersionDialog` with next-version number + archived refusal | ✅ Implemented (Phase 4) |
| `DocumentVersionsDialog` with per-version download | ✅ Implemented (Phase 4) |
| `ArchiveFileDialog` confirmation | ✅ Implemented (Phase 4) |
| `VerifyChecksumDialog` with three named digests | ✅ Implemented (Phase 4) |
| Browser verification | ❌ Waived by the user for Phases 3–4. The validation rules were exercised in Node instead (14 cases in Phase 3, 21 in Phase 4 including the version schema and `currentVersionOf`). Untouched by that harness: the dialogs' own rendering, progress bars, toasts, the live verify read, and the archived-row gating. |

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-11-01 | agent | Module 06 Phase 4: added `uploadVersion` and `archiveFile` with their mutations, the shared `fileRejection` rule table and `buildUploadVersionSchema(fileTypeId)`, the `useLocalChecksum` hook and `FilePickSummary` extracted from the first-upload dialog, and the four dialogs — version upload (next-version number, archived refusal), versions list with per-version download, archive confirmation, and checksum verify with three named digests. The plan's `ArchiveFileButton` became `ArchiveFileDialog` because these actions live in the row context menu, not in a button. |
| 2026-11-01 | agent | Module 06 Phase 3: implemented the slice — `uploadInitial`, `useUploadInitialMutation`, `uploadAttachmentSchema` (title/type/file rules plus the extension and size pre-checks against the limit mirror, each naming its `Attachments.*` code), and `UploadAttachmentDialog` with the type-driven `accept` and limits text, local SHA-256, upload progress, and the post-upload checksum comparison. |
| 2026-10-25 | Cursor | Added attachment-upload feature docs for live Module 06 endpoints. |
