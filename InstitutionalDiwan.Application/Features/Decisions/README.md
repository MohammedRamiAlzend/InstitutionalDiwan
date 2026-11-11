# Application / Features / Decisions

> **Status:** Implemented for slices B and C. Recording an appeal and its outcome does not require the document to be held or an open register visit, and it is allowed on an archived document. Join execution is still refused.

## Purpose

Decision-type administration, issuing a decision from the department that holds the document, the suggested next step, attaching the decision's file, and canceling a mistaken entry.

## Contents

| File | Purpose |
| --- | --- |
| `DecisionContracts.cs` | Commands, queries and response records. A decision response includes its appeals, newest appeal date first. |
| `DecisionMapping.cs` | Maps a decision and its type snapshot onto the response. |
| `NextActionPlanner.cs` | Suggests `refer`, `send-for-archive`, `archive`, or nothing. A referral wins over the archive route. Nothing while the file is in transit, archived or canceled. |
| `DecisionValidators.cs` | FluentValidation for the commands. |
| `DecisionTypeHandlers.cs` | Create, update, activate, deactivate and list decision types. Creating a referral without a destination stores it inactive. |
| `DocumentDecisionHandlers.cs` | Issue, list, register, next action, attach file, cancel, record an appeal and record its outcome. Cancel requires system scope. An appeal or its outcome is allowed when the caller's scope covers the issuing department or the appellant department. |

## Implementation status

| Item | State |
| --- | --- |
| Type administration | ✅ |
| Issue, file, cancel, register, next action | ✅ |
| Reopen revokes a send-to-archive decision | ✅ In `Features/Archiving` |
| Record an appeal and its outcome | ✅ No notification and no movement. |
| Join execution | ❌ |

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-11-11 | Claude | `RecordDecisionAppealCommand` and `RecordAppealOutcomeCommand`. The document decision list now returns appeals. Either the issuing department or the appellant department is enough, including on an archived document. |
| 2026-11-11 | Claude | Added the decision use cases. Join issuance is refused and appeal lists stay empty. |
