# Domain / Entities / Decisions

> **Status:** Implemented for slice B of `plans/case-decisions.md`. Appeals and joining two files are not implemented; their error codes exist so later slices keep the planned names.

## Purpose

A decision type is an administrator-managed catalog entry. A document decision is one ruling on one document, and it keeps a snapshot of the type so a later edit of the type does not rewrite history.

## Contents

| File | Purpose |
| --- | --- |
| `DecisionLookups.cs` | `DecisionEffect` (1 refer, 2 send to archive, 3 join, 4 none) and `DecisionStatus` (1 issued, 2 appealed, 3 final, 4 revoked, 5 canceled). In force means status 1, 2 or 3. |
| `DecisionType.cs` | Arabic name, appealability, effect, destination department and active flag. A referral needs a destination to be activated; any other effect forbids a destination. |
| `DecisionTypeDocumentType.cs` | Which document types a decision type may be issued on. |
| `DocumentDecision.cs` | Issues a decision (join is refused), revokes a send-to-archive decision, cancels a mistaken entry, and attaches one decision file. |

## Implementation status

| Item | State |
| --- | --- |
| Decision type shape and activation rules | ✅ |
| Issue, snapshot, one in-force decision, number uniqueness | ✅ |
| Revoke on reopen and cancel by a system administrator | ✅ |
| Appeals and join execution | ❌ Later slices |

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-11-11 | Claude | A decision type keeps one Arabic name. The English name was removed. |
| 2026-11-11 | Claude | Added decision types and document decisions. Issuing a join decision returns `Decisions.JoinTargetInvalid`. |
