import { chromium, expect, test } from '@playwright/test'
import { existsSync } from 'node:fs'
import { fileURLToPath } from 'node:url'

// Browserless managed browsers, connected over CDP:
// https://docs.browserless.io/overview/getting-started/connect-playwright
// The token comes from BROWSERLESS_TOKEN (declared empty in .env.example) and is never committed.
const BROWSERLESS_ENDPOINT = 'wss://production-sfo.browserless.io'
const envFile = fileURLToPath(new URL('../.env', import.meta.url))

function browserlessEndpoint(): string {
  if (!process.env.BROWSERLESS_TOKEN && existsSync(envFile)) process.loadEnvFile(envFile)

  const token = process.env.BROWSERLESS_TOKEN
  if (!token) {
    throw new Error(
      'BROWSERLESS_TOKEN is not set. Copy .env.example to .env and add the token from https://browserless.io/account/, or set it in the shell.',
    )
  }

  return `${BROWSERLESS_ENDPOINT}?token=${token}`
}

test('reaches a public page through a Browserless browser', async () => {
  const browser = await chromium.connectOverCDP(browserlessEndpoint())

  try {
    // New context on the managed browser instead of chromium.launch() on this machine.
    const context = await browser.newContext()
    const page = await context.newPage()

    const response = await page.goto('https://example.com/')
    expect(response?.ok()).toBe(true)
    await expect(page.getByRole('heading', { level: 1 })).toHaveText('Example Domain')

    console.log(`Browserless answered: ${await page.title()}`)
  } finally {
    // Always end the managed session, even when an assertion above fails.
    await browser.close()
  }
})