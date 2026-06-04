import { test, expect } from '@playwright/test';
import { ensureLoggedIn, E2E_USER } from '../helpers/auth';

test.describe('Home page', () => {
  test.beforeEach(async ({ page }) => {
    await ensureLoggedIn(page, E2E_USER);
    await page.goto('/home');
    await expect(page.locator('h1')).toContainText('TeamConnect', { timeout: 10_000 });
  });

  test('shows all main navigation cards', async ({ page }) => {
    // Use exact text to avoid "Feed" matching inside "Feedback"
    await expect(page.getByRole('heading', { name: 'Feed', exact: true })).toBeVisible();
    await expect(page.getByRole('heading', { name: 'Feedback', exact: true })).toBeVisible();
    await expect(page.getByRole('heading', { name: 'Cohesion Dashboard', exact: true })).toBeVisible();
  });

  test('Feed card navigates to /feed', async ({ page }) => {
    await page.locator('a:has(h2:text("Feed"))').first().click();
    await expect(page).toHaveURL(/\/feed/, { timeout: 10_000 });
  });

  test('Feedback card navigates to /teams', async ({ page }) => {
    await page.locator('a:has(h2:text("Feedback"))').first().click();
    await expect(page).toHaveURL(/\/teams/, { timeout: 10_000 });
  });

  test('logout button navigates to /login', async ({ page }) => {
    await page.locator('button:has-text("Logout")').click();
    await expect(page).toHaveURL(/\/login/, { timeout: 10_000 });
  });
});
