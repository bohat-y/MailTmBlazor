import { test, expect } from '@playwright/test';
import { randomAlias, randomPassword, register, submitExpectingError } from './helpers';

let sharedUsername: string;

test.describe.serial('account setup', { tag: '@account-once' }, () => {
    // Limit the account-creation flow to Chromium to avoid duplicate registrations.
    test.skip(({ browserName }) => browserName !== 'chromium', 'Account setup runs only on Chromium');

    test('register a new user (runs only once)', async ({ page }) => {
        sharedUsername = randomAlias();
        const password = randomPassword();

        await register(page, sharedUsername, password);
        await expect(page.getByText(`${sharedUsername}@`)).toBeVisible();
        await page.getByRole('button', { name: /Logout/i }).click();
    });

    test('fails when alias already exists (runs only once)', async ({ page }) => {
        await page.goto('/auth');
        await page.getByLabel('Username').fill(sharedUsername);
        await page.getByLabel('Password').fill('wrongpassword');

        const message = await submitExpectingError(page);
        expect(message).toMatch(/alias is invalid or already exists|too many requests/i);
    });
});
