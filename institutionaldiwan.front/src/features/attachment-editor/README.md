# `src/features/attachment-editor` — Document Attachment Office Editor

> **Status:** ✅ Implemented — lazy OnlyOffice mounting for editable Word/Excel attachments.

## Purpose

This feature opens a signed OnlyOffice editor for a document attachment. The backend owns access,
the source version, conflict checks, and immutable version creation; this slice only mounts and
cleans up the browser editor and presents its Arabic-first dialog.

## Contents

| File | Purpose |
| --- | --- |
| `ui/AttachmentEditor.tsx` | Loads the signed attachment config, mounts DocsAPI, reports editor errors, and destroys the editor on close. |
| `ui/AttachmentEditorDialog.tsx` | Resizable editor dialog with close and fallback download actions. |
| `index.ts` | Public barrel exporting the editor dialog. |

## Implementation status

| Item | State |
| --- | --- |
| Signed editor config and lazy DocsAPI mounting | ✅ |
| Automatic save as a new attachment version | ✅ (backend callback) |

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-11-23 | Copilot | Added OnlyOffice editing for supported document attachments. |
