# Infrastructure / OnlyOffice

> **Status:** Implemented (Module 11, Drive Phase 4) — HS256 editor-config/callback tokens and the
> save-callback downloader. Runs live only when `Drive:OnlyOffice` (server URL, callback base URL, JWT
> secret) **and** `Drive:Storage` (SeaweedFS endpoint + keys) are configured. The JWT secret belongs in
> user-secrets / environment variables, never in committed appsettings.

## Purpose

Connects the Drive module to a self-hosted **OnlyOffice Document Server** so Word/Excel/PowerPoint
files open in an in-site editor. This folder signs the editor configuration the browser hands to
`DocsAPI.DocEditor`, verifies the JWT OnlyOffice posts to the save callback, and downloads the edited
bytes from the temporary URL OnlyOffice supplies. The Application layer consumes these through
`IDriveOnlyOfficeSettings`, `IDriveOnlyOfficeTokenService`, and `IDriveCallbackDownloader`.

## Contents

| File | Purpose |
| --- | --- |
| `DriveOnlyOfficeOptions.cs` | Binds `Drive:OnlyOffice`: `ServerUrl`, `JwtSecret`, `CallbackBaseUrl`, `DocumentUrlLifetimeMinutes` (15), `TokenLifetimeMinutes` (60), `Enabled` (true). Implements `IDriveOnlyOfficeSettings`; `IsConfigured` requires enabled + server URL + secret + callback base URL. |
| `OnlyOfficeTokenService.cs` | Manual **HS256** JWT (`IDriveOnlyOfficeTokenService`). `Sign` serializes the payload as the token body (nested structures preserved, camelCase, relaxed escaping) and adds `iat`/`exp`; `Verify` checks the signature in fixed time and rejects expired tokens. Only the shared secret is used — no issuer/audience validation applies to the OnlyOffice exchange. |
| `DriveCallbackDownloader.cs` | `IDriveCallbackDownloader` — fetches the saved document from the callback `url` over a static `HttpClient` (2-minute timeout), accepting only absolute `http`/`https` URIs, and returns a rewound seekable stream (or null on a network/IO failure). |

## Configuration

Non-secret values live in `appsettings.json` under `Drive:OnlyOffice`; the secret is supplied out of band:

```jsonc
"Drive": {
  "OnlyOffice": {
    "Enabled": true,
    "ServerUrl": "",            // public URL of the OnlyOffice Document Server (browser-facing)
    "CallbackBaseUrl": "",      // public base URL of THIS API (OnlyOffice-facing)
    "DocumentUrlLifetimeMinutes": 15,
    "TokenLifetimeMinutes": 60
    // JwtSecret deliberately absent — set it via user-secrets / environment
  }
}
```

Set the secret (development, from the `InstitutionalDiwan.Server` project directory):

```bash
dotnet user-secrets set "Drive:OnlyOffice:JwtSecret" "<your-shared-secret>"
dotnet user-secrets set "Drive:OnlyOffice:ServerUrl" "https://onlyoffice.example.com"
dotnet user-secrets set "Drive:OnlyOffice:CallbackBaseUrl" "https://api.example.com"
```

In production use environment variables (`Drive__OnlyOffice__JwtSecret`, etc.) or a secret store. The
same secret must be configured on the OnlyOffice Document Server (`JWT_SECRET`).

## Deploying OnlyOffice Document Server (Docker)

```bash
docker run -d --name onlyoffice --restart=always -p 8080:80 \
  -e JWT_ENABLED=true \
  -e JWT_SECRET="<your-shared-secret>" \
  -e JWT_HEADER=Authorization \
  onlyoffice/documentserver
```

- `JWT_SECRET` **must equal** `Drive:OnlyOffice:JwtSecret`. With `JWT_ENABLED=true` OnlyOffice signs the
  editor config token and the save-callback token; this API signs the config with the same secret and
  verifies the callback. The token travels in the config/callback body (`token`), which is what
  `OnlyOfficeTokenService` reads.
- Put OnlyOffice behind TLS in production; `ServerUrl` is the address the **browser** loads.
- The container must be reachable from the browser, from this API (to download saved bytes), and able
  to reach this API (to post the callback) and SeaweedFS (to fetch the presigned document URL).

## Network reachability (all four legs must work)

| From → To | URL used | Why |
| --- | --- | --- |
| Browser → OnlyOffice | `Drive:OnlyOffice:ServerUrl` | Loads the editor API (`DocsAPI.DocEditor`). |
| OnlyOffice → SeaweedFS | `document.url` (presigned GET, host = `Drive:Storage:Endpoint`) | Fetches the current bytes to open the document. |
| OnlyOffice → API | `editorConfig.callbackUrl` = `CallbackBaseUrl` + `/api/drive/onlyoffice/callback` | Posts save/close callbacks. |
| API → OnlyOffice | callback `url` (temporary download URL on the OnlyOffice server) | Downloads the edited bytes to store as a new version. |
| API → SeaweedFS | `Drive:Storage:Endpoint` | `WriteAsync` stores the new version; presign/head/copy/delete. |

The presigned document URL carries the **`Drive:Storage:Endpoint` host**. If SeaweedFS is only reachable
on an internal hostname, that hostname must resolve from both the browser and the OnlyOffice container
(or expose a public endpoint), otherwise the editor cannot open the file. `CallbackBaseUrl` must be an
address the OnlyOffice container can reach — not `localhost` unless OnlyOffice runs on the same host.

## Save-callback protocol (as implemented)

- The callback is `[AllowAnonymous]`; the handler verifies the HS256 `token` (no ambient user session).
  The acting user is the edit session's owner.
- `status=1` editing → touch the session; `status=4` closed-no-change → close; `status=2` ready-to-save
  and `status=6` forcesave → save.
- Save: reject if `CurrentVersionId` ≠ session `BaseVersionId` (lost-update guard, applied to forcesave
  too) and notify the editor (`DriveEditSaveFailed`); download the bytes; dedup by SHA-256 against the
  current version (unchanged → no new version); re-check size/quota/magic bytes; `WriteAsync` a new
  object; commit a new `Source=EditorSave` version and make it current.
- Per the OnlyOffice protocol the HTTP status is **always 200**; the body is `{ "error": 0 }` on success
  or `{ "error": 1 }` on rejection.

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-11-19 | Qoder | Drive Phase 4 (plan `plans/module-11-drive-backend.md`): added `DriveOnlyOfficeOptions` (`Drive:OnlyOffice`), `OnlyOfficeTokenService` (HS256 sign/verify), and `DriveCallbackDownloader`, registered in DI. Documented Docker deployment, the user-secrets JWT secret, and the four-leg network reachability between browser, OnlyOffice, this API, and SeaweedFS. |