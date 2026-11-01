# institutionaldiwan.front / src/widgets/document-attachments-panel — Document Files Panel

> **Status:** ✅ Grid, filters, download, upload, versions, archiving, and verify are implemented
> (2026-11-01, Module 06 Phases 2–4) and verified in-browser in three roles (2026-11-01, Module 06
> Phase 5), which had waived the per-phase passes.

## Purpose

The document's files on the document-details «الملفات» tab: every file with its current version,
size, original name, checksum, and archive state. Wraps `GET /api/documents/{documentId}/files` in
the shared `DataTable` (client mode) so the grid behaves like every other grid — URL state, column
chooser, context menus, and loading/empty/error states — streams a version's bytes back to disk
under the name the server recorded, and opens the four row actions: a new file, a new version of one,
a file's version history, archiving, and checksum verification.

## Layer & Import Rules (FSD Strict Hierarchy)

- Allowed: `@/entities/*`, `@/shared/*` (public barrels only), and `@/features/attachment-upload`
  for the upload, version, archive, and verify dialogs.
- Forbidden: `@/app/*`, `@/pages/*`, sibling widgets.
- Export only the root widget via `index.ts`.

## Contents

| File | Purpose |
| --- | --- |
| `ui/DocumentAttachmentsPanel.tsx` | Client-mode `DataTable` (`tableId="document-files"`) over `useDocumentFiles`; status and file-type filters through `useTableState({ filterKeys: ['status', 'fileType'] })`; ten columns (العنوان، النوع، الإصدار، الحجم، الاسم الأصلي، مسح ضوئي أساسي، الحالة، plus البصمة / تاريخ الإنشاء / آخر تحديث hidden by default); row menu (download the current version, الإصدارات, التحقق من البصمة, and — for a manager on a live, unarchived file — رفع إصدار وأرشفة; then copy file id / checksum / original name); refresh header and empty menus; `رفع ملف` in the toolbar and as the empty-state action when `canManage`. One `rowAction` state slot holds the open dialog as `{ type, fileId }`, and the row is looked up again from the live query so a mutation updates what the dialog shows. |
| `index.ts` | Public barrel exporting `DocumentAttachmentsPanel`. |

## Behaviour

- The list route is unpaged and unfiltered, so **archived files are listed too** and the status
  (`الكل` / `نشط` / `مؤرشف`) and file-type filters run in the widget while search, sort, and paging
  stay in the URL. Archiving stops new versions without hiding anything, so an archived file is a
  state to show, not a row to drop.
- Rows arrive oldest first from the API and no default sort is imposed, so that order is what an
  unfiltered grid shows.
- Every column and action that reads the current version resolves it with `currentVersionOf` from
  `@/entities/document-file` (`versions.find(isCurrent) ?? versions[0]`), because the contract promises
  a current flag and a newest-first array — neither of which alone is worth trusting. The same helper
  serves the dialogs, so it is no longer a private detail of this widget.
- Download offers itself to every viewer: the endpoint needs the per-document DOWNLOAD access level,
  which the client is never told, so a denial arrives as a `403` and is shown as its Arabic message
  rather than predicted by hiding the action. The bytes, the name precedence, and the save are all in
  `downloadFileVersion`, which the versions dialog calls with a specific version.
- The saved file name comes from `Content-Disposition` first (only its RFC 5987 form carries an
  Arabic name undamaged), then the version's `originalFileName`, then the file title.
- Search covers the title, the original file name, and the Arabic file-type label; the file-type
  options come from the live `document-file-types` catalog with the seeded mirror as placeholder.
- The «مسح البحث والمرشحات» button calls `clearQuery()` so the search and both filters go in one URL
  write.
- Write actions are **hidden, not disabled** — the caller passes `canManage`, which is
  `DOCUMENT_EDIT` on a document that is not canceled. Permission rules in this app hide write actions
  instead of offering a disabled button. Reading actions (versions, verify, download) stay for everyone.
