# `src/pages/documents` — Correspondence Registry & Document Details

> **Status:** Registry page ✅ implemented (Module 03 Phase 2), document details ✅ implemented
> (Module 03 Phase 3), the access-grants tab ✅ implemented (Module 03 Phase 4), the numbers tab
> ✅ implemented (Module 03 Phase 5), the movements tab ✅ implemented (Module 03 Phase 6), the
> external-organizations admin tab ✅ implemented (Module 03 Phase 7), and the files tab
> ✅ implemented (Module 06 Phases 2–4: grid, download, upload, versions, archiving, verify).
> `/documents` now hosts the registry and the correspondent catalog; `/documents/:documentId` renders
> details, participants, access grants, official numbers/department registrations, movement routing
> with its timeline, status history, the audit timeline, and the document's attachment files with
> their full write actions for editors.

## Purpose

Renders the correspondence registry at `/documents` (tabbed with the external-correspondent
administration tab, Phase 7) and the document detail at
`/documents/:documentId`. Detail composes Phase 3 metadata/participants/timeline, the Phase 3
lifecycle actions (draft edit, register, participant, cancel), the Phase 4 access-grant tab
(grant/revoke behind `ACCESS_GRANT_MANAGE`), the Phase 5 numbers tab (issue, cancel, replace,
department registration, and full number history behind `NUMBERING_VIEW`/`NUMBERING_MANAGE`), and
the Phase 6 movements tab (open/send/receive/complete/reject/return/redirect/close/cancel behind
`DOCUMENT_EDIT`, with the movement chain and event log), and the Module 06 files tab (the attachment
grid with its filters, download, and — for `DOCUMENT_EDIT` on a document that is not canceled — the
atomic first upload).

## Strict FSD Rules

- Allowed: `@/widgets/*`, `@/features/*`, `@/entities/*`, `@/shared/*`.
- Forbidden: `@/app/*`, sibling pages.
- Public API: `DocumentsPage` and `DocumentDetailsPage` via `index.ts`.

## Contents

| File | Purpose |
| --- | --- |
| `ui/DocumentsPage.tsx` | Registry page: `RequirePermission DOCUMENT_VIEW`, «الوثائق» header, the `registry`/`organizations` tab switcher (URL-backed), and the matching table. |
| `ui/DocumentDetailsPage.tsx` | Detail page: fetch details/timeline, header status mutations filtered by status + permission, URL tabs, and the four lifecycle dialogs. |
| `ui/DocumentOverviewTab.tsx` | Details tab: metadata grid, status history (with cancel/edit reasons), and the audit timeline section. |
| `ui/DocumentParticipantsTab.tsx` | Participants tab: role, target type, resolved target name, primary flag, notes, and the add action. |
| `ui/DocumentPersonsTab.tsx` | People named on the document: full grid with role change and removal for `DOCUMENT_EDIT`, frozen on archived or canceled documents. |
| `ui/DocumentGrantsTab.tsx` | Access-grants tab: per-grant level label, active/expired/revoked state badge, grantee name (or short id without `USER_MANAGE`), grant/expiry/revocation metadata, and the منح/إلغاء dialogs. |
| `ui/DocumentArchiveTab.tsx` | Archive-history tab: event type, status transition, location, actor and reason, newest first, with loading/error/empty states. |
| `ui/DocumentDetailsPage.tsx` («الروابط» tab) | Mounts `DocumentRelationsPanel` for `?tab=relations`; writes need `DOCUMENT_EDIT` and a document that is neither archived nor cancelled. |
| `ui/DocumentDetailsPage.tsx` («الحقول» tab) | Mounts `DocumentFieldsPanel` for `?tab=fields`; the tab is hidden when the document has no pinned schema, and values are editable only for a draft with `DOCUMENT_EDIT`. |
| `ui/DocumentDetailsPage.tsx` («تصدير PDF») | Header action that mounts `DocumentPrintSheet` until the print dialog closes. |
| `ui/DocumentNumbersTab.tsx` | Numbers tab: central number card (status, registration date, issuer, cancellation reason, replace/cancel actions, dated event list), department register cards, and the «سجل الأرقام» history grid on the shared `DataTable` (`tableId="document-numbers"`, client mode, column chooser, sorting, URL-backed search, row menu نسخ الرقم/نسخ المعرّف); statuses and event labels come from `DOCUMENT_NUMBER_STATUS`/`DOCUMENT_NUMBER_EVENT_TYPE`, and the API's English `Replaces …` note is translated to «يستبدل الرقم …». |
| `ui/DocumentMovementsTab.tsx` | Movements tab: one `MovementCard` per movement (type/status badges, action buttons from `getMovementActions`, ordered step chain with per-step outcome label, registry number and department, and the event log), plus the open-movement and action dialogs. |
| `ui/DocumentFilesTab.tsx` | Files tab: the section card (heading plus the archiving-does-not-hide note and the versions/verify mention) around `widgets/document-attachments-panel`; receives `canManage` from the page and passes it down, otherwise carries no state — the grid owns the query, filters, download, and the five dialogs. |
| `ui/DocumentTimeline.tsx` | Structural Arabic timeline rebuilt from `eventKind`/status/number-event ids; hides the server's English descriptions. |
| `ui/ParticipantTarget.tsx` | Resolves the single participant target (department name, external organization name via by-id read, or person name). |
| `index.ts` | Public barrel exporting both pages. |

