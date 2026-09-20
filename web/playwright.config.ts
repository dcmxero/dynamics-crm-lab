import { defineConfig, devices } from '@playwright/test';

const port = 4300;

export default defineConfig({
  testDir: './e2e',
  fullyParallel: true,
  // A test that only passes on a retry is a flaky test, and a flaky test in CI
  // is worse than no test, so failures are not retried away.
  retries: 0,
  reporter: process.env['CI'] ? 'github' : 'list',

  use: {
    baseURL: `http://localhost:${port}`,
    trace: 'on-first-retry',
  },

  projects: [{ name: 'chromium', use: { ...devices['Desktop Chrome'] } }],

  // The suite stubs the API at the network layer, so only the client is served.
  webServer: {
    command: `npm run start -- --port ${port}`,
    url: `http://localhost:${port}`,
    reuseExistingServer: !process.env['CI'],
    timeout: 180_000,
  },
});
