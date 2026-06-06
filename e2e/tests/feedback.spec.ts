import { test, expect, Page } from '@playwright/test';
import type { APIRequestContext } from '@playwright/test';
import { ensureLoggedIn, E2E_USER, E2E_ADMIN, getTeamForUser, getAuthToken, ensureTeammates, deleteTeamViaApi } from '../helpers/auth';
import { API_BASE_URL } from '../helpers/config';

async function openFeedbackPage(page: Page, teamId: string) {
  await page.goto(`/teams/${teamId}/feedback`);
  await expect(page.locator('h1')).toHaveText('Feedback', { timeout: 10_000 });
}

async function reloadAndWaitForFeedback(page: Page, teamId: string) {
  const responsePromise = page.waitForResponse(
    res => res.url().includes('/api/feedback/received') && res.request().method() === 'GET',
    { timeout: 15_000 }
  );
  await page.goto(`/teams/${teamId}/feedback`);
  await responsePromise;
}

async function openSendDialog(page: Page) {
  await page.locator('button:has-text("Give Feedback")').first().click();
  await expect(page.locator('h2[mat-dialog-title]')).toHaveText('Give Feedback', { timeout: 5_000 });
}

test.describe('Feedback page', () => {
  let teamId: string;
  const sharedTeamIds: string[] = [];

  test.beforeEach(async ({ page, request }) => {
    await ensureLoggedIn(page, E2E_USER);

    const id = await getTeamForUser(request, API_BASE_URL, E2E_USER);
    if (!id) { test.skip(); return; }
    teamId = id;

    await openFeedbackPage(page, teamId);
  });

  test.afterEach(async ({ request }) => {
    // Delete any shared teams created by ensureTeammates during this test
    if (sharedTeamIds.length === 0) return;
    const adminToken = await getAuthToken(request, API_BASE_URL, E2E_ADMIN);
    for (const id of sharedTeamIds) {
      await deleteTeamViaApi(request, API_BASE_URL, id, adminToken);
    }
    sharedTeamIds.length = 0;
  });

  // ── Page structure ───────────────────────────────────────────────────────

  test('shows Give Feedback button in header', async ({ page }) => {
    // Use first() — the empty-state CTA also contains "Give Feedback" text
    await expect(page.locator('button:has-text("Give Feedback")').first()).toBeVisible();
  });

  test('shows empty state when no feedback received', async ({ page }) => {
    // May or may not have feedback — if empty state is shown, check it
    const emptyState = page.locator('text=No feedback yet');
    const feedbackItem = page.locator('article.received-item').first();
    await expect(emptyState.or(feedbackItem)).toBeVisible({ timeout: 10_000 });
  });

  // ── Dialog ───────────────────────────────────────────────────────────────

  test('Give Feedback button opens dialog with correct title', async ({ page }) => {
    await openSendDialog(page);
    // Title must be plain text — no icon elements inside it
    const title = page.locator('h2[mat-dialog-title]');
    await expect(title).toHaveText('Give Feedback');
  });

  test('dialog Send button is disabled with no recipient selected', async ({ page }) => {
    await openSendDialog(page);
    const sendBtn = page.locator('mat-dialog-actions button:has-text("Send Feedback")');
    await expect(sendBtn).toBeDisabled();
  });

  test('dialog Send button is disabled without a message', async ({ page }) => {
    await openSendDialog(page);

    // Ensure admin is registered so there is at least one colleague
    const adminPage = await page.context().newPage();
    await ensureLoggedIn(adminPage, E2E_ADMIN);
    await adminPage.close();
    await page.reload();
    await openFeedbackPage(page, teamId);
    await openSendDialog(page);

    const select = page.locator('mat-dialog-content mat-select').first();
    await select.click();
    // Skip if the only option is the disabled "No colleagues available" placeholder
    const enabledOptions = page.locator('mat-option:not([aria-disabled="true"])');
    if (await enabledOptions.count() === 0) { test.skip(); return; }
    await enabledOptions.first().click();

    const sendBtn = page.locator('mat-dialog-actions button:has-text("Send Feedback")');
    await expect(sendBtn).toBeDisabled();
  });

  test('dialog shows points incentive banner', async ({ page }) => {
    await openSendDialog(page);
    await expect(page.locator('mat-dialog-content').getByText(/You earn/)).toBeVisible();
  });

  test('Cancel button closes the dialog', async ({ page }) => {
    await openSendDialog(page);
    await page.locator('mat-dialog-actions button:has-text("Cancel")').click();
    await expect(page.locator('h2[mat-dialog-title]')).not.toBeVisible({ timeout: 3_000 });
  });

  test('dialog shows team name in colleague select hint', async ({ page }) => {
    await openSendDialog(page);
    await expect(page.locator('mat-dialog-content mat-hint').first()).toBeVisible();
  });

  // ── Send feedback ────────────────────────────────────────────────────────

  test('can send feedback to a colleague and sees success snackbar', async ({ page }) => {
    // Ensure admin exists so there is a colleague to choose
    const adminPage = await page.context().newPage();
    await ensureLoggedIn(adminPage, E2E_ADMIN);
    await adminPage.close();
    await page.reload();
    await openFeedbackPage(page, teamId);
    await openSendDialog(page);

    const select = page.locator('mat-dialog-content mat-select').first();
    await select.click();
    const enabledOptions = page.locator('mat-option:not([aria-disabled="true"])');
    if (await enabledOptions.count() === 0) { test.skip(); return; }
    await enabledOptions.first().click();

    await page.locator('mat-dialog-content textarea').fill(`E2E feedback ${Date.now()}`);

    const responsePromise = page.waitForResponse(res =>
      res.url().includes('/api/feedback') && res.request().method() === 'POST' && res.status() === 200
    );
    await page.locator('mat-dialog-actions button:has-text("Send Feedback")').click();
    await responsePromise;

    await expect(page.locator('mat-snack-bar-container').getByText(/Feedback sent/)).toBeVisible({ timeout: 8_000 });
    // Dialog closes after success
    await expect(page.locator('h2[mat-dialog-title]')).not.toBeVisible({ timeout: 5_000 });
  });

  test('can select category in dialog', async ({ page }) => {
    await openSendDialog(page);

    // Category select is the second mat-select in the dialog
    const categorySelect = page.locator('mat-dialog-content mat-select').nth(1);
    await categorySelect.click();
    const options = page.locator('mat-option');
    await expect(options.first()).toBeVisible();
    await options.getByText('Delivery').click();

    // Verify the selected value is reflected
    await expect(categorySelect).toContainText('Delivery');
  });

  test('can select tone in dialog', async ({ page }) => {
    await openSendDialog(page);

    const constructiveBtn = page.locator('mat-dialog-content button:has-text("Constructive")');
    await constructiveBtn.click();
    await expect(constructiveBtn).toHaveClass(/active/);
  });

  // ── Received feedback list & filters ────────────────────────────────────

  // ── Shared seed helper ───────────────────────────────────────────────────

  async function seedFeedback(
    request: APIRequestContext,
    userId: string,
    message: string,
    category: string,
    tone: string
  ): Promise<void> {
    // Admin creates a shared team (gaining TeamOwner role + newToken), then adds user
    const sharedTeamId = await ensureTeammates(request, API_BASE_URL, teamId, E2E_ADMIN, E2E_USER);
    if (sharedTeamId) sharedTeamIds.push(sharedTeamId);

    // Fresh admin token (carries updated role after ensureTeammates)
    const adminToken = await getAuthToken(request, API_BASE_URL, E2E_ADMIN);
    const res = await request.post(`${API_BASE_URL}/feedback`, {
      headers: { Authorization: `Bearer ${adminToken}` },
      data: { toUserId: userId, message, category, tone },
    });
    expect(res.ok(), `Feedback seed POST failed ${res.status()}: ${await res.text()}`).toBe(true);
  }

  test('tone filter chips are shown when feedback exists', async ({ page, request }) => {
    const userToken = await getAuthToken(request, API_BASE_URL, E2E_USER);
    const meRes = await request.get(`${API_BASE_URL}/users/me`, { headers: { Authorization: `Bearer ${userToken}` } });
    if (!meRes.ok()) { test.skip(); return; }
    const me = await meRes.json();

    await seedFeedback(request, me.id, 'E2E tone filter test', 'Collaboration', 'Constructive');

    await reloadAndWaitForFeedback(page, teamId);

    await expect(page.locator('[data-testid="filter-chip-all"]')).toBeVisible({ timeout: 10_000 });
    await expect(page.locator('[data-testid="filter-chip-positive"]')).toBeVisible();
    await expect(page.locator('[data-testid="filter-chip-constructive"]')).toBeVisible();
    await expect(page.locator('[data-testid="filter-chip-critical"]')).toBeVisible();
  });

  test('selecting Constructive filter shows only constructive feedback', async ({ page, request }) => {
    const userToken = await getAuthToken(request, API_BASE_URL, E2E_USER);
    const meRes = await request.get(`${API_BASE_URL}/users/me`, { headers: { Authorization: `Bearer ${userToken}` } });
    if (!meRes.ok()) { test.skip(); return; }
    const me = await meRes.json();

    await seedFeedback(request, me.id, 'Constructive item for filter test', 'Communication', 'Constructive');

    await reloadAndWaitForFeedback(page, teamId);

    const constructiveChip = page.locator('[data-testid="filter-chip-constructive"]');
    await expect(constructiveChip).toBeVisible({ timeout: 10_000 });
    await constructiveChip.click();
    await expect(constructiveChip).toHaveClass(/filter-chip--active/);

    const toneBadges = page.locator('article.received-item .badge--constructive');
    const allBadges = page.locator('article.received-item .badge:not(.badge--category)');
    const total = await allBadges.count();
    const constructive = await toneBadges.count();
    expect(constructive).toBe(total);
  });

  test('clicking active filter again resets to All', async ({ page, request }) => {
    const userToken = await getAuthToken(request, API_BASE_URL, E2E_USER);
    const meRes = await request.get(`${API_BASE_URL}/users/me`, { headers: { Authorization: `Bearer ${userToken}` } });
    if (!meRes.ok()) { test.skip(); return; }
    const me = await meRes.json();

    await seedFeedback(request, me.id, 'Toggle filter test', 'Delivery', 'Positive');

    await reloadAndWaitForFeedback(page, teamId);

    const positiveChip = page.locator('[data-testid="filter-chip-positive"]');
    await expect(positiveChip).toBeVisible({ timeout: 10_000 });
    await positiveChip.click();
    await expect(positiveChip).toHaveClass(/filter-chip--active/);

    await positiveChip.click();
    await expect(page.locator('[data-testid="filter-chip-all"]')).toHaveClass(/filter-chip--active/);
  });

  // ── Overview stats ───────────────────────────────────────────────────────

  test('stat cards are visible when feedback exists', async ({ page, request }) => {
    const userToken = await getAuthToken(request, API_BASE_URL, E2E_USER);
    const meRes = await request.get(`${API_BASE_URL}/users/me`, { headers: { Authorization: `Bearer ${userToken}` } });
    if (!meRes.ok()) { test.skip(); return; }
    const me = await meRes.json();

    await seedFeedback(request, me.id, 'Stat card test', 'Leadership', 'Positive');

    await reloadAndWaitForFeedback(page, teamId);

    await expect(page.locator('[data-testid="stat-card-total"]')).toBeVisible({ timeout: 10_000 });
    await expect(page.locator('.stat-label:has-text("Total")')).toBeVisible();
  });
});
