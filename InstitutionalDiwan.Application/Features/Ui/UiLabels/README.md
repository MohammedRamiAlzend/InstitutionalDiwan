# Application / Features / Ui / UiLabels — editable UI terminology

> **Status:** Implemented — read/set/reset slices for the superadmin UI label replacements
> (plan `plans/ui-labels-edit-mode.md`, Phase 0).

## Purpose

Holds the use cases behind `api/ui-labels`: the SPA boots with the stored replacements for its
built-in labels (buttons, page titles, table columns, sidebar entries), and the superadmin edits
them in place. Only overridden keys are stored — an absent key means the front end's built-in
default applies. Every write returns the full override set plus a cache `version` (ticks of the
newest write, 0 when empty) so the client refreshes in one round trip.

## Contents

| File | Purpose |
| --- | --- |
| `UiLabelOverridesResponse.cs` | Wire shape `{ version, overrides }`; `From(overrides)` computes the version and the key→value map. |
| `GetUiLabelOverridesQuery.cs` | Query + handler returning every stored replacement; any signed-in user may read. |
| `SetUiLabelOverrideCommand.cs` | Command + handler: upserts one key (normalizes via `UiLabelOverride.NormalizeKey`), stamps `UpdatedAt`/`UpdatedByUserId` from `IUnitOfWork.UtcNow` and the caller, saves, returns the full set. |
| `ResetUiLabelOverrideCommand.cs` | Command + handler: removes one key's replacement; an unknown key is a successful no-op (no save). |
| `SetUiLabelOverrideCommandValidator.cs` | FluentValidation: key must normalize to lowercase letter/digit segments separated by single dots or dashes (≤150); value non-blank and ≤200. Error codes `UiLabel.KeyInvalid`, `UiLabel.ValueEmpty`, `UiLabel.ValueTooLong`. |

## Rules

- Keys are case-insensitive on input (`NAV.DOCUMENTS` → `nav.documents`); the domain owns the
  normalization and the format/length invariants (`DomainErrors.UiLabel`).
- Placeholder parity (`{name}` etc.) is checked by the front end against its built-in defaults
  before saving; the server stores the text as given.
- Writes are authorized by the `UI_LABELS_MANAGE` policy (superadmin account only — see
  `Server/Authorization/UiLabelPolicies.cs`), not by a grantable permission code.
- Every write is audited by `TamperEvidentAuditInterceptor` as `ORGANIZATION` /
  `AuditEntityType.UiLabelOverrideId` (19).

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-11-17 | Qoder | Phase 0 of `plans/ui-labels-edit-mode.md`: query/set/reset slices, response with cache version, and the set validator. |