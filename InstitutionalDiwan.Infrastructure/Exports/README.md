# `Infrastructure/Exports`

> **Status:** Implemented; covered by `TableExcelWriterTests`.

## Purpose

Writes the Excel files behind `Application/Features/Exports`.

## Contents

| File | Purpose |
| --- | --- |
| `TableExcelWriter.cs` | ClosedXML workbook: right-to-left sheet named after the table, bold frozen header, filter, capped column widths, every value stored as inert text. |
| `README.md` | This file. |

## Implementation status

| Item | State |
| --- | --- |
| `TableExcelWriter` | ✅ Implemented |

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-11-09 | Claude | Added `TableExcelWriter`. |
