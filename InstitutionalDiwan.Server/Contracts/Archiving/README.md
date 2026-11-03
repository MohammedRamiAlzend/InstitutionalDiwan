# Presentation / Contracts / Archiving — archive and saved search requests

> **Status:** Implemented.

## Purpose

HTTP request records for Module 07, consumed by `DocumentArchiveController` and
`SavedSearchesController`. The read models are the Application responses in `Features/Archiving`.

## Contents

| File | Purpose |
| --- | --- |
| `ArchivingRequests.cs` | `ArchiveDocumentRequest` (required location, optional reason), `UnarchiveDocumentRequest` (required reason), and `SaveSearchRequest` (name, criteria JSON string, default flag), the last shared by create and replace. |

## Implementation status

| Item | State |
| --- | --- |
| Archive and unarchive requests | ✅ |
| Saved search request | ✅ |

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-11-03 | Claude | Added the Module 07 requests (Phase 3). |
