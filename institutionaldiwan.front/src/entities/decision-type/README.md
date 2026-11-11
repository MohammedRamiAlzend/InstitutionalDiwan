# entities / decision-type

> **Status:** Implemented. Reads only.

## Purpose

Loads decision types for the type grid, the issue dialog and the document tab.

## Contents

| File | Purpose |
| --- | --- |
| `model/types.ts` | The decision-type response. |
| `api/decisionTypeApi.ts` | `GET /api/decision-types`. |
| `model/queries.ts` | `decisionTypeKeys` and `useDecisionTypes`. |
| `index.ts` | Public barrel. |

## Implementation status

| Item | State |
| --- | --- |
| List query | ✅ |

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-11-11 | Claude | Added the decision-type read model. |
