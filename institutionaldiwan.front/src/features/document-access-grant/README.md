# institutionaldiwan.front / src/features/document-access-grant — Document Access Grants

> **Status:** ✅ Implemented (Module 03 Phase 4, 2026-10-28) — grant a user a document access
> level and revoke a grant. The document details page owns the tab and permission gate.

## 1. Purpose

Owns the two `ACCESS_GRANT_MANAGE` commands on one document: `POST` a grant (user + level +
optional reason/expiry, caller recorded as grantor) and `DELETE` (revoke; the row is kept with
revoker and timestamp). Listing stays in `entities/document` (`useDocumentAccessGrants`).

## 2. Strict FSD Rules

- Allowed Imports: `@/entities/document`, `@/entities/lookup`, `@/entities/user`, and `@/shared/*`.
- Forbidden Imports: NEVER import from `@/app`, `@/pages`, `@/widgets`, or sibling `@/features/*`.
- Public API: `AddAccessGrantDialog`, `RevokeAccessGrantDialog`, both mutations, the request type,
  and the Zod schema via `src/features/document-access-grant/index.ts`.
- No permission checks here; the page hides the tab and buttons when the caller lacks
  `ACCESS_GRANT_MANAGE`.

## 3. Contents

| File | Purpose |
| --- | --- |
| `model/types.ts` | `GrantDocumentAccessRequest` mirroring `InstitutionalDiwan.Server.Contracts.Identity.GrantDocumentAccessRequest`. |
| `model/schemas.ts` | `grantAccessSchema`: user and level required, reason ≤500 chars, optional `yyyy-MM-dd` expiry that must be after today (the server rejects past or same-day values). |
| `api/accessGrantMutations.ts` | `useGrantDocumentAccessMutation` (`POST /api/documents/{id}/access-grants`) and `useRevokeDocumentAccessMutation` (`DELETE …/{grantId}`); both invalidate `documentKeys.grants(documentId)`. |
| `ui/AddAccessGrantDialog.tsx` | Debounced user search over `GET /api/identity-options/users` (inactive accounts filtered out; the chosen label stays selectable), access-level select from `access-levels`, optional expiry date, optional reason. |
| `ui/RevokeAccessGrantDialog.tsx` | Destructive confirmation naming the grantee; surfaces `Identity.GrantAlreadyRevoked` / `Identity.AccessGrantNotFound` in Arabic. |
| `index.ts` | Public barrel. |

## 4. Behaviour & limits

- The grant endpoint accepts `access-levels` ids 1–4 (عرض / تنزيل / إحالة / إدارة); the level list
  comes from the lookup catalog with the seeded Arabic fallback.
- A grant whose `expiresAt` is not in the future is rejected by the domain
  (`Identity.InvalidDateRange`), so the form validates the date client-side first.
- **Revoke has no reason field:** `DELETE /api/documents/{documentId}/access-grants/{grantId}`
  records the caller and the timestamp only. The dialog therefore confirms without a reason, and
  the revoked row keeps its grant reason.
- Success toasts: «تم منح الوصول إلى الوثيقة.» / «تم إلغاء تصريح الوصول.».

## 5. Implementation Status

| Item | State |
| --- | --- |
| Types, schema, mutations, dialogs, barrel | ✅ Implemented |
| Interactive verification | ✅ Verified in-browser (2026-10-28): empty submit showed «اختر المستخدم.» / «اختر مستوى الوصول.»; a 2020 expiry showed «تاريخ الانتهاء يجب أن يكون بعد اليوم.» without closing; granting `user` «عرض» with 30/06/2027 expiry and a reason added the row (ساري) with grantor `admin`; revoking left the row as «ملغى» with the revocation timestamp and revoker, counts moved to 0 ساري; a `USER`-role login saw no grants tab and the direct `POST` returned 403. |
| Bulk/department-level grants | Out of scope — the API grants per user only. |
| Revocation reason | Not supported by the API; see §4. |

## 6. Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-10-28 | agent | Verified Module 03 Phase 4 in-browser against the live API: required-field and past-expiry Arabic validation, a successful grant rendered immediately with level, state, grantee, grantor and expiry, revoke kept the row as a record with timestamp + revoker, and a caller without `ACCESS_GRANT_MANAGE` had no tab and got 403 from the endpoint. |
| 2026-10-28 | agent | Module 03 Phase 4: implemented the slice — request type, grant schema with future-expiry validation, grant/revoke mutations invalidating `documentKeys.grants`, the add dialog (user picker, level lookup, expiry, reason), and the revoke confirmation. |
| 2026-10-22 | opencode | Converted this `README.md` from the Word document it was authored in to markdown, so the `README.md`-per-folder rule in `AGENT.md` holds and the content is diffable and agent-editable. |
| 2026-10-19 | agent | Created `features/document-access-grant` slice folder and `README.md`. |
