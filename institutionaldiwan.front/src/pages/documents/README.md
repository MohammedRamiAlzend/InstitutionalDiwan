# `src/pages/documents` — Correspondence Registry & Document Details

> **Status:** Registry page ✅ implemented (Module 03 Phase 2), document details ✅ implemented
> (Module 03 Phase 3), the access-grants tab ✅ implemented (Module 03 Phase 4), the numbers tab
> ✅ implemented (Module 03 Phase 5), and the movements tab ✅ implemented (Module 03 Phase 6).
> `/documents/:documentId` renders details, participants, access grants, official
> numbers/department registrations, movement routing with its timeline, status history, and the
> audit timeline; only the files tab waits on Module 06.

## Purpose

Renders the correspondence registry at `/documents` and the document detail at
`/documents/:documentId`. Detail composes Phase 3 metadata/participants/timeline, the Phase 3
lifecycle actions (draft edit, register, participant, cancel), the Phase 4 access-grant tab
(grant/revoke behind `ACCESS_GRANT_MANAGE`), the Phase 5 numbers tab (issue, cancel, replace,
department registration, and full number history behind `NUMBERING_VIEW`/`NUMBERING_MANAGE`), and
the Phase 6 movements tab (open/send/receive/complete/reject/return/redirect/close/cancel behind
`DOCUMENT_EDIT`, with the movement chain and event log); only files mount later, in Module 06.

## Strict FSD Rules

- Allowed: `@/widgets/*`, `@/features/*`, `@/entities/*`, `@/shared/*`.
- Forbidden: `@/app/*`, sibling pages.
- Public API: `DocumentsPage` and `DocumentDetailsPage` via `index.ts`.

## Contents

| File | Purpose |
| --- | --- |
| `ui/DocumentsPage.tsx` | Registry page: `RequirePermission DOCUMENT_VIEW`, «سجل الوثائق» header, registry table. |
| `ui/DocumentDetailsPage.tsx` | Detail page: fetch details/timeline, header status mutations filtered by status + permission, URL tabs, and the four lifecycle dialogs. |
| `ui/DocumentOverviewTab.tsx` | Details tab: metadata grid, status history (with cancel/edit reasons), and the audit timeline section. |
| `ui/DocumentParticipantsTab.tsx` | Participants tab: role, target type, resolved target name, primary flag, notes, and the add action. |
| `ui/DocumentGrantsTab.tsx` | Access-grants tab: per-grant level label, active/expired/revoked state badge, grantee name (or short id without `USER_MANAGE`), grant/expiry/revocation metadata, and the منح/إلغاء dialogs. |
| `ui/DocumentNumbersTab.tsx` | Numbers tab: central number card (status, registration date, issuer, cancellation reason, replace/cancel actions, dated event list), department register cards, and the «سجل الأرقام» table; translates the API's English `Replaces …` note to «يستبدل الرقم …». |
| `ui/DocumentMovementsTab.tsx` | Movements tab: one `MovementCard` per movement (type/status badges, action buttons from `getMovementActions`, ordered step chain with per-step outcome label, registry number and department, and the event log), plus the open-movement and action dialogs. |
| `ui/DocumentTimeline.tsx` | Structural Arabic timeline rebuilt from `eventKind`/status/number-event ids; hides the server's English descriptions. |
| `ui/ParticipantTarget.tsx` | Resolves the single participant target (department name, external organization name via by-id read, or person name). |
| `index.ts` | Public barrel exporting both pages. |

## Widgets / features composed

- `widgets/document-registry-table` ✅
- `features/document-create` ✅, `features/document-manage` ✅ (Phase 3)
- `features/document-access-grant` ✅ (Phase 4)
- `features/numbering-manage` ✅ (Phase 5)
- `features/movement-routing` ✅ (Phase 6)
- `features/attachment-upload` (planned, Module 06)

## URL state

- List: `?page=&pageSize=&search=&statusId=&directionId=&documentTypeId=&owningDepartmentId=&sortBy=&sortDescending=`
- Detail: `?tab=details|participants|numbers|movements|files|grants`; unknown, not-yet-available, or
  unauthorized tabs fall back to `details`. `grants` is live behind `ACCESS_GRANT_MANAGE`; `numbers`
  is live behind `NUMBERING_VIEW`; `movements` is live for `DOCUMENT_VIEW`; only the files chip
  remains disabled with «قريبًا» until Module 06.

