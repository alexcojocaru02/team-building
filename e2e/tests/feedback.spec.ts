import { test, expect } from '@playwright/test';
import { ensureLoggedIn, E2E_USER, E2E_ADMIN } from '../helpers/auth';

// Submit button is inside the form — scope all "Send Feedback" button lookups there
// to avoid matching the "Send Feedback" tab button
const submitBtn = (page: import('@playwright/test').Page) =>
  page.locator('form button[type="submit"]:has-text("Send Feedback")');

test.describe('Feedback page', () => {
  test.beforeEach(async ({ page }) => {
    await ensureLoggedIn(page, E2E_USER);
    await page.goto('/feedback');
    await expect(page.locator('h1')).toHaveText('Feedback', { timeout: 10_000 });
  });

  test('loads with Send Feedback tab active', async ({ page }) => {
    await expect(page.locator('h2:has-text("Share feedback with a colleague")')).toBeVisible();
  });

  test('switching to Received Feedback tab works', async ({ page }) => {
    await page.locator('button:has-text("Received Feedback")').click();
    await expect(page.locator('button:has-text("Received Feedback")')).toHaveClass(/tw:border-b-2/);
    // Shows either the empty-state message or a list of feedback items
    await expect(
      page.locator('text=No feedback received yet.').or(page.locator('.tw\\:space-y-4 > div').first())
    ).toBeVisible({ timeout: 10_000 });
  });

  test('Send Feedback button is disabled when fields are empty', async ({ page }) => {
    await expect(submitBtn(page)).toBeDisabled();
  });

  test('Send Feedback button is disabled without a message', async ({ page }) => {
    const select = page.locator('select[name="toUserId"]');
    const options = await select.locator('option').all();

    if (options.length <= 1) {
      test.skip();
      return;
    }

    const firstValue = await options[1].getAttribute('value');
    await select.selectOption(firstValue!);

    await expect(submitBtn(page)).toBeDisabled();
  });

  test('can send feedback to a colleague', async ({ page }) => {
    // Ensure the E2E admin user exists so there is at least one colleague to select
    const adminPage = await page.context().newPage();
    await ensureLoggedIn(adminPage, E2E_ADMIN);
    await adminPage.close();

    await page.reload();
    await expect(page.locator('h1')).toHaveText('Feedback', { timeout: 10_000 });

    const select = page.locator('select[name="toUserId"]');
    await expect(select).toBeVisible({ timeout: 10_000 });

    const options = await select.locator('option').all();
    if (options.length <= 1) {
      test.skip();
      return;
    }

    const firstValue = await options[1].getAttribute('value');
    await select.selectOption(firstValue!);

    const message = `E2E feedback ${Date.now()}`;
    await page.locator('textarea[name="message"]').fill(message);

    const responsePromise = page.waitForResponse(res =>
      res.url().includes('/api/feedback') && res.request().method() === 'POST' && res.status() === 200
    );
    await submitBtn(page).click();
    await responsePromise;

    await expect(page.locator('text=Feedback sent successfully!')).toBeVisible({ timeout: 10_000 });
  });
});
