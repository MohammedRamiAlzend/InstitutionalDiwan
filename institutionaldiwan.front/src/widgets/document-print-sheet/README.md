# `src/widgets/document-print-sheet` — Document PDF Template

> **Status:** Implemented — content and layout checked in the browser against a live API on 2026-11-09.

## Purpose

Lays out a whole document on one printable A4 template and hands it to `PrintSheet`, which opens the browser's "Save as PDF" dialog. A section the caller may not read, or one with nothing in it, is left out.

## Contents

| File | Purpose |
| --- | --- |
| `ui/DocumentPrintSheet.tsx` | Loads every part of the document and renders its sections: data, department registers, fields, persons, movements with steps, attachments, relations, status history, timeline and archive log. |
| `ui/printParts.tsx` | The template's building blocks: section heading, label/value grid, bordered table and date values. |
| `index.ts` | Public barrel. |
| `README.md` | This file. |

## Implementation status

| Item | State |
| --- | --- |
| Template and all sections | ✅ Implemented |

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-11-18 | Qoder | UI labels Phase 4 (ب5): the whole template — section titles, fact labels, table headers, the archived/yes-no markers and the hidden-relations notice — resolves from `print.*` via `t()`. Lookup names, official numbers, field labels and reasons stay data. |
| 2026-11-18 | Qoder | UI-labels edit mode (relations unit): the relation direction cell now resolves `RELATION_DIRECTION_LABELS` (a `LabelKey` map) through `t()`. The rest of the template's text is migrated in the ب5 print batch. |
| 2026-11-09 | Claude | Added the document PDF template. |
