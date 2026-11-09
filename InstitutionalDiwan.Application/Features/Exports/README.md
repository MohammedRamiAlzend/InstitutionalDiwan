# `Application/Features/Exports`

> **Status:** Implemented; workbook and validator covered by `TableExcelWriterTests`, endpoint exercised against a live API on 2026-11-09.

## Purpose

Turns a table the caller already has on screen into an Excel workbook. Nothing is read from the database, so the file can only hold what the caller sent, and any signed-in user may use it.

## Contents

| File | Purpose |
| --- | --- |
| `TableExport.cs` | `ITableExcelWriter`, `ExportTableQuery` with its limits (100 columns, 5000 rows, 200-character title), the validator and the handler. |
| `README.md` | This file. |

## Implementation status

| Item | State |
| --- | --- |
| Excel export of a displayed table | ✅ Implemented |

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-11-09 | Claude | Added the table export query, validator and writer contract. |
