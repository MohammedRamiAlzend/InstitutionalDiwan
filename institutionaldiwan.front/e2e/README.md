# institutionaldiwan.front / e2e — Browserless Playwright workflow

> **Status:** Implemented (needs a `BROWSERLESS_TOKEN` to run)

## Purpose

A minimal Playwright workflow that drives a managed browser from
[Browserless](https://docs.browserless.io/overview/getting-started/connect-playwright) instead of a
browser downloaded on this machine. It replaces `chromium.launch()` with
`chromium.connectOverCDP()` against the Browserless CDP endpoint, which makes end-to-end runs possible
on machines with no Playwright browser install and keeps the SPA's tests off local resources. The
single spec is a smoke test against a public page; it is the place to add SPA scenarios later.

## Setup

1. Create an API token at <https://browserless.io/account/>.
2. Copy `.env.example` to `.env` (git-ignored) and fill `BROWSERLESS_TOKEN=…`, or set the variable for
   one shell session:

   ```powershell
   $env:BROWSERLESS_TOKEN = '<your token>'
   ```

3. Run the workflow from `institutionaldiwan.front/`:

   ```powershell
   npm run test:e2e
   ```

The endpoint is `wss://production-sfo.browserless.io` with the token passed as `?token=`; the spec
builds it from the environment, so no token is ever committed. `test:e2e` needs no
`npx playwright install` — the browser lives at Browserless.

## Contents

| File | Purpose |
| --- | --- |
| `browserless.spec.ts` | Connects to Browserless over CDP with the token from `BROWSERLESS_TOKEN`, opens a new context/page on the managed browser, and asserts the public `example.com` page loads. |
| `../playwright.config.ts` | Playwright Test config: `testDir` is this folder, one worker so only one Browserless session is open at a time, 60 s test timeout. |

## Conventions / Notes

- Never hardcode a token or endpoint credentials; read `BROWSERLESS_TOKEN` from the environment only.
- Close the browser in a `finally` block so a failing assertion still ends the managed session.
- Keep the suite small and public-page-based unless a test needs the external API; CI has no API.
- `test-results/`, `playwright-report/`, and `blob-report/` are git-ignored.

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-11-22 | agent | Added the Browserless-connected Playwright smoke workflow (`browserless.spec.ts`, root `playwright.config.ts`, `test:e2e` script, `BROWSERLESS_TOKEN` in `.env.example`). |