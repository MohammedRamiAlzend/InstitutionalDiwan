# Presentation / Properties

> **Status:** Implemented — two folder publish profiles (`Http-4567`, `Https-5678`) build the SPA,
> fold `dist/` into `wwwroot`, and seed the Kestrel binding; `launchSettings.json` keeps the local
> `http` / `https` development profiles.

## Purpose

Hosting and publishing configuration for the API. The publish profiles are the single place that
decides **where Kestrel binds**, **whether the SPA is shipped inside the API**, and **which API base
URL the SPA is compiled with**.

## Contents

| File | Purpose |
| --- | --- |
| `launchSettings.json` | Local development profiles: `http` on `http://localhost:5028`, `https` on `https://localhost:7126;http://localhost:5028` (Development, SPA proxy hosting startup). Not used by a published build. |
| `PublishProfiles/Http-4567.pubxml` | Folder publish: builds the SPA, stages `dist/` into `wwwroot`, and binds Kestrel to `http://0.0.0.0:4567`. Output: `InstitutionalDiwan.Server/publish/Http-4567/`. |
| `PublishProfiles/Https-5678.pubxml` | Folder publish: same SPA pipeline, binds Kestrel to `https://0.0.0.0:5678` and requires a certificate at runtime. Output: `InstitutionalDiwan.Server/publish/Https-5678/`. |

## Publishing

From the repository root:

```bash
dotnet publish InstitutionalDiwan.Server -c Release -p:PublishProfile=Http-4567
dotnet publish InstitutionalDiwan.Server -c Release -p:PublishProfile=Https-5678
```

Visual Studio: right-click `InstitutionalDiwan.Server` → **Publish** → pick the profile.

Each profile runs, in order:

1. `npm ci` in `..\institutionaldiwan.front` — only when `node_modules` is missing (a warm
   `node_modules` is reused).
2. `npm run build` (`tsc -b && vite build`), with `VITE_API_BASE_URL=$(FrontendApiBaseUrl)` and
   `VITE_HUB_BASE_URL=$(FrontendHubBaseUrl)` exported to the build.
3. A wipe of `InstitutionalDiwan.Server/wwwroot` followed by a copy of `institutionaldiwan.front/dist/*`
   into it, so `wwwroot` mirrors `dist/` exactly. Those files are added to the publish manifest.
4. After publish: `appsettings.Production.json` in the output folder is seeded with
   `"Urls": "<binding>"` and `"AllowedHosts": "*"`.

`wwwroot/` and `publish/` are generated and git-ignored; never edit them by hand.

## SPA API base URL

`FrontendApiBaseUrl` is `/api` in both profiles, because the SPA is served from `wwwroot` by the same
Kestrel instance — same origin, no CORS, no hardcoded domain, and it keeps working whatever host or
port the API is published on.

Only for a cross-origin deployment (SPA on another host) set an absolute value, e.g.
`<FrontendApiBaseUrl>https://diwan.example.org/api</FrontendApiBaseUrl>`, and add a CORS policy to
`Program.cs`. `VITE_*` values are baked into the bundle at build time — changing them means
publishing again.

## Runtime configuration

The published app starts as `Production` (the ASP.NET Core default when `ASPNETCORE_ENVIRONMENT` is
unset) and reads `appsettings.Production.json` from its own folder. If the service sets
`ASPNETCORE_ENVIRONMENT` to any other name, that file is ignored — set `ASPNETCORE_URLS` in that
environment (or add `Urls` to the matching `appsettings.<Environment>.json`) instead.

Runtime settings it reads:

| Setting | Source | Effect |
| --- | --- | --- |
| `Urls` | seeded `appsettings.Production.json` | Kestrel binding: `http://0.0.0.0:4567` or `https://0.0.0.0:5678`. |
| `ASPNETCORE_URLS` | environment (optional) | Overrides `Urls`, e.g. `ASPNETCORE_URLS=http://0.0.0.0:8080`. |
| `ASPNETCORE_Kestrel__Certificates__Default__Path` | environment | PFX path for the HTTPS profile. |
| `ASPNETCORE_Kestrel__Certificates__Default__Password` | environment | PFX password. |
| `Jwt__SecretKey` | environment / secret store | Optional (≥ 32 bytes when set). When absent, a signing key is generated once and persisted — see below. |
| `Jwt__SigningKeyPath` | environment / `appsettings.json` | Where the generated key lives. Default `keys/jwt-signing.key`, relative to the content root. |
| `ConnectionStrings__DefaultConnection` | environment / secret store | SQL Server connection string. |

