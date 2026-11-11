# widgets / document-decisions-panel

> **Status:** Implemented. The in-force card, the history, the archive suggestion, recording an appeal, and accepting it were exercised in the browser.

## Purpose

The «القرار» tab: the decision in force, its appeals, its history, and a button for the suggested next step. A referral or a send-for-archive step opens the movement dialog already filled in. Archive opens the archive dialog. «تسجيل اعتراض» and «تسجيل النتيجة» stay available when the document is archived.

## Contents

| File | Purpose |
| --- | --- |
| `ui/DocumentDecisionsPanel.tsx` | The card, the appeals, the next-action button, and the history grid (`tableId` `document-decisions`). |
| `index.ts` | Public barrel. |

## Implementation status

| Item | State |
| --- | --- |
| In-force card, history, next action, issue, attach, cancel, appeal, outcome | ✅ |

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-11-11 | Claude | The panel lists appeals and offers «تسجيل اعتراض» and «تسجيل النتيجة», including on an archived document. |
| 2026-11-11 | Claude | Added the document decisions panel. |
