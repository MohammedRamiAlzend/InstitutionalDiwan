# Application / Features / Decisions

> **Status:** Implemented for slices B, C and D. Recording an appeal and its outcome does not require the document to be held or an open register visit, and it is allowed on an archived document. Issuing a join decision creates one `JoinedInto` link in the same save.

## Purpose

Decision-type administration, issuing a decision from the department that holds the document, the suggested next step, attaching the decision's file, and canceling a mistaken entry.

## Contents

| File | Purpose |
| --- | --- |
| `DecisionContracts.cs` | Commands, queries and response records. Issue accepts an optional `JoinTargetDocumentId`. A decision response includes its appeals, newest appeal date first. |
| `DecisionMapping.cs` | Maps a decision and its type snapshot onto the response. |
| `DocumentJoin.cs` | Creates and removes the `JoinedInto` link through the relation code, without its own save. Every invalid principal file is `Decisions.JoinTargetInvalid`. |
| `NextActionPlanner.cs` | Suggests `refer`, `move`, `send-for-archive`, `archive`, or nothing. A referral wins over the archive route. A join suggests an ordinary referral only when the principal file is held by another department. Nothing while the file is in transit, archived or canceled. |
| `DecisionValidators.cs` | FluentValidation for the commands. |
| `DecisionTypeHandlers.cs` | Create, update, activate, deactivate and list decision types. Creating a referral without a destination stores it inactive. |
| `DocumentDecisionHandlers.cs` | Issue (including join), list (the principal file's decisions when the caller may view it), join candidates, register, next action, attach file, cancel (a join cancel removes the link), record an appeal and record its outcome. Cancel requires system scope. An appeal or its outcome is allowed when the caller's scope covers the issuing department or the appellant department. |

## Implementation status

| Item | State |
| --- | --- |
| Type administration | ✅ |
| Issue, file, cancel, register, next action | ✅ |
| Reopen revokes a send-to-archive decision | ✅ In `Features/Archiving` |
| Record an appeal and its outcome | ✅ No notification and no movement. |
| Join execution | ✅ One `JoinedInto` link on the joined file. It is not the principal file's in-force decision. Cancel removes the link in the same save. |

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-11-11 | Claude | Slice D: issuing a join decision links the file to its principal file. Listing decisions on the joined file includes the principal file when the caller can view it. `GET` join candidates is a suggestion. Canceling the join removes the link. |
| 2026-11-11 | Claude | `RecordDecisionAppealCommand` and `RecordAppealOutcomeCommand`. The document decision list now returns appeals. Either the issuing department or the appellant department is enough, including on an archived document. |
| 2026-11-11 | Claude | Added the decision use cases. Join issuance is refused and appeal lists stay empty. |
