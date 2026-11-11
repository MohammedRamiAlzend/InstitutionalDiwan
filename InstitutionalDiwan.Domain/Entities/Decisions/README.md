# Domain / Entities / Decisions

> **Status:** Implemented for slices B and C of `plans/case-decisions.md`. Joining two files is not implemented; `Decisions.JoinTargetInvalid` stays the refusal to issue a join decision.

## Purpose

A decision type is an administrator-managed catalog entry. A document decision is one ruling on one document, and it keeps a snapshot of the type so a later edit of the type does not rewrite history.

## Contents

| File | Purpose |
| --- | --- |
| `DecisionLookups.cs` | `DecisionEffect` (1 refer, 2 send to archive, 3 join, 4 none), `DecisionStatus` (1 issued, 2 appealed, 3 final, 4 revoked, 5 canceled) and `AppealOutcome` (1 pending, 2 accepted, 3 rejected). In force means status 1, 2 or 3. A ruling outcome is accepted or rejected. |
| `DecisionType.cs` | Arabic name, appealability, effect, destination department and active flag. A referral needs a destination to be activated; any other effect forbids a destination. |
| `DecisionTypeDocumentType.cs` | Which document types a decision type may be issued on. |
| `DocumentDecision.cs` | Issues a decision (join is refused), records an appeal and its outcome, revokes a send-to-archive decision, cancels a mistaken entry, and attaches one decision file. |
| `DecisionAppeal.cs` | One appeal on a decision: date, appellant department, optional reference and notes, outcome, and who recorded each step. |

## Implementation status

| Item | State |
| --- | --- |
| Decision type shape and activation rules | ✅ |
| Issue, snapshot, one in-force decision, number uniqueness | ✅ |
| Revoke on reopen and cancel by a system administrator | ✅ |
| Record an appeal and its outcome | ✅ Issued and appealable becomes appealed; rejection makes it final; acceptance revokes it. An archived document may still be appealed. |
| Join execution | ❌ Issuing a join decision returns `Decisions.JoinTargetInvalid`. |

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-11-11 | Claude | Appeals (slice C): `DecisionAppeal` and `AppealOutcome`. Recording an appeal moves an issued appealable decision to appealed and raises `DecisionAppealedEvent`. A rejected outcome makes the decision final; an accepted outcome revokes it. Join execution stays refused. |
| 2026-11-11 | Claude | A decision type keeps one Arabic name. The English name was removed. |
| 2026-11-11 | Claude | Added decision types and document decisions. Issuing a join decision returns `Decisions.JoinTargetInvalid`. |
