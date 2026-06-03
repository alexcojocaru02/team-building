import { test, expect } from '@playwright/test';
import { ensureLoggedIn, E2E_USER } from '../helpers/auth';

test.describe('Teams', () => {
  test.beforeEach(async ({ page }) => {
    await ensureLoggedIn(page, E2E_USER);
  });

  test('teams page loads after login', async ({ page }) => {
    await page.goto('/teams');
    await expect(page.locator('h1')).toHaveText('Teams', { timeout: 10000 });
    await expect(page.locator('text=Loading teams...')).not.toBeVisible({ timeout: 10000 });
  });

  test('shows teams list or empty state without errors', async ({ page }) => {
    await page.goto('/teams');
    await expect(page.locator('text=Loading teams...')).not.toBeVisible({ timeout: 10000 });
    await expect(page.locator('text=Failed to load teams')).not.toBeVisible();
  });

  test('navigating to teams from sidebar works', async ({ page }) => {
    await page.goto('/home');
    await page.click('a[href="/teams"], a:has-text("Teams")');
    await expect(page).toHaveURL(/\/teams/);
    await expect(page.locator('h1')).toHaveText('Teams', { timeout: 10000 });
  });
});
