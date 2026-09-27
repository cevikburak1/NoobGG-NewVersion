import { defineConfig, devices } from '@playwright/test';

export default defineConfig({
  testDir: './tests/e2e',
  use: {
    baseURL: 'http://localhost:3001',
    browserName: 'chromium',
    launchOptions: process.env.PLAYWRIGHT_CHROME_EXECUTABLE
      ? { executablePath: process.env.PLAYWRIGHT_CHROME_EXECUTABLE }
      : undefined,
  },
  projects: [
    { name: 'desktop', use: { ...devices['Desktop Chrome'] } },
    { name: 'mobile', use: { ...devices['Pixel 7'] } },
  ],
  webServer: {
    command: 'npm run dev -- --host 127.0.0.1 --port 3001 --strictPort',
    url: 'http://localhost:3001',
    reuseExistingServer: !process.env.CI,
    timeout: 60_000,
  },
});
