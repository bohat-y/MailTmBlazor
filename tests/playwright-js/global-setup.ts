import fs from 'fs';
import path from 'path';
import { chromium } from '@playwright/test';

const authDir = path.join(__dirname, '.auth');
const authStateFile = path.join(authDir, 'state.json');
const authMetaFile = path.join(authDir, 'user-meta.json');
const baseURL = process.env.PLAYWRIGHT_BASE_URL ?? 'http://localhost:5004';

const randomAlias = () => `pw-${Date.now().toString(36)}-${Math.random().toString(36).slice(2, 6)}`;
const randomPassword = () => `P@${Math.random().toString(36).slice(2, 12)}!`;

async function createUser() {
    const browser = await chromium.launch({ headless: true });
    const page = await browser.newPage({ baseURL });
    const alias = randomAlias();
    const password = randomPassword();

    await page.goto('/auth');
    await page.getByLabel('Username').fill(alias);
    await page.getByLabel('Password').fill(password);
    await page.getByRole('button', { name: /Register & Sign in/i }).click();
    await page.waitForURL(/\/me$/);

    fs.mkdirSync(authDir, { recursive: true });
    await page.context().storageState({ path: authStateFile });
    fs.writeFileSync(authMetaFile, JSON.stringify({ alias, password }, null, 2));

    await browser.close();
}

export default async function globalSetup() {
    await createUser();
}
