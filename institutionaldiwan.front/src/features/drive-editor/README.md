# `src/features/drive-editor` — OnlyOffice Drive Editor

> **Status:** Implemented (front-end Phase 6) — lazy DocsAPI loading, signed `edit-config`
> mounting, edit/view configuration passthrough, cleanup, and Arabic error states.

## Contents

| File | Purpose |
| --- | --- |
| `api/onlyOfficeLoader.ts` | Loads `/web-apps/apps/api/documents/api.js` once per OnlyOffice server URL and validates `window.DocsAPI`. |
| `ui/DriveEditor.tsx` | Reads the signed edit config, mounts `DocEditor`, wires close/error events, and destroys the editor on close/unmount. |
| `index.ts` | Public barrel. |

## Implementation status

| Item | State |
| --- | --- |
| Lazy DocsAPI + signed config mounting | ✅ Phase 6 |
| Edit/view mode and Arabic error states | ✅ Phase 6 |
| Live OnlyOffice save verification | ⏳ Requires deployment configuration and SeaweedFS |

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-11-21 | Copilot | Added the lazy OnlyOffice editor feature for Drive. |