## Transitions & gates

| Action | Visible when |
| --- | --- |
| تعديل المسودة | status = DRAFT and caller holds `DOCUMENT_EDIT` |
| تسجيل برقم | status = DRAFT and caller holds `DOCUMENT_CREATE` |
| إضافة مشارك | status ≠ CANCELED and caller holds `DOCUMENT_EDIT` |
| إلغاء الوثيقة | status ∉ {ARCHIVED, CANCELED} and caller holds `DOCUMENT_EDIT` |
| تبويب التصاريح | caller holds `ACCESS_GRANT_MANAGE` (hidden, not disabled, otherwise) |
| منح وصول | grants tab open and caller holds `ACCESS_GRANT_MANAGE` |
| إلغاء تصريح | grant state ≠ revoked and caller holds `ACCESS_GRANT_MANAGE` |
| تبويب الأرقام | caller holds `NUMBERING_VIEW` (hidden, not disabled, otherwise) |
| إصدار رقم | numbers tab open, status = DRAFT, no central number, caller holds `NUMBERING_MANAGE` |
| تسجيل رقم الدائرة | numbers tab open, a central number exists, caller holds `NUMBERING_MANAGE` |
| استبدال الرقم | number active, caller holds `NUMBERING_MANAGE` |
| إلغاء الرقم | number active, caller holds `NUMBERING_MANAGE` |
| تبويب الحركات | caller holds `DOCUMENT_VIEW` (shown, not disabled, otherwise) |
| فتح حركة | movements tab open, status = REGISTERED, no active movement, caller holds `DOCUMENT_EDIT` |
| إرسال / إعادة توجيه / إغلاق / إلغاء الحركة | movement status allows it (`getMovementActions`) and caller holds `DOCUMENT_EDIT` |
| استلام / إكمال / رفض / إرجاع | movement/step status allows it and caller holds `DOCUMENT_EDIT`; receive still needs the destination department's register number |

Server-side rejections (`Correspondence.OnlyDraftEditable`, `Correspondence.HasActiveMovements`,
`Correspondence.AlreadyCanceled`, `Numbering.RegistryNotFound`, `Movements.CentralDiwanRoutingRequired`,
`Numbering.DepartmentRegistrationRequired`, `Movements.InvalidStatusTransition`, …) render inside the
mounted dialog.

## Implementation status

