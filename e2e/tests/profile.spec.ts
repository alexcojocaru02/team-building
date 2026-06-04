import { test, expect } from '@playwright/test';
import { ensureLoggedIn, E2E_USER } from '../helpers/auth';

test.describe('Profile page', () => {
  test.beforeEach(async ({ page }) => {
    await ensureLoggedIn(page, E2E_USER);
    await page.goto('/profile');
    await expect(page.locator('h1')).toHaveText('My Profile', { timeout: 10_000 });
  });

  test('shows user name and email', async ({ page }) => {
    await expect(page.locator(`text=${E2E_USER.fullName}`)).toBeVisible({ timeout: 10_000 });
    await expect(page.locator(`text=${E2E_USER.email}`)).toBeVisible();
  });

  test('Edit Profile button navigates to /profile/edit', async ({ page }) => {
    await page.locator('button:has-text("Edit Profile")').click();
    await expect(page).toHaveURL(/\/profile\/edit/, { timeout: 10_000 });
  });

  test('edit profile form shows name and email as read-only', async ({ page }) => {
    await page.locator('button:has-text("Edit Profile")').click();
    await expect(page).toHaveURL(/\/profile\/edit/, { timeout: 10_000 });

    const nameInput = page.locator('input[name="fullName"]');
    const emailInput = page.locator('input[name="email"]');

    await expect(nameInput).toBeVisible();
    await expect(emailInput).toBeVisible();
    await expect(nameInput).toBeDisabled();
    await expect(emailInput).toBeDisabled();
  });

  test('Cancel on edit profile returns to profile view', async ({ page }) => {
    await page.locator('button:has-text("Edit Profile")').click();
    await expect(page).toHaveURL(/\/profile\/edit/, { timeout: 10_000 });

    await page.locator('button:has-text("Cancel")').click();
    await expect(page).toHaveURL(/\/profile$/, { timeout: 10_000 });
  });

  test('can save bio and see it on profile page', async ({ page }) => {
    await page.locator('button:has-text("Edit Profile")').click();
    await expect(page).toHaveURL(/\/profile\/edit/, { timeout: 10_000 });

    const bio = `E2E bio ${Date.now()}`;
    await page.fill('textarea[name="bio"]', bio);

    const saveResponse = page.waitForResponse(res =>
      res.url().includes('/api/users') && res.request().method() === 'PUT',
      { timeout: 15_000 }
    );
    await page.locator('button:has-text("Save Changes")').click();
    await saveResponse;

    await expect(page.locator('text=Profile updated successfully!')).toBeVisible({ timeout: 10_000 });

    await page.goto('/profile');
    await expect(page.locator(`text=${bio}`)).toBeVisible({ timeout: 10_000 });
  });
});
