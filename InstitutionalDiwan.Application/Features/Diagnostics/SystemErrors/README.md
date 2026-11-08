# `Features/Diagnostics/SystemErrors`

> **Status:** Implemented and verified against a live API on 2026-11-08.

## Purpose

Queries and commands of the developer's error log, and how a failure becomes a grouped occurrence.

## Contents

| File | Purpose |
| --- | --- |
| `SystemErrorFeature.cs` | The queue and Excel abstractions, responses, queries, commands, validators and handlers. |
| `SystemErrorOccurrenceFactory.cs` | Builds an occurrence with bounded text and a fingerprint that survives ids, numbers and rebuilds. |

## Notes

- Request bodies, headers and query strings are never stored.
- The repeat of a resolved error reopens it as new; nothing is deleted without a manual purge.

## Implementation status

| Item | State |
| --- | --- |
| Everything listed above | ✅ Implemented |

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-11-08 | Claude | Added for the developer's error log. |
