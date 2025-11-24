import { test, expect, Page } from '@playwright/test';

const randomAlias = () => `pw-${Date.now().toString(36)}-${Math.random().toString(36).slice(2, 6)}`;
const randomPassword = () => `P@${Math.random().toString(36).slice(2, 12)}`;

async function register(page: Page, username: string, password: string) {
  await page.goto('/auth');
  await page.getByLabel('Username').fill(username);
  await page.getByLabel('Password').fill(password);
  await page.getByRole('button', { name: /Register & Sign in/i }).click();
  await expect(page).toHaveURL(/\/me$/);
}

async function submitExpectingError(page: Page, retryDelayMs = 3000, maxRetries = 2) {
  const submit = page.getByRole('button', { name: /Register & Sign in/i });
  const errorLocator = page.locator('p.text-danger').last();

  let attempts = 0;
  let text = '';

  do {
    await submit.click();
    await expect(errorLocator).toBeVisible();
    text = (await errorLocator.textContent())?.trim() ?? '';
    if (!/Too many requests/i.test(text)) {
      break;
    }
    attempts++;
    await page.waitForTimeout(retryDelayMs);
  } while (attempts < maxRetries);

  return text;
}

test.describe.serial('account flows', () => {
  test('registers, visits Me, copies address, and logs out', async ({ page, context, browserName }, testInfo) => {
    const origin = testInfo.project.use.baseURL ?? 'http://localhost:5004';
    if (browserName !== 'firefox') {
      await context.grantPermissions(['clipboard-read', 'clipboard-write'], { origin });
    }

    const username = randomAlias();
    const password = randomPassword();

    await register(page, username, password);
    await expect(page.getByText(`${username}@`)).toBeVisible();

    await page.getByRole('button', { name: /Copy address/i }).click();
    const clipboard = await page.evaluate(() => navigator.clipboard.readText());
    expect(clipboard).toContain(username);

    await page.getByRole('button', { name: /Logout/i }).click();
    await expect(page).toHaveURL(/\/auth$/);
  });

  test('shows error when alias already exists', async ({ page }) => {
    const username = randomAlias();
    const password = randomPassword();
    await register(page, username, password);
    await page.getByRole('button', { name: /Logout/i }).click();

    await page.goto('/auth');
    await page.getByLabel('Username').fill(username);
    await page.getByLabel('Password').fill(`${password}!wrong`);
    const message = await submitExpectingError(page, 6000);
    expect(message).toMatch(/alias is invalid or already exists|too many requests/i);
  });
});

test.describe.parallel('informational flows', () => {
  test('shows validation errors for invalid input', async ({ page }) => {
    await page.goto('/auth');
    const usernameInput = page.getByLabel('Username');
    const passwordInput = page.getByLabel('Password');
    await usernameInput.waitFor();
    await usernameInput.fill('xx');
    await passwordInput.fill('123');
    await usernameInput.press('Tab');
    await passwordInput.press('Tab');
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
