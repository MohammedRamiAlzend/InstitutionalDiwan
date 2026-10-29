# `src/features/external-organization-manage` — External Correspondents (Module 03)

> **Status:** Implemented and browser-verified.

## Purpose

Writes for the Module 03 external correspondent catalog: create, update, deactivate, and
reactivate a ministry, company, or individual that documents can address. Complements the
`entities/external-organization` reads; the surface itself is the `organizations` tab of
`pages/documents` through `widgets/external-organizations-table`.

## Contents

| File | Purpose |
| --- | --- |
| `api/externalOrganizationMutations.ts` | `useCreateExternalOrganizationMutation` (`POST /api/external-organizations`), `useUpdateExternalOrganizationMutation` (`PUT /{id}`), `useSetExternalOrganizationActiveMutation` (`DELETE /{id}` = deactivate, `POST /{id}/activate`); each invalidates `externalOrganizationKeys.all` plus the edited detail |
| `model/types.ts` | `CreateExternalOrganizationRequest` (code + descriptive fields) and `UpdateExternalOrganizationRequest` (no code — it is immutable) |
| `model/schemas.ts` | `externalOrganizationFormSchema`: code ≤ 50, Arabic name ≤ 200, organization type required, English name ≤ 200, contact info ≤ 254 and must parse as an e-mail or phone (blank allowed) |
| `ui/ExternalOrganizationFormDialog.tsx` | Create/edit dialog: code (disabled while editing), type select over the `organization-types` lookup, Arabic/English names, contact info; renders the mutation error in Arabic and closes only on success |
| `index.ts` | Public barrel |

## Domain rules encoded in the UI

- The code is entered once and is read-only in edit mode; a duplicate code answers
  `Correspondence.ExternalOrgCodeAlreadyExists` and is rendered inside the dialog through
  `toArabicErrorMessage` («توجد جهة خارجية أخرى بنفس الرمز.»). The dialog stays open so the caller
  can correct the code.
- Optional fields are sent as `null`, never as an empty string, matching the server's nullable
  contract.
- Organization types come from the `organization-types` lookup (Governments 1 / Private 2 /
  International 3 / Individual 4, mirrored in `ORGANIZATION_TYPE`); the submit button waits for the
  catalog.
- Deactivation is a soft state change (the row stays in the registry with «موقوف»), and a deactivated
  organization disappears from the participant picker because that picker reads active rows only.
- Every write requires `DOCUMENT_EDIT`; the API remains the final judge and answers 403 otherwise.

## Composes

- `entities/external-organization` (query keys + response type)
- `entities/lookup` (`organization-types` labels)
- `shared/ui` (`Dialog`, `FormField`, `Input`, `Select`, `Button`)

## Implementation status

| Item | State |
| --- | --- |
| Docs | ✅ |
| Code | ✅ |
| Browser-verified | ✅ (create, duplicate-code rejection in Arabic, edit with read-only code, deactivate/activate; deactivation emptied the participant picker) |

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-10-29 | agent | Implemented the feature: request types, Zod schema, create/update/toggle-active mutations with prefix invalidation, and the create/edit dialog wired to the `organization-types` lookup. |
| 2026-10-25 | Cursor | Documented external-organization writes for the Module 03 plan (tab placement, `DOCUMENT_EDIT` gate). |
