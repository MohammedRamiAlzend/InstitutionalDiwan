# entities / document-decision

> **Status:** Implemented. Reads only. A decision's `appeals` come from `GET /api/documents/{id}/decisions`.

## Purpose

A document's decisions, the paged register, the suggested next action, join suggestions, and the status badge.

## Contents

| File | Purpose |
| --- | --- |
| `model/types.ts` | Decision, register row, appeal, next-action (`move` included) and join-candidate response. |
| `api/documentDecisionApi.ts` | `GET /api/documents/{id}/decisions`, `GET .../next-action`, `GET .../join-candidates`, and `GET /api/decisions`. |
| `model/queries.ts` | `documentDecisionKeys` and the four read hooks. |
| `ui/DecisionStatusBadge.tsx` | Arabic status badge. |
| `index.ts` | Public barrel. |

## Implementation status

| Item | State |
| --- | --- |
| Document list, register, next action, join candidates, badge | ✅ |

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-11-11 | Claude | `useJoinCandidates` reads `GET /api/documents/{id}/join-candidates`. The next-action kind `move` is an ordinary movement. |
| 2026-11-11 | Claude | An appeal response includes who recorded it and its outcome, and when. |
| 2026-11-11 | Claude | Added the document-decision reads and the status badge. |
