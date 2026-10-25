# `src/widgets/document-attachments-panel` — Document Files Panel

> **Status:** Docs only — Module 06 API live; code pending.

## Purpose

Composite panel on document detail: lists files/versions, mounts upload/archive actions, download
and verify checksum. Used by `pages/documents` detail tab `files`.

The file list is a `shared/ui/data-table` grid (table id `document-files`) with URL filters, a
`localStorage` column chooser, and a permission-filtered row context menu (download, verify checksum,
upload new version, archive, copy file id); the same actions stay available in the toolbar.

## Composes

- `entities/document-file` (list, verify, checksum badge)
- `features/attachment-upload` (upload dialog, archive)

## Implementation status

| Item | State |
| --- | --- |
| Docs | ✅ |
| Code | ❌ |

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-10-25 | Buffy | Documented the files grid contract (table id `document-files`, column chooser, row context menus). |
| 2026-10-25 | Cursor | Added attachments panel widget docs for Module 06. |
