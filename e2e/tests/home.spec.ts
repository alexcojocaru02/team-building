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
    // Target the card link specifically (has routerlink attribute), not the sidebar link
    await page.locator('a[routerlink="/feed"]').click();
    await expect(page).toHaveURL(/\/feed/, { timeout: 10_000 });
  });

  test('Feedback card navigates to /feedback', async ({ page }) => {
    await page.locator('a[routerlink="/feedback"]').click();
    await expect(page).toHaveURL(/\/feedback/, { timeout: 10_000 });
  });

  test('logout button navigates to /login', async ({ page }) => {
    await page.locator('button:has-text("Logout")').click();
    await expect(page).toHaveURL(/\/login/, { timeout: 10_000 });
  });
});
