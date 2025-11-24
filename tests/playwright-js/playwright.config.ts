import { defineConfig, devices } from '@playwright/test';
import 'dotenv/config';

const baseURL = process.env.PLAYWRIGHT_BASE_URL ?? 'http://localhost:5004';

export default defineConfig({
    testDir: './tests',
    timeout: 30_000,
    fullyParallel: true,
    retries: process.env.CI ? 2 : 0,
    globalSetup: './global-setup.ts',

    use: {
        baseURL,
        trace: 'on-first-retry',
        screenshot: 'only-on-failure',
    },

    projects: [
        {
            name: 'chromium',
            use: { ...devices['Desktop Chrome'] },
        },
        {
            name: 'firefox',
            use: {
                ...devices['Desktop Firefox'],
                launchOptions: {
                    firefoxUserPrefs: {
                        'dom.events.asyncClipboard.readText': true,
                        'dom.events.testing.asyncClipboard': true,
                    },
                },
            },
        },
    ],
});
