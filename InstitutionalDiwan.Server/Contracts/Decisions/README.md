# Server / Contracts / Decisions

> **Status:** Implemented.

## Purpose

HTTP request bodies for the decision endpoints. Responses live in `Application/Features/Decisions`.

## Contents

| File | Purpose |
| --- | --- |
| `DecisionContracts.cs` | Create and update a decision type, issue a decision (optional `joinTargetDocumentId`), attach its file, cancel it with a reason, record an appeal, and record the appeal outcome. |

## Implementation status

| Item | State |
| --- | --- |
| Request records | ✅ |

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-11-11 | Claude | `IssueDocumentDecisionRequest` accepts an optional `joinTargetDocumentId`. |
| 2026-11-11 | Claude | Added `RecordDecisionAppealRequest` and `RecordAppealOutcomeRequest`. |
| 2026-11-11 | Claude | Added the decision request bodies. |
