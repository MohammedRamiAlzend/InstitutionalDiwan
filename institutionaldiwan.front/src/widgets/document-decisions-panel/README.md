# widgets / document-decisions-panel

> **Status:** Implemented. The in-force card uses only this document's decision. Decisions of the principal file are read-only. A join can suggest an ordinary movement.

## Purpose

The «القرار» tab: the decision in force, its appeals, its history, decisions of the principal file for reading, a hint about other files that share a person, and a button for the suggested next step. A referral, a send-for-archive step, or a move to the principal file's department opens the movement dialog already filled in. Archive opens the archive dialog. «تسجيل اعتراض» and «تسجيل النتيجة» stay available when the document is archived.

## Contents

| File | Purpose |
| --- | --- |
| `ui/DocumentDecisionsPanel.tsx` | The card, the appeals, the next-action button, and the history grid (`tableId` `document-decisions`). |
| `index.ts` | Public barrel. |

## Implementation status

| Item | State |
| --- | --- |
| In-force card, history, next action, issue, attach, cancel, appeal, outcome, join | ✅ Principal-file rows have no write menu. The person hint is a suggestion. |

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-11-11 | Claude | After a join, the panel names the principal file, lists its decisions as read-only, and can offer «نقل إلى ديوان الملف الرئيسي». A shared person on another visible file is only a hint. |
| 2026-11-11 | Claude | The panel lists appeals and offers «تسجيل اعتراض» and «تسجيل النتيجة», including on an archived document. |
| 2026-11-11 | Claude | Added the document decisions panel. |