- An archived row hides «رفع إصدار» and «أرشفة» as well: the first the server refuses
  (`Attachments.FileArchived`), the second is a no-op it happily repeats. Both stay writes behind
  `canManage`, so a viewer never sees either.
- Only one row dialog can be open at a time, keyed by `fileId` rather than by the row object, so the
  archive and version mutations — which invalidate the document's file list — update the dialog's own
  view of the file instead of leaving it on stale data.
- Every write invalidates `documentFileKeys.byDocument(documentId)`, so the grid refetches on its own
  and the widget holds no mutation state of its own.
- The empty-state upload button only appears when the document really has no files
  (`files.length === 0`); a filtered-to-zero grid would otherwise offer a write under a
  "no match" message.

## Implementation status

| Item | State |
| --- | --- |
| Grid, filters, row menu, download | ✅ Implemented (Module 06 Phase 2) |
| Upload action + dialog mount | ✅ Implemented (Module 06 Phase 3) |
| Versions, upload-version, archive, verify actions | ✅ Implemented (Module 06 Phase 4) |
| Browser verification | ✅ Verified in-browser (Module 06 Phase 5) in three roles: manager (populated grid, Arabic download names, URL-backed filters, search/sort, column chooser persisted, both empty states, all five dialogs), `USER` with `DOCUMENT_EDIT` (write UI visible but refused per access level with the Arabic 403), and read-only (no write buttons, download + verify only) |
| Known issues | The no-match empty area offers «رفع ملف» because `DataTable` builds one empty view for both "no files" and "no match" — the only item left from the Phase 5 list: the two upload-dialog issues (stale server error on reopen, premature «اختر ملفًا للرفع») were fixed in `@/features/attachment-upload` the same day |

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-11-01 | agent | Cleared two of the three known issues from `UploadAttachmentDialog` (fix in `@/features/attachment-upload`, no widget code): a server rejection no longer stays visible after close-and-reopen, and changing the file type no longer flags an untouched file field. Both re-verified in the browser against the scratch document — a real 400 `Attachments.SignatureMismatch` closed and reopened clean, and a picked `.pdf` still rejected against «توقيع». Header status updated to match the Phase 5 verification. |
| 2026-11-01 | agent | Module 06 Phase 5: browser verification flipped to ✅ after the three-role pass on the permanent fixture document (`d6b111be-…`): grid, filters, search/sort, column chooser, both empty states, download of current and replaced versions with Arabic filenames, upload, new version, versions dialog, archive, and checksum verify all exercised; recorded the two open UX issues (stale dialog error, premature empty-file message) and the clipboard-denial harness limit. No code change. |
| 2026-11-01 | agent | Module 06 Phase 4: renamed `canUpload` to `canManage` (three write actions now depend on it), completed the row menu with الإصدارات / التحقق من البصمة for every viewer and رفع إصدار / أرشفة for a manager on a live unarchived file, added one `rowAction` state that resolves the file from the live query, moved download-by-version to `downloadFileVersion` and current-version resolution to `currentVersionOf` in the entity so the dialogs share them. |
| 2026-11-01 | agent | Module 06 Phase 3: added the `canUpload` prop, the «رفع ملف» toolbar button and empty-state action, and the `UploadAttachmentDialog` mount from `@/features/attachment-upload`; the grid already refetches because the upload mutation invalidates its query key. |
| 2026-11-01 | agent | Module 06 Phase 2: implemented the panel — client-mode `DataTable` over `useDocumentFiles`, URL-backed status/file-type filters, ten columns with the checksum and timestamps hidden by default, a row menu that downloads the current version and copies the id / checksum / original name, and Arabic empty states that distinguish "no files" from "no match". |
| 2026-10-25 | Buffy | Documented the files grid contract (table id `document-files`, column chooser, row context menus). |
| 2026-10-25 | Cursor | Added attachments panel widget docs for Module 06. |
