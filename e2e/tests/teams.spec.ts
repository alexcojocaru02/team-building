import { test, expect } from '@playwright/test';
import { ensureLoggedIn, E2E_USER } from '../helpers/auth';

test.describe('Teams', () => {
  test.beforeEach(async ({ page }) => {
    await ensureLoggedIn(page, E2E_USER);
  });

  test('pagina de teams se incarca dupa login', async ({ page }) => {
    await page.goto('/teams');
    await expect(page.locator('h1')).toHaveText('Teams', { timeout: 10000 });
    await expect(page.locator('text=Loading teams...')).not.toBeVisible({ timeout: 10000 });
  });

  test('afiseaza teams sau mesaj gol fara eroare', async ({ page }) => {
    await page.goto('/teams');
    await expect(page.locator('text=Loading teams...')).not.toBeVisible({ timeout: 10000 });
    await expect(page.locator('text=Failed to load teams')).not.toBeVisible();
  });

  test('navigarea catre teams din sidebar functioneaza', async ({ page }) => {
    await page.goto('/home');
    await page.click('a[href="/teams"], a:has-text("Teams")');
    await expect(page).toHaveURL(/\/teams/);
    await expect(page.locator('h1')).toHaveText('Teams', { timeout: 10000 });
  });
});
