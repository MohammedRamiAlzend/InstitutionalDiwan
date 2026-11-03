# Presentation / Contracts / Relations — document relation requests

> **Status:** Implemented.

## Purpose

HTTP request records for Module 08, consumed by `DocumentRelationsController`. The read models are the
Application responses in `Features/Relations`.

## Contents

| File | Purpose |
| --- | --- |
| `RelationsRequests.cs` | `CreateDocumentRelationRequest` (target document, relation type, optional notes) and `RemoveDocumentRelationRequest` (the required removal reason). |

## Implementation status

| Item | State |
| --- | --- |
| Create and remove requests | ✅ Implemented |

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-11-03 | Claude | Added the Module 08 requests. |
