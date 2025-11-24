import { test, expect } from '@playwright/test';

test.describe.parallel('informational flows', () => {
    test('shows validation errors for invalid input', async ({ page }) => {
        await page.goto('/auth');
        await page.getByLabel('Username').fill('xx');
        await page.getByLabel('Password').fill('123');
        await page.getByLabel('Username').press('Tab');
        await expect(page.getByText(/letters, numbers, dashes, or underscore/i)).toBeVisible();
        await expect(page.getByText(/at least 6 characters/i)).toBeVisible();
    });

    test('domains page lists available domains', async ({ page }) => {
        await page.goto('/domains');
        await expect(page.locator('ul li')).not.toHaveCount(0);
    });

    test('redirects unauthenticated users from /inbox', async ({ page }) => {
        await page.goto('/inbox');
        await expect(page).toHaveURL(/\/auth$/);
    });
});
