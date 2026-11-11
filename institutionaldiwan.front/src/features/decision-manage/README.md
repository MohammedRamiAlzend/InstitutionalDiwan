# features / decision-manage

> **Status:** Implemented. Issuing «منع المحاكمة» and canceling it, then recording an appeal on «الظن» and accepting it, were exercised in the browser. A unification decision asks for the principal file.

## Purpose

Issues a decision, attaches its copy, cancels a mistaken entry, records an appeal, and records the appeal's outcome. Cancel is offered when the signed-in token carries system scope; the server remains the authority. An appeal does not require the document to be held.

## Contents

| File | Purpose |
| --- | --- |
| `model/schemas.ts` | Zod schemas for issue (including the principal file), attach, cancel, appeal and outcome. |
| `api/decisionMutations.ts` | The five mutations. Issue and cancel also refresh document relations. |
| `ui/IssueDecisionDialog.tsx` | Fields follow the effect. A referral shows the destination name; send-to-archive shows a hint; unification asks for the principal file. |
| `ui/JoinTargetPicker.tsx` | Searches documents the caller may view and excludes this file, archived files and canceled files. |
| `ui/AttachDecisionFileDialog.tsx` | Offers unarchived files of type «قرار» on the same document. |
| `ui/CancelDecisionDialog.tsx` | Requires a reason. |
| `ui/RecordAppealDialog.tsx` | Appeal date, appellant department (the owning department by default), optional reference and notes. |
| `ui/RecordAppealOutcomeDialog.tsx` | Accepted or rejected, and the outcome date. |
| `index.ts` | Public barrel. |

## Implementation status

| Item | State |
| --- | --- |
| Issue, attach, cancel, unification | ✅ The principal file is required only when the effect is join. |
| Appeal and outcome dialogs | ✅ On the document decision tab. |

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-11-11 | Claude | Unification is offered. The dialog names the principal file and says the number and place do not change. Issue and cancel refresh the relation queries. |
| 2026-11-11 | Claude | Added the appeal dialog and the outcome dialog. Both invalidate `documentDecisionKeys`. |
| 2026-11-11 | Claude | Added issuing, attaching and canceling a decision. |
