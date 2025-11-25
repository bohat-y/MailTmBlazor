import { expect, Page } from '@playwright/test';

export const randomAlias = () => `pw-${Date.now().toString(36)}-${Math.random().toString(36).slice(2, 6)}`;
export const randomPassword = () => `P@${Math.random().toString(36).slice(2, 12)}!`;

export async function register(page: Page, username: string, password: string) {
    await page.goto('/auth');
    await page.getByLabel('Username').fill(username);
    await page.getByLabel('Password').fill(password);
    await page.getByRole('button', { name: /Register & Sign in/i }).click();

    // Wait until the app has persisted the account or navigated to a logged-in page.
    let success = false;
    for (let i = 0; i < 15; i++) {
        const url = page.url();
        if (/\/(me|inbox)$/i.test(url)) {
            success = true;
            break;
        }
        const hasAccount = await page.evaluate(() => !!localStorage.getItem('account'));
        if (hasAccount) {
            success = true;
            break;
        }
        await page.waitForTimeout(1000);
    }

    expect(success).toBeTruthy();
}

export async function submitExpectingError(page: Page) {
    const button = page.getByRole('button', { name: /Register & Sign in/i });
    const error = page.locator('p.text-danger').last();

    await button.click();
    await expect(error).toBeVisible({ timeout: 10_000 });
    return (await error.textContent())?.trim() ?? '';
}
