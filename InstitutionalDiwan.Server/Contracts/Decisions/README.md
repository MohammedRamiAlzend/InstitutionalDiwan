# Server / Contracts / Decisions

> **Status:** Implemented.

## Purpose

HTTP request bodies for the decision endpoints. Responses live in `Application/Features/Decisions`.

## Contents

| File | Purpose |
| --- | --- |
| `DecisionContracts.cs` | Create and update a decision type, issue a decision, attach its file, and cancel it with a reason. |

## Implementation status

| Item | State |
| --- | --- |
| Request records | ✅ |

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-11-11 | Claude | Added the decision request bodies. |
