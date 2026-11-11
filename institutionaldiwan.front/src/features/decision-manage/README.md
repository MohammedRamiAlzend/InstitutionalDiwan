# features / decision-manage

> **Status:** Implemented. Exercised in the browser: issuing «منع المحاكمة» and canceling it. No appeal dialogs. Join types are not offered.

## Purpose

Issues a decision, attaches its copy, and cancels a mistaken entry. Cancel is offered when the signed-in token carries system scope; the server remains the authority.

## Contents

| File | Purpose |
| --- | --- |
| `model/schemas.ts` | Zod schemas for issue, attach and cancel. |
| `api/decisionMutations.ts` | The three mutations, invalidating `documentDecisionKeys.all` and the document detail. |
| `ui/IssueDecisionDialog.tsx` | Fields follow the effect. A referral shows the destination name; send-to-archive shows a hint. |
| `ui/AttachDecisionFileDialog.tsx` | Offers unarchived files of type «قرار» on the same document. |
| `ui/CancelDecisionDialog.tsx` | Requires a reason. |
| `index.ts` | Public barrel. |

## Implementation status

| Item | State |
| --- | --- |
| Issue, attach, cancel | ✅ |
| Appeal dialogs | ❌ Later slice |

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-11-11 | Claude | Added issuing, attaching and canceling a decision. |
