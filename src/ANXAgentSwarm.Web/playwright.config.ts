import { defineConfig, devices } from '@playwright/test'

/**
 * Playwright configuration for ANXAgentSwarm E2E tests.
 * See https://playwright.dev/docs/test-configuration
 */
export default defineConfig({
  testDir: './e2e',
  /* Run tests in files in parallel */
  fullyParallel: true,
  /* Fail the build on CI if you accidentally left test.only in the source code. */
  forbidOnly: !!process.env.CI,
  /* Retry on CI only */
  retries: process.env.CI ? 2 : 0,
  /* Opt out of parallel tests on CI. */
  workers: process.env.CI ? 1 : undefined,
  /* Reporter to use. */
  reporter: [
    ['html', { outputFolder: 'playwright-report' }],
    ['json', { outputFile: 'playwright-report/test-results.json' }],
    ['list']
  ],
  /* Shared settings for all the projects below. */
  use: {
    /* Base URL to use in actions like `await page.goto('/')`. */
    baseURL: process.env.PLAYWRIGHT_BASE_URL || 'http://localhost:5173',

    /* Collect trace when retrying the failed test. */
    trace: 'on-first-retry',
    
    /* Capture screenshot on failure */
    screenshot: 'only-on-failure',
    
    /* Record video on failure */
    video: 'on-first-retry',
  },

  /* Configure projects for major browsers */
  projects: [
    {
      name: 'chromium',
      use: { ...devices['Desktop Chrome'] },
    },

    {
      name: 'firefox',
      use: { ...devices['Desktop Firefox'] },
    },

    {
      name: 'webkit',
      use: { ...devices['Desktop Safari'] },
    },

    /* Test against mobile viewports. */
    {
      name: 'Mobile Chrome',
      use: { ...devices['Pixel 5'] },
    },
    {
      name: 'Mobile Safari',
      use: { ...devices['iPhone 12'] },
    },

    /* Accessibility testing project */
    {
      name: 'accessibility',
      use: { ...devices['Desktop Chrome'] },
      testMatch: '**/*.a11y.spec.ts',
    },

    /* Visual regression testing project */
    {
      name: 'visual',
      use: { ...devices['Desktop Chrome'] },
      testMatch: '**/*.visual.spec.ts',
    },
  ],

  /* Run your local dev server before starting the tests */
  webServer: [
    {
      command: 'npm run dev',
      url: 'http://localhost:5173',
      reuseExistingServer: !process.env.CI,
      timeout: 120 * 1000,
    },
    {
      command: 'cd ../../ANXAgentSwarm.Api && dotnet run',
      url: 'http://localhost:5046/health',
      reuseExistingServer: !process.env.CI,
      timeout: 120 * 1000,
    },
  ],

  /* Timeout settings */
  timeout: 30 * 1000,
  expect: {
    timeout: 10 * 1000,
  },

  /* Output folder for test artifacts */
  outputDir: 'playwright-results',

  /* Snapshot path template for visual comparisons */
  snapshotPathTemplate: '{testDir}/__snapshots__/{testFilePath}/{arg}{ext}',
})
