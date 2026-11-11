# pages / decisions

> **Status:** Implemented. Exercised in the browser: the register showed the canceled decision, and the types tab showed the five seeded types.

## Purpose

`/decisions` lists every decision the caller may view, and, for `LOOKUP_MANAGE`, the decision types. The tab lives in `?tab`.

## Contents

| File | Purpose |
| --- | --- |
| `ui/DecisionsPage.tsx` | The page, its guide and the two tabs. Switching tabs clears the other search params. |
| `ui/DecisionRegisterTable.tsx` | Server-paged grid (`tableId` `decisions`) with type, status, department and year filters. |
| `ui/DecisionTypesTable.tsx` | Client grid (`tableId` `decision-types`) with activate, deactivate and the form dialog. |
| `index.ts` | Public barrel. |

## Implementation status

| Item | State |
| --- | --- |
| Register and types tabs | ✅ |

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-11-11 | Claude | Added the decisions page. |