## Widgets / features composed

- `widgets/document-registry-table` ✅
- `features/document-create` ✅, `features/document-manage` ✅ (Phase 3)
- `features/document-access-grant` ✅ (Phase 4)
- `features/numbering-manage` ✅ (Phase 5)
- `features/movement-routing` ✅ (Phase 6)
- `widgets/external-organizations-table` ✅ + `features/external-organization-manage` ✅ (Phase 7)
- `widgets/document-attachments-panel` ✅ (Module 06 Phase 2)
- `features/attachment-upload` (planned, Module 06 Phase 3)

## URL state

- List: `?tab=registry|organizations` (default `registry`); the registry adds
  `?page=&pageSize=&search=&statusId=&directionId=&documentTypeId=&owningDepartmentId=&sortBy=&sortDescending=`,
  and the organizations tab adds `?page=&pageSize=&search=&include=active|all`.
- Detail: `?tab=details|participants|numbers|movements|files|grants`; unknown or unauthorized tabs
  fall back to `details`. `grants` is live behind `ACCESS_GRANT_MANAGE`; `numbers` is live behind
  `NUMBERING_VIEW`; `movements` and `files` are live for `DOCUMENT_VIEW`. Every tab chip is now
  available, so no «قريبًا» badge remains.
- Inside the files tab the grid adds `?status=active|archived`, `?fileType=<id>`, `?search=`,
  `?sort=`, `?dir=`, `?page=`, and `?pageSize=`. Switching tabs writes `?tab=` alone, which drops
  those grid params — a deliberate consequence of the existing tab switcher, not of the files tab.

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
| إرسال / إعادة توجيه / إغلاق / إلغاء الحركة | movement status allows it (`getMovementActions`), caller holds `DOCUMENT_EDIT`, and caller's scope covers the side of the current step: sender for send, cancel and a draft's redirect; receiver for redirect once sent; either for close |
| استلام / إكمال / رفض / إرجاع | movement/step status allows it, caller holds `DOCUMENT_EDIT`, and caller's scope covers the current step's receiver; receive still needs the destination department's register number |
| إلغاء الوثيقة | caller holds `DOCUMENT_EDIT` and the document is neither canceled nor archived. The server also requires MANAGE access, which depends on grant levels the client is never told, so a denial arrives as the Arabic `Correspondence.DocumentAccessDenied` message in the dialog |
| تبويب الملفات | caller holds `DOCUMENT_VIEW` (the route gate); no separate permission hides it |
| تنزيل الإصدار الحالي | files tab open and the file has a version. Offered to every viewer because the per-document DOWNLOAD level is never exposed to the client — a denial surfaces as the Arabic `403` message |
| رفع ملف / رفع إصدار / أرشفة | caller holds `DOCUMENT_EDIT` **and** the document is not canceled (`canEdit && !isCanceled`, passed down as `canManage`). Hidden, not disabled, in line with the rest of the write actions here; the server additionally requires MANAGE access on the document, and that denial also arrives as an Arabic message. An archived row offers neither a new version nor archiving. |
| تنزيل / الإصدارات / التحقق من البصمة | every viewer of the tab — no write permission is checked client-side; download additionally needs the per-document DOWNLOAD level, which the client is never told, so its `403` is translated rather than predicted |
| تبويب الجهات الخارجية | caller holds `DOCUMENT_VIEW` (the whole `/documents` route gate) |
| جهة خارجية جديدة / تعديل البيانات / إيقاف / تفعيل | organizations tab open and caller holds `DOCUMENT_EDIT`; نسخ الرمز/المعرّف stay available to every reader |

Server-side rejections (`Correspondence.OnlyDraftEditable`, `Correspondence.HasActiveMovements`,
`Correspondence.AlreadyCanceled`, `Numbering.RegistryNotFound`, `Movements.CentralDiwanRoutingRequired`,
`Numbering.DepartmentRegistrationRequired`, `Movements.InvalidStatusTransition`, …) render inside the
mounted dialog.

## Implementation status

