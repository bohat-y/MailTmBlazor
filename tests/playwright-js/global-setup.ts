import fs from 'fs';
import path from 'path';
import { chromium, FullConfig } from '@playwright/test';

const authDir = path.join(__dirname, '.auth');
const statePath = path.join(authDir, 'state.json');
const metaPath = path.join(authDir, 'user.json');
const baseURL = process.env.PLAYWRIGHT_BASE_URL ?? 'http://localhost:5004';

const randomAlias = () => `pw-${Date.now().toString(36)}-${Math.random().toString(36).slice(2, 6)}`;
const randomPassword = () => `P@${Math.random().toString(36).slice(2, 12)}!`;

async function ensureAccount() {
    const browser = await chromium.launch({ headless: true });
    const page = await browser.newPage({ baseURL });

    const alias = randomAlias();
    const password = randomPassword();

    await page.goto('/auth');
    await page.getByLabel('Username').fill(alias);
    await page.getByLabel('Password').fill(password);
    await page.getByRole('button', { name: /Register & Sign in/i }).click();

    let success = false;
    for (let i = 0; i < 20; i++) {
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

    if (!success) {
        throw new Error('Failed to create/login seeded account');
    }

    fs.mkdirSync(authDir, { recursive: true });
    await page.context().storageState({ path: statePath });
    fs.writeFileSync(metaPath, JSON.stringify({ alias, password }, null, 2), 'utf-8');

    await browser.close();
}

async function globalSetup(config: FullConfig) {
    const exists = fs.existsSync(statePath) && fs.existsSync(metaPath);
    if (!exists) {
        await ensureAccount();
    }
}

export default globalSetup;