`ASPNETCORE_ENVIRONMENT` and the certificate variables belong to the service, container, or shell
that starts `dotnet InstitutionalDiwan.Server.dll` — not to the publish profile, which only ships
files.

If a source `appsettings.Production.json` is ever added (for a real connection string, for example),
the profile leaves it untouched and emits a build warning; keep an `Urls` entry in it, or set
`ASPNETCORE_URLS`, otherwise Kestrel falls back to its default ports.

### JWT signing key

`Jwt:SecretKey` is not baked into the publish output. At startup the app uses the configured key when
one exists (user-secrets in Development, `Jwt__SecretKey` on the server); otherwise it generates a
64-byte random key, writes it base64-encoded to `Jwt:SigningKeyPath`
(default: `<content root>/keys/jwt-signing.key`, `0600` on Unix) and logs a warning with that path.

- The key survives restarts and redeployments of the same folder — tokens and session stamps stay
  valid.
- `keys/` is **not** part of the artifact: it only exists on the machine that runs the app, so the
  secret never travels in a zip/backup of the publish folder. Back it up with the deployment.
- Deleting or replacing the file (or scaling to instances with different folders) rotates the key:
  every issued token is rejected and users sign in again, so share one path/volume across instances.
- Set `Jwt__SecretKey` explicitly when an external secret store owns the key; it always wins.
- A publish that wipes the output folder (Visual Studio's "Delete existing files", a fresh Docker
  layer, a re-extracted ZIP) also wipes `keys/`. Back it up first, or point
  `Jwt__SigningKeyPath` at a persistent location outside the publish folder (for example
  `/var/lib/institutionaldiwan/jwt-signing.key`).

### HTTPS profile

Kestrel needs a server certificate for `https://0.0.0.0:5678`. Either export a PFX and set the two
variables above, or, on a development machine with the ASP.NET Core dev certificate installed,
let Kestrel fall back to it (**not** suitable for a real deployment). Linux containers have no
fallback certificate, so the variables are mandatory there.

The SPA and the API share the origin, so the certificate also covers every `/api` and `/hubs` call.
When TLS is terminated by a reverse proxy instead, publish the HTTP profile and let the proxy own
the certificate; `UseHttpsRedirection` is skipped when the app itself has no `https://` binding.

## Implementation status

| Item | State |
| --- | --- |
| SPA build + `dist` → `wwwroot` on publish | ✅ `PublishFrontendBuild` target in `InstitutionalDiwan.Server.csproj`, opt-in via `PublishFrontend=true` |
| API base URL passed to the SPA build | ✅ `FrontendApiBaseUrl` / `FrontendHubBaseUrl` → `VITE_API_BASE_URL` / `VITE_HUB_BASE_URL` |
| Kestrel binding per profile | ✅ seeded `appsettings.Production.json` (`PublishRuntimeBinding` target) |
| Plain `dotnet publish` | ✅ stays API-only (`wwwroot` is not published without a profile) |
| JWT signing key | ✅ configured value wins, otherwise generated once and persisted (`keys/jwt-signing.key`); never in the artifact |
| HTTPS certificate handling | ✅ documented env-var contract (no certificate is shipped) |

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-11-15 | opencode | Documented the JWT signing-key behaviour (`Jwt__SecretKey` optional; generated once at `Jwt__SigningKeyPath`, default `keys/jwt-signing.key`, never shipped in the publish output) and added `Jwt:SigningKeyPath` to `appsettings.json`. |
| 2026-11-15 | opencode | Added `Http-4567` / `Https-5678` folder publish profiles: they build the SPA, copy `dist/` into `wwwroot`, and seed the Kestrel binding; documented the runtime environment variables. |
