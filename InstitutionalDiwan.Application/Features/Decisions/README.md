# Application / Features / Decisions

> **Status:** Implemented for slice B. No appeal commands and no join execution.

## Purpose

Decision-type administration, issuing a decision from the department that holds the document, the suggested next step, attaching the decision's file, and canceling a mistaken entry.

## Contents

| File | Purpose |
| --- | --- |
| `DecisionContracts.cs` | Commands, queries and response records. `Appeals` is always an empty list. |
| `DecisionMapping.cs` | Maps a decision and its type snapshot onto the response. |
| `NextActionPlanner.cs` | Suggests `refer`, `send-for-archive`, `archive`, or nothing. A referral wins over the archive route. Nothing while the file is in transit, archived or canceled. |
| `DecisionValidators.cs` | FluentValidation for the commands. |
| `DecisionTypeHandlers.cs` | Create, update, activate, deactivate and list decision types. Creating a referral without a destination stores it inactive. |
| `DocumentDecisionHandlers.cs` | Issue, list, register, next action, attach file and cancel. Cancel requires system scope. |

## Implementation status

| Item | State |
| --- | --- |
| Type administration | ✅ |
| Issue, file, cancel, register, next action | ✅ |
| Reopen revokes a send-to-archive decision | ✅ In `Features/Archiving` |
| Appeals and join | ❌ |

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-11-11 | Claude | Added the decision use cases. Join issuance is refused and appeal lists stay empty. |
