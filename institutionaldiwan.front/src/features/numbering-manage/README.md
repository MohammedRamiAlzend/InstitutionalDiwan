# institutionaldiwan.front / src/features/numbering-manage — Numbering & Document Number Writes

> **Status:** ✅ Implemented (Module 03 Phase 5; registry form extended in Module 04 Phases 2 & 5). All
> registry administration and official-number lifecycle writes, permission-gated in the UI and
> enforced by the server.

## Purpose

Encapsulates every numbering write: registry create/update/deactivate/activate, and the document
number lifecycle — issue, cancel, replace, and department registration. Each dialog owns its Zod
schema, `useMutation`, RFC 7807 → Arabic error mapping, and query invalidation; it never renders in
another layer's surface by itself.

## Layer & Import Rules (FSD Strict Hierarchy)

- Allowed: `@/entities/*`, `@/shared/*` (public barrels only).
- Forbidden: `@/app/*`, `@/pages/*`, `@/widgets/*`, sibling `@/features/*`.
- Layout: `ui/`, `model/`, `api/`, `index.ts`.

## Contents

| File | Purpose |
| --- | --- |
| `model/types.ts` | Create/update registry, issue, cancel, replace, and department-registration request payloads. |
| `model/schemas.ts` | Zod schemas: registry form (code/name/scope/format/reset policy), issue (optional year), cancel (required reason), replace (required reason), department registration (department, register number, date, optional notes). The format pattern must expand to known tokens only (`{YEAR}`, `{SEQ}`, `{SEQ:Dn}`) and must contain `{SEQ}`. |
| `api/registryMutations.ts` | `useCreateNumberingRegistryMutation`, `useUpdateNumberingRegistryMutation`, `useSetNumberingRegistryActiveMutation`; invalidate registry lists/details. |
| `api/numberMutations.ts` | `useIssueOfficialNumberMutation`, `useCancelOfficialNumberMutation`, `useReplaceOfficialNumberMutation`, `useRecordDepartmentRegistrationMutation`; invalidate the document number history, registry entries, and document detail/list keys. |
| `ui/RegistryFormDialog.tsx` | Create/edit registry dialog with scope selects (department/type/direction), format pattern with a live next-number preview (`renderNumberPattern`) and a lock once counters exist, and reset policy. The department list keeps the registry's own department visible — marked «(موقوف)» and disabled — even after that department was deactivated, so editing never hides the real scope. |
| `ui/IssueNumberDialog.tsx` | Issues the next official number for a DRAFT document; `Numbering.OfficialNumberTaken` and other server rejections render in-dialog. |
| `ui/CancelNumberDialog.tsx` | Cancels an active number with a required Arabic reason. |
| `ui/ReplaceNumberDialog.tsx` | Replaces an active number with a required reason; keeps the prior number as «مُستبدل». |
| `ui/DepartmentRegistrationDialog.tsx` | Records the number/date a department wrote in its own register; requires an active department-scoped registry. |
| `index.ts` | Public barrel exporting request types, mutation hooks, and dialogs. |

## Domain rules & error handling

- Only DRAFT documents can be issued a number; issuing moves the document to REGISTERED.
- `Numbering.OfficialNumberTaken` (duplicate number/year) is shown in Arabic inside the issue dialog,
  which stays open with the document still draft.
- Department registration requires the destination department to have an active department-scoped
  registry; otherwise the Arabic «سجل ترقيم الدائرة غير مهيأ.» is shown.
- The API stamps replacement history with an English `Replaces …` note; `pages/documents` translates
  it to «يستبدل الرقم …» at render time.

## Implementation status

| Item | State |
| --- | --- |
| Registry writes | ✅ Implemented (create, edit, deactivate/activate) |
| Registry form preview & token validation | ✅ Implemented (Module 04 Phase 2) |
| Inactive-department scope option | ✅ Implemented (Module 04 Phase 5) — the registry's deactivated department stays visible, disabled, and labelled «(موقوف)» |
| Number lifecycle writes | ✅ Implemented (issue, cancel, replace, department registration) |
| Browser verification | ✅ Issue (draft → registered `2026/000005`), department registration (`DEP-2026-001`), duplicate `2026/000007` rejected in Arabic, replace (`2026/000008` → `2026/000009`), cancel with reason, all on the live API; live pattern preview (`{YEAR}/{SEQ:D3}` → `2026/001`), `{FOO}` and a missing `{SEQ}` both rejected in Arabic |

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-10-30 | agent | Module 04 Phase 5: the registry form keeps its own department in the scope select after that department is deactivated (option disabled and labelled «(موقوف)»), fixing a blank select that hid the registry's real scope; verified in-browser. |
| 2026-10-29 | agent | Module 04 Phase 2: added a live next-number preview to the registry form and stricter pattern validation (unknown tokens and a missing `{SEQ}` are rejected in Arabic); verified in-browser. |
| 2026-10-29 | agent | Module 03 Phase 5: implemented the slice — registry create/edit/activate mutations and dialogs, issue/cancel/replace/department-registration mutations and dialogs, with key invalidation and Arabic domain errors; verified end to end in-browser. |
