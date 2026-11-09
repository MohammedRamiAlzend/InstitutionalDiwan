# Application / Features / Persons

> **Status:** Implemented. Plan: [`plans/document-persons.md`](../../../plans/document-persons.md).

## Purpose

People named on documents who are not system users (defendants, plaintiffs, ...). One `Person` row
stands for one human across documents, so their documents can be gathered from their name.

## Contents

| File | Purpose |
| --- | --- |
| `DocumentPersonLinker.cs` | `DocumentPersonInput`, and the one place that resolves roles (including the catalog default), creates or fetches people, and links them to a document. |
| `PersonValidationRules.cs` | `DocumentPersonInputValidator`, shared by every flow that accepts people. |
| `PersonResponses.cs` | Wire shapes: the full record, the short suggestion, the list row, and the person-on-a-document row. |
| `PersonQueries.cs` | `PersonAccess` plus the suggestion, list, by-id, documents, timeline and per-document queries. |
| `PersonCommands.cs` | Updating a person, and adding, re-roling and removing a person on a document. |

## Rules

- Everything reached from a person is limited to documents the caller may view; a person with none reads as not found.
- Suggestions are the one exception, so nobody is registered twice from another department, and they expose only the short row.
- A typed name always creates a new person; only an explicit `personId` reuses one.

## Implementation status

| Area | Status |
| --- | --- |
| Commands and queries | ✅ Implemented |

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-11-09 | Claude | Created the slice. |