| Item | State |
| --- | --- |
| Slice folder + rules | ✅ Documented |
| `ui/DocumentsPage.tsx` | ✅ Implemented (Module 03 Phase 2); organizations tab added in Phase 7 |
| External-organizations tab | ✅ Implemented (Module 03 Phase 7: registry/organizations switcher, grid, create/edit/status dialogs) |
| `ui/DocumentDetailsPage.tsx` + tabs | ✅ Implemented (Module 03 Phase 3) |
| `ui/DocumentGrantsTab.tsx` | ✅ Implemented (Module 03 Phase 4) |
| `ui/DocumentArchiveTab.tsx` | ✅ Implemented (Module 07) |
| `ui/DocumentNumbersTab.tsx` | ✅ Implemented (Module 03 Phase 5); history grid moved onto `DataTable` in Module 04 Phase 4 |
| `ui/DocumentMovementsTab.tsx` | ✅ Implemented (Module 03 Phase 6) |
| `ui/DocumentFilesTab.tsx` | ✅ Implemented (Module 06 Phases 2–4) — the tab chip went from a disabled «قريبًا» placeholder to live, mounting `widgets/document-attachments-panel`, and forwards `canManage` so editors get upload, new version, and archiving while viewers keep download, versions, and verify; browser verification ✅ in Module 06 Phase 5 (three roles, including the access-level denial) |
| Browser verification | Registry ✅ (2026-10-28); details ✅ (2026-10-28): draft actions, `?tab=participants`, person + department participants, draft edit, register (`2026/000004`), cancel with reason, status history, timeline, and canceled-document gating. Grants ✅ (2026-10-28): `?tab=grants` loaded, empty submit and past expiry rejected in Arabic, grant to `user` at «عرض» with expiry/reason appeared immediately with counts, revoke kept the row as «ملغى» with timestamp + revoker, and a `USER`-role login saw no grants chip and fell back to details. Numbers ✅ (2026-10-29): draft issue → registered `2026/000005`, department registration `DEP-2026-001`, duplicate `2026/000007` rejected in Arabic with the dialog retained and the document still draft, replace `2026/000008` → `2026/000009`, and cancel `2026/000009` with an Arabic reason; a `USER`-role login saw no numbers tab and the issue/department/registry endpoints returned 403. Movements ✅ (2026-10-29): the full two-department cycle (open+send from the Central Diwan, receive at HR against `HR-2026-118`, reject with a reason, automatic reroute back to the Central Diwan, receive at the Central Diwan against `CEN-2026-1000`, close) updated the queues and the movements tab at every step; the chain showed the rejected step as «رُفضت» followed by the registered receive, with Arabic notes and the full event log, and the document returned to «مسجل» with «فتح حركة» available again. Organizations ✅ (2026-10-29): `?tab=organizations` loaded from the URL, a new `MOI-01`/«حكومي» correspondent was created («تمت إضافة الجهة الخارجية.»), a second record with the same code kept the dialog open with «توجد جهة خارجية أخرى بنفس الرمز.», editing carried the disabled code and prefilled fields («تم تحديث بيانات الجهة الخارجية.»), and the participant picker offered «وزارة الداخلية — محدَّث · MOI-01»; after «إيقاف الجهة» the row showed «موقوف» under `include=all` and the participant picker listed no external organizations, and «تفعيل الجهة» restored «نشط» and the option. Files ✅ (2026-11-01, Module 06 Phase 5): `?tab=files` over the permanent fixture document in three roles — manager (grid, filters, search/sort, column chooser, both empty states, upload, second version with the first marked «مُستبدل», downloads of current and replaced versions under Arabic names, verify with matching digest, archive with the badge and the write entries then hidden), `USER` with `DOCUMENT_EDIT` (write UI present, but a VIEW-only grant made download and upload answer «لا تملك صلاحية الوصول إلى هذه الوثيقة.» while the same calls succeeded on a MANAGE-granted document), and read-only (no write buttons at all). |

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-11-10 | Claude | Document details gains the «سجل الديوان» tab; archive, unarchive and cancel show only to the creating department. |
| 2026-11-09 | Claude | Document page gained the «تصدير PDF» header action. |
| 2026-11-09 | Claude | The participants tab and the add-participant action are hidden while `FEATURES.participants` is off. |
| 2026-11-09 | Claude | The external organizations tab and the tab bar are hidden while `FEATURES.externalOrganizations` is off. |
| 2026-11-09 | Claude | Removed «تاريخ الوثيقة» from the overview; the creation date remains. |
| 2026-11-09 | Claude | Removed the summary from the header and overview, and the edit-draft action. |
| 2026-11-09 | Claude | Added the «الأشخاص المرتبطون» tab (`ui/DocumentPersonsTab.tsx`). |
| 2026-11-08 | Claude | Movements tab shows the register number and visit date per step; numbers tab lists each line's arrival dates. |
| 2026-11-05 | Claude | The overview tab and the page header badge no longer show direction, confidentiality or priority (suspended). |
| 2026-11-04 | Claude | User, department and employee references now render as «name + short id» (`UserLabel` / `DepartmentLabel` / `EmployeeLabel`) instead of a bare id. |
| 2026-11-03 | Claude | Added the «الحقول» tab for the dynamic document fields. |
| 2026-11-03 | agent | Module 08: `DocumentDetailsPage` gained the «الروابط» tab (`?tab=relations`) mounting `DocumentRelationsPanel`. |
| 2026-11-03 | agent | Module 07: `DocumentDetailsPage` gained «أرشفة»/«إلغاء الأرشفة» actions (`DOCUMENT_ARCHIVE`), the `archive` tab (`ui/DocumentArchiveTab.tsx`), and now treats an archived document as frozen for participants, files and numbers. |
| 2026-11-03 | Claude | Movements tab offers only the actions on the caller's side of the current step. Verified in-browser against a scratch API with sender, receiver and unrelated users, including the Arabic denials on document cancel, edit, register and open movement. |
| 2026-11-01 | agent | Module 06 Phase 5: verified the `files` tab in-browser in three roles against a permanent fixture document, including the per-access-level denial (`Correspondence.DocumentAccessDenied` in Arabic on download and upload for a VIEW-only grant, success under MANAGE) and the absence of the `grants` tab for a user without `ACCESS_GRANT_MANAGE`. No code change. |
| 2026-11-01 | agent | Module 06 Phase 4: the files tab's `canUpload` prop became `canManage` and now gates three write actions (upload, new version, archive); the row menu gained الإصدارات and التحقق من البصمة for every viewer, and the tab description says what the grid can do. |
| 2026-11-01 | agent | Module 06 Phase 3: `DocumentDetailsPage` now passes `canUpload={canEdit && !isCanceled}` to the files tab, so the panel gains its «رفع ملف» toolbar button and empty-state action for editors on a live document and stays unchanged for viewers and canceled documents. |
| 2026-11-01 | agent | Module 06 Phase 2: added `ui/DocumentFilesTab.tsx` and made the «الملفات» chip live (`available: true`, `'files'` in `AvailableTabKey`, a `resolveTab` branch, and the conditional mount), so the last disabled tab on the detail page is gone. The tab is a stateless card around `widgets/document-attachments-panel`; the grid owns the query, the URL filters, and the download. |
| 2026-10-30 | agent | Module 04 Phase 5: verified the numbers tab in-browser as a `NUMBERING_VIEW`-only user (`num.viewer`) — the central-number card shows only «تحديث الأرقام», no issue/replace/cancel/register actions, and the registration lists plus history grid render read-only while direct writes return 403. |
| 2026-10-29 | agent | Module 04 Phase 4: replaced the bespoke history table in `ui/DocumentNumbersTab.tsx` with the shared `DataTable` (`tableId="document-numbers"`, client mode, column chooser persisted at `diwan.table.document-numbers.columns.v1`, sorting, URL-backed search, row menu نسخ الرقم/نسخ المعرّف) and keyed the status/event labels off `DOCUMENT_NUMBER_STATUS`/`DOCUMENT_NUMBER_EVENT_TYPE`; the central-number card and department registration event lists are unchanged. Verified in-browser on `70c1b912…` and `c2fd1217…`: search and sort round-trip through the URL, hidden columns survive a reload, the row menu offers both copy actions, and نشط/ملغى/مُستبدل render in Arabic. |
| 2026-10-29 | agent | Module 03 Phase 7: added the `organizations` tab to `ui/DocumentsPage.tsx` (URL-backed `?tab=registry\|organizations` switcher with the page-level `DOCUMENT_VIEW` gate, each tab owning its table state) mounting `widgets/external-organizations-table`; verified create/duplicate/edit/deactivate/activate in-browser and the participant-picker effect of deactivation. |
| 2026-10-29 | agent | Module 03 Phase 6: added the live `ui/DocumentMovementsTab.tsx` (movement cards with type/status badges, status/permission-driven action buttons, the step chain with per-step outcome labels, and the event log) plus the open-movement and action dialogs, and made the `movements` chip available. Server-written English system notes are translated before rendering; the completed-at label follows the step outcome because the API stamps it on every step end. |
| 2026-10-29 | agent | Verified the Phase 5 numbers tab in-browser: a draft issued `2026/000005` and moved to REGISTERED, the department registration recorded `DEP-2026-001` with event 4, a duplicate manual number produced the Arabic «رقم النظام مستخدم بالفعل…» with the dialog open and the document still draft, replace moved `2026/000008` to «مُستبدل» and created `2026/000009`, cancel marked `2026/000009` «ملغى» with the Arabic reason, and the replacement note rendered as «يستبدل الرقم 2026/000008». A `USER`-role login saw no numbers tab and the registry/issue/department-registration writes returned 403. |
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
