# entities / document-decision

> **Status:** Implemented. Reads only. A decision's `appeals` come from `GET /api/documents/{id}/decisions`.

## Purpose

A document's decisions, the paged register, the suggested next action, and the status badge.

## Contents

| File | Purpose |
| --- | --- |
| `model/types.ts` | Decision, register row, appeal and next-action response. |
| `api/documentDecisionApi.ts` | `GET /api/documents/{id}/decisions`, `GET .../next-action`, and `GET /api/decisions`. |
| `model/queries.ts` | `documentDecisionKeys` and the three read hooks. |
| `ui/DecisionStatusBadge.tsx` | Arabic status badge. |
| `index.ts` | Public barrel. |

## Implementation status

| Item | State |
| --- | --- |
| Document list, register, next action, badge | ✅ |

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-11-11 | Claude | An appeal response includes who recorded it and its outcome, and when. |
| 2026-11-11 | Claude | Added the document-decision reads and the status badge. |