| Item | State |
| --- | --- |
| Slice folder + rules | ✅ Documented |
| `ui/DocumentsPage.tsx` | ✅ Implemented (Module 03 Phase 2) |
| `ui/DocumentDetailsPage.tsx` + tabs | ✅ Implemented (Module 03 Phase 3) |
| `ui/DocumentGrantsTab.tsx` | ✅ Implemented (Module 03 Phase 4) |
| `ui/DocumentNumbersTab.tsx` | ✅ Implemented (Module 03 Phase 5) |
| `ui/DocumentMovementsTab.tsx` | ✅ Implemented (Module 03 Phase 6) |
| Files tab | Disabled placeholder until Module 06 |
| Browser verification | Registry ✅ (2026-10-28); details ✅ (2026-10-28): draft actions, `?tab=participants`, person + department participants, draft edit, register (`2026/000004`), cancel with reason, status history, timeline, and canceled-document gating. Grants ✅ (2026-10-28): `?tab=grants` loaded, empty submit and past expiry rejected in Arabic, grant to `user` at «عرض» with expiry/reason appeared immediately with counts, revoke kept the row as «ملغى» with timestamp + revoker, and a `USER`-role login saw no grants chip and fell back to details. Numbers ✅ (2026-10-29): draft issue → registered `2026/000005`, department registration `DEP-2026-001`, duplicate `2026/000007` rejected in Arabic with the dialog retained and the document still draft, replace `2026/000008` → `2026/000009`, and cancel `2026/000009` with an Arabic reason; a `USER`-role login saw no numbers tab and the issue/department/registry endpoints returned 403. Movements ✅ (2026-10-29): the full two-department cycle (open+send from the Central Diwan, receive at HR against `HR-2026-118`, reject with a reason, automatic reroute back to the Central Diwan, receive at the Central Diwan against `CEN-2026-1000`, close) updated the queues and the movements tab at every step; the chain showed the rejected step as «رُفضت» followed by the registered receive, with Arabic notes and the full event log, and the document returned to «مسجل» with «فتح حركة» available again. |

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-10-29 | agent | Module 03 Phase 6: added the live `ui/DocumentMovementsTab.tsx` (movement cards with type/status badges, status/permission-driven action buttons, the step chain with per-step outcome labels, and the event log) plus the open-movement and action dialogs, and made the `movements` chip available. Server-written English system notes are translated before rendering; the completed-at label follows the step outcome because the API stamps it on every step end. |
| 2026-10-29 | agent | Verified the Phase 5 numbers tab in-browser: a draft issued `2026/000005` and moved to REGISTERED, the department registration recorded `DEP-2026-001` with event 4, a duplicate manual number produced the Arabic «الرقم الرسمي مستخدم بالفعل…» with the dialog open and the document still draft, replace moved `2026/000008` to «مُستبدل» and created `2026/000009`, cancel marked `2026/000009` «ملغى» with the Arabic reason, and the replacement note rendered as «يستبدل الرقم 2026/000008». A `USER`-role login saw no numbers tab and the registry/issue/department-registration writes returned 403. |
| 2026-10-28 | agent | Verified the Phase 4 grants tab in-browser: `?tab=grants` opened from the URL for an admin, the grant dialog rejected an empty submit («اختر المستخدم.» / «اختر مستوى الوصول.») and a 2020 expiry («تاريخ الانتهاء يجب أن يكون بعد اليوم.»), granting access to `user` at «عرض» with a 2027 expiry and a reason produced the row and «1 ساري» immediately, and revoking left the same row as «ملغى» with the revocation timestamp and revoker and no revoke button. A `USER`-role login had no grants chip, `?tab=grants` fell back to details, and the endpoint returned 403. |
| 2026-10-28 | agent | Module 03 Phase 4: added `ui/DocumentGrantsTab.tsx` (state badges, grantee resolution, metadata, grant/revoke dialogs), made the grants tab live and permission-gated (hidden without `ACCESS_GRANT_MANAGE`, URL fallback to details), and documented the grant/revoke gates. |
| 2026-10-28 | agent | Verified the details page in-browser against the live API: a draft exposed the four actions, `?tab=participants` resolved from the URL, the participant dialog rejected an empty name in Arabic and then added a person and an HR-department participant, the external target showed an Arabic empty hint, the draft edit updated title/summary/priority, registration assigned `2026/000004` and removed the edit/register actions, and cancelling with an Arabic reason produced «ملغي» with no write actions left. Status history showed the three transitions and the timeline showed status, participant, and number events; `?tab=grants` fell back to details. Server-written system reasons remain English audit text. |
| 2026-10-28 | agent | Module 03 Phase 3: implemented `DocumentDetailsPage` with URL tabs, status/permission-driven lifecycle actions (`features/document-manage`), details/participants tabs, status history, and the structural Arabic audit timeline; registered `/documents/:documentId`. |
| 2026-10-28 | agent | Completed the interactive pass for `/documents`: route gate, sidebar activation (`aria-current="page"`), registry behaviors, and both creation paths verified against the live API. |
| 2026-10-28 | agent | Module 03 Phase 2: implemented `ui/DocumentsPage.tsx` (`DOCUMENT_VIEW` gate + `widgets/document-registry-table`); detail page deferred to Phase 3, so no `/documents/:documentId` route is registered yet. |
| 2026-10-25 | Buffy | Documented the shared data-grid contract for the registry and detail lists (column chooser, row/header context menus). |
| 2026-10-25 | Cursor | Documented Module 06 attachments panel on detail, URL tabs, and composed widgets. |
| 2026-10-22 | opencode | Converted this README from the Word document it was authored in to markdown. |
| 2026-10-19 | agent | Created `pages/documents` slice folder and README. |
