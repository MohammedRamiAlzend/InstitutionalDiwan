# widgets / document-decisions-panel

> **Status:** Implemented. Exercised in the browser on a registered base-record document: the in-force card, the history, and the archive suggestion.

## Purpose

The «القرار» tab: the decision in force, its history, and a button for the suggested next step. A referral or a send-for-archive step opens the movement dialog already filled in. Archive opens the archive dialog.

## Contents

| File | Purpose |
| --- | --- |
| `ui/DocumentDecisionsPanel.tsx` | The card, the next-action button, and the history grid (`tableId` `document-decisions`). |
| `index.ts` | Public barrel. |

## Implementation status

| Item | State |
| --- | --- |
| In-force card, history, next action, issue, attach, cancel | ✅ |

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-11-11 | Claude | Added the document decisions panel. |
