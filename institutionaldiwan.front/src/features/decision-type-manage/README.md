# features / decision-type-manage

> **Status:** Implemented. The form was exercised in the browser: a referral shows the destination, and a join type hides it and says issuing is not available yet.

## Purpose

Creates and edits decision types. The destination department is shown only for a referral. A join type says that issuing it is not available yet.

## Contents

| File | Purpose |
| --- | --- |
| `model/types.ts` | Form values. |
| `model/schemas.ts` | Zod schema: a destination is forbidden unless the effect is a referral. |
| `api/decisionTypeMutations.ts` | Create, update, activate and deactivate, invalidating `decisionTypeKeys.all`. |
| `ui/DecisionTypeFormDialog.tsx` | The form, including the document-type checkboxes. |
| `index.ts` | Public barrel. |

## Implementation status

| Item | State |
| --- | --- |
| Form, activate, deactivate | ✅ |
| UI labels | ✅ Phase 4 (decisions ب): `DecisionTypeFormDialog` renders from `decisions.type.*` — title/description/toasts/placeholders/hints/aria via `t()`, fields via `FormField labelKey`, buttons via `<Label>` (save reuses `common.action.save`), the appealable checkbox label via `t()`, Zod via `t()`. Effect options are computed in-render through `decisionEffectLabel` (which resolves via `t()`). Data-driven text (department names/codes, document-type `descAr`) stays out. |

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-11-18 | Qoder | UI labels Phase 4 (decisions ب): `DecisionTypeFormDialog` and its Zod schema render from `decisions.type.*`; effect options moved in-render so they resolve through `decisionEffectLabel`/`t()`. No Arabic literals remain. |
| 2026-11-11 | Claude | The join-effect note says issuing unification links the file to a principal file and does not change its number or place. |
| 2026-11-11 | Claude | The form asks for one Arabic name. The English name field was removed. |
| 2026-11-11 | Claude | Added decision-type administration. |
