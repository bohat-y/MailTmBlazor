import { test, expect } from '@playwright/test';

test('copies address to clipboard in both browsers', { tag: '@clipboard' }, async ({ page }) => {
    // Capture what the app attempts to write so we can assert without needing clipboard permissions.
    await page.addInitScript(() => {
        const original = navigator.clipboard.writeText.bind(navigator.clipboard);
        (window as any).__pwCopied = null;
        navigator.clipboard.writeText = async (text: string) => {
            (window as any).__pwCopied = text;
            return original(text);
        };
    });

    await page.goto('/me');
    const emailPill = page.locator('button.email-pill').first();
    await expect(emailPill).toBeVisible({ timeout: 15_000 });

    const email = (await emailPill.innerText()).trim();
    await emailPill.click();

    const clipboardWrite = await page.evaluate(() => (window as any).__pwCopied);
    expect(clipboardWrite).toContain(email.split('@')[0]);
});
