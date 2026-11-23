# `src/features/drive-editor` - Drive Office Editor (Module 11)

> **Status:** Implemented (front-end Phase 6) - lazy DocsAPI loading, signed edit/view config,
> host-fill editor sizing, cleanup, and Arabic error states.

## Purpose

Loads the OnlyOffice browser API and creates a document editor/preview surface for Office files.

## Layer & Import Rules (FSD)

Allowed: `@/entities/drive-item`, `@/shared/*`. Public exports are limited to the feature barrel.
Forbidden: `@/widgets/*`, `@/pages/*`, `@/app/*`, sibling features.

## Contents

| File | Purpose |
| --- | --- |
| `api/onlyOfficeLoader.ts` | Loads `/web-apps/apps/api/documents/api.js` once per OnlyOffice server URL and validates `window.DocsAPI`. |
| `ui/DriveEditor.tsx` | Reads the signed edit config, mounts `DocEditor`, wires close/error events, fills the preview host, and destroys the editor on close/unmount. |
| `index.ts` | Public barrel exporting `DriveEditor`. |

## Implementation status

| Item | State |
| --- | --- |
| Lazy DocsAPI + signed config mounting | Implemented |
| Edit/view mode and Arabic error states | Implemented |
| Resizable preview host sizing | Implemented |
| Live OnlyOffice save verification | Pending deployment configuration and SeaweedFS |

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-11-23 | Copilot | Allowed editor loading and error surfaces to fill the enlarged resizable preview host. |
| 2026-11-21 | Copilot | Added the lazy OnlyOffice editor feature for Drive. |
