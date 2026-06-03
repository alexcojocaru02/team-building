import { defineConfig, devices } from '@playwright/test';

export default defineConfig({
  testDir: './tests',
  fullyParallel: false,
  forbidOnly: !!process.env['CI'],
  retries: process.env['CI'] ? 2 : 0,
  workers: 1,
  timeout: process.env['CI'] ? 60_000 : 30_000,
  reporter: [['html', { open: 'never' }], ['line']],

  use: {
    baseURL: process.env['BASE_URL'] ?? 'http://localhost:4200',
    trace: 'on-first-retry',
    screenshot: 'only-on-failure',
    ignoreHTTPSErrors: true,
  },

  projects: [
    {
      name: 'chromium',
      use: { ...devices['Desktop Chrome'] },
    },
  ],

  // Porneste local API + UI doar cand nu ruleaza in CI
  webServer: process.env['BASE_URL'] ? [] : [
    {
      command: 'dotnet run --launch-profile http',
      cwd: '../TeamConnect.Api',
      url: 'http://localhost:5217/swagger/index.html',
      reuseExistingServer: true,
      timeout: 60_000,
    },
    {
      command: 'npm start',
      cwd: '../UI',
      url: 'http://localhost:4200',
      reuseExistingServer: true,
      timeout: 120_000,
    },
  ],
});
