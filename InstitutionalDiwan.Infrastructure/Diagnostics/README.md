# `Infrastructure/Diagnostics`

> **Status:** Implemented and verified against a live API on 2026-11-08.

## Purpose

Storage side of the developer's error log.

## Contents

| File | Purpose |
| --- | --- |
| `SystemErrorCapture.cs` | The bounded queue, the background writer that merges occurrences by fingerprint, the repository and the ClosedXML workbook writer. |

## Notes

- Occurrences are written on their own database context, so a failed request cannot lose or block them.
- A full queue drops new occurrences; the application log still has them.

## Implementation status

| Item | State |
| --- | --- |
| Everything listed above | ✅ Implemented |

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-11-08 | Claude | Added for the developer's error log. |
