import fs from 'fs';
import path from 'path';
import { test, expect } from '@playwright/test';

const authDir = path.join(__dirname, '..', '.auth');
const authStateFile = path.join(authDir, 'state.json');
const authMetaFile = path.join(authDir, 'user.json');
const { alias } = JSON.parse(fs.readFileSync(authMetaFile, 'utf-8'));

test.use({ storageState: authStateFile });

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
    await page.getByRole('button', { name: /Copy address/i }).click();

    const clipboardWrite = await page.evaluate(() => (window as any).__pwCopied);
    expect(clipboardWrite).toContain(alias);
});
