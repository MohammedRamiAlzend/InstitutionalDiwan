# Publishing the Client & Wiring API Endpoints

> **Applies to:** `institutionaldiwan.client` (React 19 + TypeScript + Vite 8 SPA).
> **Last updated:** 2026-10-25.
>
> This guide covers two things:
> 1. How to build/publish the SPA and point it at the right API host (including changing an IP).
> 2. How to wire a backend endpoint into the client, end to end.
>
> Configuration lives in `.env` (git-ignored, already present in this folder). The committed
> template is [`.env.example`](.env.example).

---

## 1. Configuration model

Vite reads configuration from `.env`, `.env.local`, and `.env.<mode>` **in this folder**. Process
environment variables exported by the shell (or by Visual Studio) override file values.

| Key | Scope | Used by | Purpose |
| --- | --- | --- | --- |
| `DEV_SERVER_PORT` | dev server only | `vite.config.ts` | HTTPS port of `npm run dev` (default `61325`). |
| `DEV_HOST` | dev server only | `vite.config.ts` | Interface/hostname to bind (`true` = all interfaces). |
| `DEV_ALLOWED_HOSTS` | dev server only | `vite.config.ts` | Allowed host headers when using a custom hostname. |
| `DEV_PROXY_TARGET` | dev server only | `vite.config.ts` | Absolute backend origin the dev proxy forwards `/api` and `/hubs` to. |
| `VITE_API_BASE_URL` | **build-time, public** | browser bundle | Base URL the HTTP client prepends to every request. Default `/api`. |

Two rules matter:

1. **`VITE_*` values are baked into the JavaScript bundle at build time.** Editing `.env` after
   `npm run build` changes nothing — rebuild and redeploy. Never put secrets in a `VITE_*` key.
2. **Plain `DEV_*` keys never reach the browser.** They are read only by `vite.config.ts`.

The `.env` file in this folder is ignored by git (see `.gitignore`); `.env.example` is committed and
documents every key. Start from it on any new machine:

```bash
cd institutionaldiwan.client
cp .env.example .env   # only if .env does not exist yet
```

---

## 2. Setting the API host (development)

In development the browser always calls the relative `/api` path. Vite proxies that prefix to the
backend, so no CORS is involved. `vite.config.ts` resolves the proxy target in this order:

1. `DEV_PROXY_TARGET` from `.env` or the shell;
2. `ASPNETCORE_HTTPS_PORT` (set automatically when Visual Studio launches the API);
3. the first URL in `ASPNETCORE_URLS`;
4. `https://localhost:7126`.

To pin a specific backend host or LAN IP, put it in `.env`:

```dotenv
DEV_PROXY_TARGET=http://192.168.1.20:5028
```

Both prefixes are proxied:

| Prefix | Purpose |
| --- | --- |
| `^/api` | REST controllers (`/api/documents`, `/api/audit-logs`, …). |
| `^/hubs` | SignalR hubs, including the WebSocket upgrade (enabled once Module 10 lands). |

Leave `VITE_API_BASE_URL=/api`. The dev proxy handles the origin; changing it to an absolute URL
would bypass the proxy and require CORS.

---

## 3. Build

```bash
cd institutionaldiwan.client
npm ci            # or npm install on a fresh machine
npm run build     # tsc -b && vite build
npm run preview   # optional: serve dist/ locally and click through it
```

Output: `institutionaldiwan.client/dist/` — static HTML, JS, CSS, and assets. `npm run build`
type-checks first, so a TypeScript error fails the build. Keep `npm run lint` clean as well.

---

## 4. Publish

The SPA is static files; the API is a separate ASP.NET Core process. Pick one of the two models.

### Option A — Same origin behind a reverse proxy (recommended)

Serve `dist/` and reverse-proxy `/api` and `/hubs` to the backend on the same hostname. This avoids
CORS, keeps bearer tokens on one origin, and lets SignalR use relative `/hubs/...` URLs.

**Integrated with the ASP.NET server.** The server already has `UseDefaultFiles()`,
`MapStaticAssets()`, and `MapFallbackToFile("/index.html")` in `InstitutionalDiwan.Server/Program.cs`.

```bash
cd institutionaldiwan.client
npm ci
npm run build

# Copy the built SPA into the server's web root before publishing, so the static-assets
# manifest includes it.
rm -rf ../InstitutionalDiwan.Server/wwwroot
mkdir -p ../InstitutionalDiwan.Server/wwwroot
cp -r dist/. ../InstitutionalDiwan.Server/wwwroot/

cd ..
dotnet publish InstitutionalDiwan.Server/InstitutionalDiwan.Server.csproj \
  -c Release -o ./publish
```

Deploy `publish/` to IIS/Kestrel. `VITE_API_BASE_URL=/api` stays correct because the API is on the
same origin. If you copy `dist` into an **already published** folder instead, confirm
`MapStaticAssets` serves the new files; the supported path is copying before `dotnet publish`.

