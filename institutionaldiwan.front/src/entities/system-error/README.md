# `src/entities/system-error`

> **Status:** Implemented and verified against a live API on 2026-11-08.

## Purpose

Read side of the developer's error log.

## Contents

| File | Purpose |
| --- | --- |
| `api/systemErrorApi.ts` | `list`, `details` and the Excel `exportFile`. |
| `model/types.ts` | Response types, integer enums and the shared filter shape. |
| `model/labels.ts` | Arabic labels for status, source and kind. |
| `model/queries.ts` | `systemErrorKeys`, `useSystemErrors` and `useSystemError`. |
| `ui/SystemErrorStatusBadge.tsx` | The status badge. |
| `index.ts` | Public barrel. |

## Implementation status

| Item | State |
| --- | --- |
| Everything listed above | ✅ Implemented |

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-11-08 | Claude | Added for the developer's error log. |
