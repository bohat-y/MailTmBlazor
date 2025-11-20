import { test, expect } from '@playwright/test';

const headingSelector = 'h1';

test('home page renders hero content', async ({ page }) => {
  await page.goto('/');
  await expect(page).toHaveTitle(/MailTmBlazor/i);
  await expect(page.locator(headingSelector)).toContainText('Hello, world!');
});
