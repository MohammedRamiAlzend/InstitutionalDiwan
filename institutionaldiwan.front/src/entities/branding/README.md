# institutionaldiwan.front / src/entities/branding — Institution Name and Logo

> **Status:** Implemented.

## Purpose

Owns the read of the institution's public identity through the anonymous `GET /api/branding` (Arabic and
English name plus the version of the uploaded logo) and the pieces every screen uses to show it. Editing
the name and uploading the logo belong to `features/institution-settings-manage`.

## Contents

| File | Purpose |
| --- | --- |
| `model/types.ts` | `BrandingResponse`: the names and `logoUpdatedAt` (null without a logo; also the cache version). |
| `api/brandingApi.ts` | Fetches the branding through the shared HTTP client; no sign-in is needed. |
| `model/queries.ts` | `brandingKeys` and `useBranding` (five-minute stale time). |
| `lib/brand.ts` | `DEFAULT_BRAND_NAME` and `brandingLogoUrl`, the versioned address of `GET /api/branding/logo`. |
| `ui/BrandMark.tsx` | The institution mark: the uploaded logo, or the default landmark badge when there is none. |
| `model/useBrandName.ts` | `useBrandName`: the stored Arabic name, defaulting while it loads. |
| `ui/BrandName.tsx` | `BrandName`: renders that name as plain text. |
| `ui/BrandingEffects.tsx` | Sets the browser tab title and icon from the stored name and logo; renders nothing. |
| `index.ts` | Public exports. |

## Conventions / Notes

- Imports only `shared` and packages. The sign-in page, shell, header and dashboard read the name and mark
  from here instead of hard-coding them.
- The logo address carries `?v=<logoUpdatedAt>`, so uploading a new logo shows at once while unchanged ones
  stay cached. The settings feature invalidates `brandingKeys.all` after an upload, removal or name change.

## Implementation status

| Item | State |
| --- | --- |
| Branding read, mark, name, tab title and icon | Implemented. |

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-11-11 | Claude | Added the entity: branding query, versioned logo address, `BrandMark`, `BrandName` and `BrandingEffects`. |
