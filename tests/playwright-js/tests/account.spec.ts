import { test, expect } from '@playwright/test';
import { randomAlias, randomPassword, register, submitExpectingError } from './helpers';

let sharedUsername: string;

test.describe.serial('account setup', { tag: '@account-once' }, () => {
    // Limit the account-creation flow to Chromium to avoid duplicate registrations.
    test.skip(({ browserName }) => browserName !== 'chromium', 'Account setup runs only on Chromium');

    test('reuses seeded account (runs only once)', async ({ page }) => {
        const meta = require('../.auth/user.json');
        sharedUsername = meta.alias;

        await page.goto('/me');
        await expect(page.getByText(`${sharedUsername}@`)).toBeVisible();
        await page.getByRole('button', { name: /Logout/i }).click();
    });

    test('fails when alias already exists (runs only once)', async ({ page }) => {
        const meta = require('../.auth/user.json');
        await page.goto('/auth');
        await page.getByLabel('Username').fill(meta.alias);
        await page.getByLabel('Password').fill(meta.password);

        const message = await submitExpectingError(page);
        expect(message).toMatch(/alias is invalid or already exists|too many requests/i);
    });
});
