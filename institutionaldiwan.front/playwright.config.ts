import { defineConfig } from '@playwright/test'

// The only test workflow here drives a managed Browserless browser from inside the test
// (see e2e/browserless.spec.ts), so no local Playwright browser needs to be downloaded:
//   npm run test:e2e
export default defineConfig({
  testDir: './e2e',
  // One Browserless session at a time keeps usage and the free-tier concurrency sane.
  fullyParallel: false,
  workers: 1,
  timeout: 60_000,
  expect: { timeout: 15_000 },
  reporter: 'list',
})