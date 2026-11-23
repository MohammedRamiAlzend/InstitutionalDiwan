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
| `DriveOnlyOfficeOptions.cs` | Binds `Drive:OnlyOffice`: `ServerUrl`, `JwtSecret`, `CallbackBaseUrl`, `DocumentUrlLifetimeMinutes` (15), `TokenLifetimeMinutes` (60), `Enabled` (true). Implements `IDriveOnlyOfficeSettings`; `IsConfigured` requires enabled + server URL + secret + callback base URL. `Enabled` and the per-value presence flags feed `GET /api/drive/onlyoffice/readiness` without exposing the secret. |
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

## Installing on Windows (this project's development machine)

The Windows installer deploys the Document Server under `C:\Program Files\ONLYOFFICE\DocumentServer`
and registers three services (`DsProxySvc`, `DsDocServiceSvc`, `DsConverterSvc`). The proxy (Nginx)
listens on **port 80**, so `ServerUrl` is `http://localhost` — **not** `http://localhost:8080`.

| Piece | Where |
| --- | --- |
| Health probe | `GET http://localhost/healthcheck` → `true` |
| Browser editor API | `http://localhost/web-apps/apps/api/documents/api.js` (200, `application/javascript`) |
| Settings override | `C:\Program Files\ONLYOFFICE\DocumentServer\config\local.json` (wins over `default.json`) |
| Shared JWT secret | `services.CoAuthoring.secret.{inbox,outbox,browser,session}.string` in `local.json` |
| Windows services | `DsProxySvc` (Nginx), `DsDocServiceSvc` (DocService), `DsConverterSvc` |

`services.CoAuthoring.token.enable` is already on for the browser config and for both request
directions (`inbox`/`outbox`); the four secret strings normally hold the same generated 32-character
value created by the installer. Read it only to copy it into the API's user-secrets — never commit it
or print it in logs.

> **Port 8080 belongs to SeaweedFS in this environment, not to OnlyOffice.** The S3 gateway is `8333`
> (`Drive:Storage:Endpoint = http://127.0.0.1:8333`) and the volume server keeps `8080`. The Docker
> recipe below publishes `8080:80`, so on a host where SeaweedFS already owns 8080 map another host
> port (for example `-p 8880:80`) and set `ServerUrl` to that address.

Change the secret by editing the four `*.string` values in `local.json`, then restart the services
from an administrator PowerShell and restart the API so it reloads `Drive:OnlyOffice`:

```powershell
Restart-Service DsProxySvc, DsDocServiceSvc, DsConverterSvc
```

Match the API side (development, from `InstitutionalDiwan.Server`):

```bash
dotnet user-secrets set "Drive:OnlyOffice:ServerUrl" "http://localhost"
dotnet user-secrets set "Drive:OnlyOffice:CallbackBaseUrl" "http://localhost:5028"
dotnet user-secrets set "Drive:OnlyOffice:JwtSecret" "<the value from local.json>"
```

To copy the installed secret without displaying it (PowerShell, repository root):

```powershell
$secret = (Get-Content 'C:\Program Files\ONLYOFFICE\DocumentServer\config\local.json' -Raw |
  ConvertFrom-Json).services.CoAuthoring.secret.inbox.string
dotnet user-secrets set "Drive:OnlyOffice:JwtSecret" $secret --project InstitutionalDiwan.Server
```

`start-drive-services.ps1` in the repository root automates this start-up: it checks port 80 and the
SeaweedFS ports first, reuses whatever already answers, and starts only what is missing — the three
services here through `Start-Service` (so run it from an elevated PowerShell).

## Readiness check (no secrets exposed)

`GET /api/drive/onlyoffice/readiness` (permission `DRIVE_USE`) reports whether the editing path is
wired up. It returns presence flags, the non-secret URLs, and the resolved save-callback URL only — the
JWT secret and the storage credentials are never part of the response.