**IIS / NGINX in front of a separate API process.** Point the site root at `dist/` and add the SPA
fallback plus `/api` and `/hubs` proxy rules.

IIS (`web.config`):

```xml
<configuration>
  <system.webServer>
    <rewrite>
      <rules>
        <rule name="SPA fallback" stopProcessing="true">
          <match url=".*" />
          <conditions logicalGrouping="MatchAll">
            <add input="{REQUEST_FILENAME}" matchType="IsFile" negate="true" />
            <add input="{REQUEST_FILENAME}" matchType="IsDirectory" negate="true" />
          </conditions>
          <action type="Rewrite" url="/index.html" />
        </rule>
      </rules>
    </rewrite>
  </system.webServer>
</configuration>
```

NGINX:

```nginx
server {
    listen 443 ssl;
    server_name diwan.example.org;
    root /var/www/institutionaldiwan;
    index index.html;

    location / {
        try_files $uri $uri/ /index.html;
    }

    location /api/ {
        proxy_pass http://127.0.0.1:5028;
        proxy_set_header Host $host;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }

    location /hubs/ {
        proxy_pass http://127.0.0.1:5028;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection "upgrade";
        proxy_set_header Host $host;
    }
}
```

### Option B — Static host + cross-origin API

Host `dist/` on IIS, NGINX, Azure Static Web Apps, Cloudflare Pages, Netlify, etc., and call the API
on another origin:

```dotenv
VITE_API_BASE_URL=https://api.example.org/api
```

Then **rebuild**. This model requires server-side CORS for the SPA origin (including credentials if
cookies are ever used) and CORS/WebSocket origin configuration for SignalR. The API currently has no
CORS policy configured, so prefer Option A until that is added in
`InstitutionalDiwan.Server/Program.cs`.

### Deployment checklist

- [ ] `npm ci && npm run build` passes locally.
- [ ] `VITE_API_BASE_URL` matches the deployment model (`/api` for same origin).
- [ ] SPA history fallback rewrites unknown paths to `index.html`.
- [ ] `/api` and `/hubs` reach the backend (WebSocket upgrade allowed for hubs).
- [ ] HTTPS end to end; no mixed-content calls from an HTTPS page to an HTTP API.
- [ ] `.env` is **not** in the published output or the repository.

---

## 5. Changing the API host or IP

| What changed | Edit | Rebuild? |
| --- | --- | --- |
| Backend port/host for local development | `.env` → `DEV_PROXY_TARGET` (or let `ASPNETCORE_*` supply it) | No — restart `npm run dev`. |
| API origin for a deployed build | `.env` (or CI secret) → `VITE_API_BASE_URL` | **Yes** — `npm run build` then redeploy. |
| Proxy prefixes or target resolution order | `vite.config.ts` (`server.proxy`, `target` fallback chain) | Dev restart. |
| Same-origin reverse proxy upstream | IIS `web.config`, NGINX site, ingress, or load-balancer config | No client rebuild. |
| Typed env access / HTTP client defaults | `src/shared/config/env.ts`, `src/shared/api/httpClient.ts` (planned) | Yes. |
| CORS for a cross-origin API | `InstitutionalDiwan.Server/Program.cs` + hosting config | Backend redeploy. |

If the only change is a new IP/port and the SPA is served same-origin, change the reverse proxy
upstream — no client rebuild is needed. If the SPA calls the API cross-origin, change
`VITE_API_BASE_URL` and rebuild.

The browser client reads the base URL once, from `ENV.API_BASE_URL` (the planned typed accessor in
`src/shared/config/env.ts`). `src/shared/api/httpClient.ts` passes it as axios `baseURL`, so every
API function uses a path relative to it (`/documents`, not `/api/documents`).

---

## 6. Wiring a backend endpoint into the client

`knowledge.md` is the source of truth for live routes and permissions. Do not invent a route; if the
controller does not exist yet, the client slice stays blocked.

The snippets below reference the planned `src/shared/api`, `src/shared/config`, and `src/shared/types`
modules (see [`src/shared/README.md`](src/shared/README.md)). If they are still scaffolding, create
that shared layer first — every entity and feature depends on it.

### Step 1 — Confirm the contract

Read the controller in `InstitutionalDiwan.Server/Controllers/` and the matching row in
[`knowledge.md`](knowledge.md). Note the HTTP method, route, query/body shape, response envelope
(`PagedResult<T>` vs plain array), required permission, and domain error codes.

### Step 2 — Add the read model

`src/entities/<slice>/model/types.ts` — TypeScript types mirroring the backend response DTOs
(camelCase JSON). Keep them structural; do not import generated server types.

### Step 3 — Add the API function (read)

