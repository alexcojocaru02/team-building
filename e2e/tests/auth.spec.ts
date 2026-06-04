import { test, expect } from '@playwright/test';
import { ensureLoggedIn, E2E_USER } from '../helpers/auth';

test.describe('Authentication', () => {
  test('redirects to /login when not authenticated', async ({ page }) => {
    await page.goto('/home');
    await expect(page).toHaveURL(/\/login/);
  });

  test('redirects to /login for any protected route', async ({ page }) => {
    await page.goto('/teams');
    await expect(page).toHaveURL(/\/login/);

    await page.goto('/feed');
    await expect(page).toHaveURL(/\/login/);
  });

  test('login with valid credentials redirects to home', async ({ page }) => {
    await ensureLoggedIn(page, E2E_USER);
    await expect(page).toHaveURL(/\/home/);
  });

  test('login with wrong password shows error', async ({ page }) => {
    await page.goto('/login');
    await page.fill('#login-email', 'nonexistent@test.com');
    await page.fill('#login-password', 'WrongPassword123!');
    await page.click('button[type="submit"]');

    await expect(page.locator('text=Invalid email or password')).toBeVisible({ timeout: 5000 });
  });

  test('register with already existing email shows error', async ({ page }) => {
    await ensureLoggedIn(page, E2E_USER);
    await page.goto('/login');

    await page.locator('p button:has-text("Sign Up")').click();
    await page.fill('#register-full-name', E2E_USER.fullName);
    await page.fill('#register-email', E2E_USER.email);
    await page.fill('#register-password', E2E_USER.password);
    await page.click('button[type="submit"]');

    await expect(page.locator('.tw\\:text-red-600')).toBeVisible({ timeout: 5000 });
  });

  test('toggle between login and register forms', async ({ page }) => {
    await page.goto('/login');

    await expect(page.locator('h1')).toHaveText('Welcome Back');
    await page.locator('p button:has-text("Sign Up")').click();
    await expect(page.locator('h1')).toHaveText('Create Account');
    await page.locator('p button:has-text("Sign In")').click();
    await expect(page.locator('h1')).toHaveText('Welcome Back');
  });
});