```json
{
  "isReady": true,
  "onlyOffice": {
    "enabled": true,
    "serverUrlConfigured": true,
    "callbackBaseUrlConfigured": true,
    "jwtSecretConfigured": true,
    "isConfigured": true,
    "serverUrl": "http://localhost",
    "callbackBaseUrl": "http://localhost:5028",
    "callbackUrl": "http://localhost:5028/api/drive/onlyoffice/callback"
  },
  "storage": { "isConfigured": true }
}
```

`isReady` is true only when `onlyOffice.isConfigured` **and** `storage.isConfigured` are both true.
Call it from Scalar (`http://localhost:5028/scalar/v1`) after signing in, or with a token:

```bash
curl -s http://localhost:5028/api/drive/onlyoffice/readiness -H "Authorization: Bearer $TOKEN"
```

## Deployment checklist

Run top to bottom; every row must pass before the editor is considered connected.

| # | Check | How | Expected |
| --- | --- | --- | --- |
| 1 | Document Server is running | `curl http://localhost/healthcheck` | `true` (HTTP 200) |
| 2 | Browser can load the editor API | `curl -I http://localhost/web-apps/apps/api/documents/api.js` | 200, `application/javascript` |
| 3 | JWT is enabled | `services.CoAuthoring.token.enable.browser/request` in `local.json` | all `true` |
| 4 | Both sides share one secret | readiness `jwtSecretConfigured: true`, then edit one document | editor opens and saves; a mismatch makes OnlyOffice answer `{"error":6}` (invalid token) |
| 5 | API knows the server URL and callback base | readiness `serverUrlConfigured` / `callbackBaseUrlConfigured` | both `true`, `callbackUrl` shows the API address OnlyOffice can reach |
| 6 | Storage is configured | readiness `storage.isConfigured` | `true` (`Drive:Storage` endpoint + bucket + keys) |
| 7 | Browser → OnlyOffice | open a `.docx` in Drive | the in-site editor renders, not the «المحرر غير مضبوط» message |
| 8 | OnlyOffice → SeaweedFS | the same open | document content appears (the presigned host must resolve from OnlyOffice) |
| 9 | OnlyOffice → API | edit and close the document | `POST /api/drive/onlyoffice/callback` returns 200 `{"error":0}` in the API log |
| 10 | API → OnlyOffice → storage | the same save | a new version appears with `Source = EditorSave`; unchanged bytes only touch the session |
| 11 | Conflict guard | open the same file twice from two accounts, save the older session | the save is rejected and the editor receives `DriveEditSaveFailed` |

Checklist notes: `CallbackBaseUrl` must be reachable **from the OnlyOffice host** (`localhost` is
acceptable only when both run on the same machine), and the presigned document host
(`Drive:Storage:Endpoint`) must resolve from both the browser and OnlyOffice. A 500
`Drive.OnlyOfficeNotConfigured` from `edit-config` means the API is not configured or was not restarted
after the user-secrets change; a 500 `Drive.StorageNotConfigured` means `Drive:Storage` is incomplete.

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
| 2026-11-22 | Buffy | Documented the native **Windows** install (Nginx proxy on port 80, services `DsProxySvc`/`DsDocServiceSvc`/`DsConverterSvc`, the shared secret under `services.CoAuthoring.secret.*.string` in `config/local.json`), warned that **8080 is SeaweedFS** and not the Document Server, and added an 11-row deployment checklist. Documented and implemented the secret-free readiness probe `GET /api/drive/onlyoffice/readiness` (`GetDriveReadinessQuery`): presence flags plus the non-secret URLs, `isReady` = OnlyOffice configured **and** storage configured. |
| 2026-11-19 | Qoder | Drive Phase 4 (plan `plans/module-11-drive-backend.md`): added `DriveOnlyOfficeOptions` (`Drive:OnlyOffice`), `OnlyOfficeTokenService` (HS256 sign/verify), and `DriveCallbackDownloader`, registered in DI. Documented Docker deployment, the user-secrets JWT secret, and the four-leg network reachability between browser, OnlyOffice, this API, and SeaweedFS. |