`src/entities/<slice>/api/<verb><Thing>.ts`:

```ts
import { httpClient } from '@/shared/api';
import type { PagedResult } from '@/shared/types';
import type { DocumentListItem } from '../model/types';

export type ListDocumentsParams = {
  page?: number;
  pageSize?: number;
  search?: string;
  sortBy?: string;
  sortDescending?: boolean;
};

export async function listDocuments(params: ListDocumentsParams) {
  const { data } = await httpClient.get<PagedResult<DocumentListItem>>('/documents', { params });
  return data;
}
```

Paths are relative to `VITE_API_BASE_URL` (`/api`), so use `/documents`, not `/api/documents`.

### Step 4 — Add the query key + hook

`src/entities/<slice>/model/queries.ts`:

```ts
export const documentKeys = {
  all: ['documents'] as const,
  list: (params: ListDocumentsParams) => [...documentKeys.all, 'list', params] as const,
  detail: (id: string) => [...documentKeys.all, 'detail', id] as const,
};

export function useDocuments(params: ListDocumentsParams) {
  return useQuery({
    queryKey: documentKeys.list(params),
    queryFn: () => listDocuments(params),
    placeholderData: keepPreviousData,
  });
}
```

Reads live in `entities`; the URL (not `useState`) owns filters, search, sorting, and paging.

### Step 5 — Add a write path (feature)

For `POST`/`PUT`/`PATCH`/`DELETE`, create a `features/<slice>` slice with a Zod schema, an API
function, and `useMutation`. Invalidate the owning entity keys on success — never a hand-written
key string.

```ts
export function useCreateDocument() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: createDocument,
    onSuccess: () => queryClient.invalidateQueries({ queryKey: documentKeys.all }),
  });
}
```

### Step 6 — Surface it in the UI

- Tables use `shared/ui/data-table` with a stable table id, default columns, URL filters/paging, and
  a permission-filtered row/header context menu (see the root `AGENTS.md`).
- Actions are reachable from the context menu **and** an always-visible overflow button; both call
  the feature's mutation.
- Permission codes come from `src/shared/config/permissions.ts` (must match
  `InstitutionalDiwan.Domain/Common/Permissions.cs`). The server remains authoritative: still handle
  `403`.
- Errors: branch on `ApiError.code` (ProblemDetails `title`) and show `ApiError.detail`; show the
  loading/empty/error/forbidden states required by `AGENTS.md`.
- Special case: `POST /api/audit-logs/integrity-checks` returns **200** with `isValid: false` when
  the chain is broken — read the body, do not raise a transport error.

### Step 7 — Chaining two endpoints

When one action needs two backend calls (for example create a document, then upload its first
file), run them in order and treat the pair as one workflow:

```ts
const document = await createDocument.mutateAsync(payload);
try {
  await uploadFirstFile.mutateAsync({ documentId: document.documentId, file });
} catch (error) {
  // The document exists but the file failed: surface a retry action instead of pretending
  // the whole operation rolled back.
  return { documentId: document.documentId, fileError: error };
}
```

Prefer a single atomic backend route when one exists (attachments already expose the atomic
`POST /api/documents/{documentId}/files`). Invalidate every affected entity key after the pair, and
make retries idempotent or clearly resumable.

### Step 8 — Update the docs and verify

- Add or update the folder `README.md` (`Contents`, status, Change Log) per `AGENTS.md`.
- If the backend contract or permissions changed, update `knowledge.md` in the same turn.
- Run `npm run build` (typecheck) and `npm run lint`; exercise loading, empty, error, forbidden, and
  pagination states.

---

## 7. Troubleshooting

| Symptom | Likely cause / fix |
| --- | --- |
| API calls go to the Vite dev server and 404 | Proxy target wrong or backend not running; check `DEV_PROXY_TARGET` / `ASPNETCORE_*`. |
| 401 on every call | Missing/expired bearer token; the HTTP client must attach `Authorization`. |
| 403 on a screen that works for admin | Missing permission code or stale token claims; check the policy and `permissions.ts`. |
| CORS error only after deployment | Cross-origin `VITE_API_BASE_URL` without a server CORS policy. Use Option A or add CORS. |
| Refreshing `/documents` returns 404 | SPA history fallback missing; rewrite unknown paths to `index.html`. |
| Changed the API host but the deployed app still calls the old one | `VITE_*` is baked in at build time — rebuild and redeploy. |
| SignalR connects then drops | Reverse proxy is not upgrading WebSockets for `/hubs`; enable `Upgrade`/`Connection` headers. |
| Mixed-content errors | HTTPS SPA calling an HTTP API; terminate TLS consistently. |

---

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-10-25 | Buffy | Added the publishing/configuration/API-wiring guide; replaced the template sample proxy with env-driven `^/api` and `^/hubs` routes and documented the `.env` keys. |
