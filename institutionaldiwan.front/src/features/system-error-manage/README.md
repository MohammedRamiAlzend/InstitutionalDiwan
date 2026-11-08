# `src/features/system-error-manage`

> **Status:** Implemented and verified against a live API on 2026-11-08.

## Purpose

Write side of the developer's error log: work status and notes, and the manual purge.

## Contents

| File | Purpose |
| --- | --- |
| `api/systemErrorMutations.ts` | `useUpdateSystemErrorMutation` and `usePurgeSystemErrorsMutation`. |
| `model/schemas.ts` | Zod schemas of both forms. |
| `ui/UpdateSystemErrorForm.tsx` | Status and notes of one error. |
| `ui/PurgeSystemErrorsDialog.tsx` | Picks the age (30, 60, 90, 180 or typed days) and confirms the permanent deletion. |
| `index.ts` | Public barrel. |

## Implementation status

| Item | State |
| --- | --- |
| Everything listed above | ✅ Implemented |

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-11-08 | Claude | Added for the developer's error log. |
