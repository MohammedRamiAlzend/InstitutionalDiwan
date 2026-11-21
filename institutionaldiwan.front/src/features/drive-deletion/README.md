# Drive deletion

## Contents

| File | Purpose |
| --- | --- |
| `api/driveDeletionMutations.ts` | Request/review/cancel deletion, direct trash, restore, and purge mutations. |
| `model/schemas.ts` | Arabic validation for deletion reasons and review notes. |
| `ui/RequestDeletionDialog.tsx` | Required-reason deletion request dialog. |
| `ui/ReviewDeletionDialog.tsx` | Approve/reject dialog with an optional review note. |
| `index.ts` | Public feature barrel. |

## Implementation status

| Area | Status |
| --- | --- |
| Deletion workflow | ✅ Implemented in Phase 5 |

## Change Log

| Date | Author | Summary |
| --- | --- | --- |
| 2026-11-21 | Copilot | Added Drive deletion, review, restore, and purge workflows. |
