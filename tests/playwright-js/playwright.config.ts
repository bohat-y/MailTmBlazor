import { defineConfig, devices } from '@playwright/test';
import 'dotenv/config';

const baseURL = process.env.PLAYWRIGHT_BASE_URL || 'http://localhost:5004';

export default defineConfig({
  testDir: './tests',
  timeout: 30 * 1000,
  retries: process.env.CI ? 2 : 0,
  workers: process.env.CI ? 1 : undefined,
  use: {
    baseURL,
    trace: 'on-first-retry'
  },
  projects: [
    {
      name: 'chromium',
      use: { ...devices['Desktop Chrome'] }
    },
    {
      name: 'firefox',
      use: {
        ...devices['Desktop Firefox'],
        launchOptions: {
          firefoxUserPrefs: {
            'dom.events.asyncClipboard.readText': true,
            'dom.events.testing.asyncClipboard': true
          }
        }
      }
    }
  ]
});
