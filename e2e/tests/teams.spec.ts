import { test, expect } from '@playwright/test';
import { ensureLoggedIn, E2E_USER, getAuthToken, deleteTeamViaApi } from '../helpers/auth';
import { API_BASE_URL } from '../helpers/config';

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

  test.describe('create team', () => {
    const teamName = `E2E Team ${Date.now()}`;
    let createdTeamId: string | null = null;

    test.afterAll(async ({ request }) => {
      if (!createdTeamId) return;
      const token = await getAuthToken(request, API_BASE_URL, E2E_USER);
      await deleteTeamViaApi(request, API_BASE_URL, createdTeamId, token);
    });

    test('can create a team', async ({ page, request }) => {
      await page.goto('/teams');
      await expect(page.locator('text=Loading teams...')).not.toBeVisible({ timeout: 10000 });

      await page.click('button:has-text("Create Team")');

      const dialog = page.locator('mat-dialog-container, [role="dialog"]');
      await expect(dialog).toBeVisible({ timeout: 5000 });

      await dialog.locator('#team-name').fill(teamName);
      await dialog.locator('button:has-text("Create team")').click();

      await expect(page.locator(`h2:has-text("${teamName}")`)).toBeVisible({ timeout: 10000 });

      const token = await getAuthToken(request, API_BASE_URL, E2E_USER);
      const teamsRes = await request.get(`${API_BASE_URL}/teams`, {
        headers: { Authorization: `Bearer ${token}` },
      });
      const teams = await teamsRes.json();
      const created = (teams as any[]).find((t: any) => t.name === teamName);
      if (created) createdTeamId = created.id;
    });
  });
});
