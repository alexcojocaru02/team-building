import { test, expect } from '@playwright/test';
import {
  ensureLoggedIn,
  getAuthToken,
  createTeamViaApi,
  deleteTeamViaApi,
  requestJoinTeamViaApi,
  E2E_USER,
  E2E_ADMIN,
} from '../helpers/auth';
import { API_BASE_URL } from '../helpers/config';

const E2E_SECOND_USER = {
  fullName: 'E2E Second User',
  email: 'e2e-second@teamconnect.test',
  password: 'E2eSecond@1234!',
};

test.describe('Team creation by any user', () => {
  let adminToken: string;
  const createdTeamIds: string[] = [];

  test.beforeAll(async ({ request }) => {
    adminToken = await getAuthToken(request, API_BASE_URL, E2E_ADMIN);
  });

  test.afterAll(async ({ request }) => {
    for (const teamId of createdTeamIds) {
      await deleteTeamViaApi(request, API_BASE_URL, teamId, adminToken);
    }
  });

  test('Create Team button is visible for a regular user', async ({ page }) => {
    await ensureLoggedIn(page, E2E_USER);
    await page.goto('/teams');
    await expect(page.locator('text=Loading teams...')).not.toBeVisible({ timeout: 10000 });
    await expect(page.getByRole('button', { name: /create team/i })).toBeVisible();
  });

  test('regular user can create a team and it appears in the list', async ({ page }) => {
    await ensureLoggedIn(page, E2E_USER);
    await page.goto('/teams');
    await expect(page.locator('text=Loading teams...')).not.toBeVisible({ timeout: 10000 });

    const teamName = `E2E Team ${Date.now()}`;
    await page.getByRole('button', { name: /create team/i }).click();

    // Fill the dialog
    await page.locator('input[placeholder*="name"], mat-dialog-container input').first().fill(teamName);

    const responsePromise = page.waitForResponse(res =>
      res.url().includes('/api/teams') && res.request().method() === 'POST' && res.status() === 200
    );
    await page.locator('mat-dialog-container button:has-text("Create team")').click();

    const response = await responsePromise;
    const body = await response.json();
    if (body.team?.id) createdTeamIds.push(body.team.id);

    await expect(page.getByRole('heading', { name: teamName })).toBeVisible({ timeout: 10000 });
  });

  test('user is shown as Owner of their newly created team', async ({ page }) => {
    await ensureLoggedIn(page, E2E_USER);
    await page.goto('/teams');
    await expect(page.locator('text=Loading teams...')).not.toBeVisible({ timeout: 10000 });

    const teamName = `E2E Owner Check ${Date.now()}`;
    await page.getByRole('button', { name: /create team/i }).click();
    await page.locator('input[placeholder*="name"], mat-dialog-container input').first().fill(teamName);

    const responsePromise = page.waitForResponse(res =>
      res.url().includes('/api/teams') && res.request().method() === 'POST' && res.status() === 200
    );
    await page.locator('mat-dialog-container button:has-text("Create team")').click();

    const response = await responsePromise;
    const body = await response.json();
    if (body.team?.id) createdTeamIds.push(body.team.id);

    // Owner pill should be visible on the new team card
    const teamCard = page.locator('.tw\\:rounded-lg').filter({ has: page.getByRole('heading', { name: teamName }) });
    await expect(teamCard.locator('text=Owner').first()).toBeVisible({ timeout: 10000 });
  });
});

test.describe('Request to join a team', () => {
  let ownerToken: string;
  let requesterToken: string;
  let teamId: string;
  let teamName: string;

  test.beforeAll(async ({ request }) => {
    ownerToken = await getAuthToken(request, API_BASE_URL, E2E_USER);
    requesterToken = await getAuthToken(request, API_BASE_URL, E2E_SECOND_USER);

    // Create a team that the second user is NOT a member of
    teamName = `Join Test Team ${Date.now()}`;
    const created = await createTeamViaApi(request, API_BASE_URL, teamName, ownerToken);
    teamId = created.teamId;
    // Refresh token if role upgraded
    if (created.newToken) ownerToken = created.newToken;
  });

  test.afterAll(async ({ request }) => {
    // Use admin token for cleanup since owner role may have changed
    const adminToken = await getAuthToken(request, API_BASE_URL, E2E_ADMIN);
    if (teamId) await deleteTeamViaApi(request, API_BASE_URL, teamId, adminToken);
  });

  test('non-member sees Request to Join button', async ({ page }) => {
    await ensureLoggedIn(page, E2E_SECOND_USER);
    await page.goto('/teams');
    await expect(page.locator('text=Loading teams...')).not.toBeVisible({ timeout: 10000 });

    const teamCard = page.locator('.tw\\:rounded-lg').filter({ has: page.getByRole('heading', { name: teamName, exact: true }) });
    await expect(teamCard.getByRole('button', { name: /request to join/i })).toBeVisible({ timeout: 10000 });
  });

  test('clicking Request to Join sends request and shows pending state', async ({ page }) => {
    await ensureLoggedIn(page, E2E_SECOND_USER);
    await page.goto('/teams');
    await expect(page.locator('text=Loading teams...')).not.toBeVisible({ timeout: 10000 });

    // Find the specific team card and click its Request to Join button
    const teamCard = page.locator('.tw\\:rounded-lg').filter({ has: page.getByRole('heading', { name: teamName, exact: true }) });
    await expect(teamCard.getByRole('button', { name: /request to join/i })).toBeVisible({ timeout: 10000 });

    const joinResponsePromise = page.waitForResponse(
      res => res.url().includes('/join-request') && res.request().method() === 'POST',
      { timeout: 10000 }
    );
    await teamCard.getByRole('button', { name: /request to join/i }).click();
    await joinResponsePromise;

    // Request to Join button should be gone (replaced by Request Pending)
    await expect(teamCard.getByRole('button', { name: /request to join/i })).not.toBeVisible({ timeout: 10000 });
  });

  test('member does not see Request to Join button for their own team', async ({ page }) => {
    await ensureLoggedIn(page, E2E_USER);
    await page.goto('/teams');
    await expect(page.locator('text=Loading teams...')).not.toBeVisible({ timeout: 10000 });

    // The owner should not see "Request to Join" on any team they own/are member of
    // Find their team card — it should show Edit/Manage Members, not Request to Join
    const ownedCard = page.locator('.tw\\:bg-white').filter({ has: page.locator('text=Owner') }).first();
    await expect(ownedCard.getByRole('button', { name: /request to join/i })).not.toBeVisible();
  });
});

test.describe('Admin approves/rejects join requests', () => {
  let adminToken: string;
  let requesterToken: string;
  let teamId: string;
  let teamName: string;

  test.beforeAll(async ({ request }) => {
    adminToken = await getAuthToken(request, API_BASE_URL, E2E_ADMIN);
    requesterToken = await getAuthToken(request, API_BASE_URL, E2E_SECOND_USER);

    // Admin creates a team for this test group
    teamName = `Admin Approve Team ${Date.now()}`;
    const created = await createTeamViaApi(request, API_BASE_URL, teamName, adminToken);
    teamId = created.teamId;
    if (created.newToken) adminToken = created.newToken;

    // Second user requests to join
    await requestJoinTeamViaApi(request, API_BASE_URL, teamId, requesterToken);
  });

  test.afterAll(async ({ request }) => {
    const token = await getAuthToken(request, API_BASE_URL, E2E_ADMIN);
    if (teamId) await deleteTeamViaApi(request, API_BASE_URL, teamId, token);
  });

  test('admin sees pending join requests in /admin page', async ({ page }) => {
    await ensureLoggedIn(page, E2E_ADMIN);
    await page.goto('/admin');
    await expect(page.locator('h1')).toHaveText('Admin Dashboard', { timeout: 10000 });

    await expect(page.locator('text=Pending Join Requests')).toBeVisible({ timeout: 10000 });
    await expect(page.getByRole('button', { name: /approve/i }).first()).toBeVisible({ timeout: 10000 });
    await expect(page.getByRole('button', { name: /reject/i }).first()).toBeVisible({ timeout: 10000 });
  });

  test('admin can approve a join request', async ({ page }) => {
    await ensureLoggedIn(page, E2E_ADMIN);
    await page.goto('/admin');
    await expect(page.locator('h1')).toHaveText('Admin Dashboard', { timeout: 10000 });

    // Scope to the specific row by team name (unique via Date.now() in beforeAll)
    const requestRow = page.locator('tr').filter({ hasText: teamName });
    await expect(requestRow.getByRole('button', { name: /approve/i })).toBeVisible({ timeout: 10000 });
    await requestRow.getByRole('button', { name: /approve/i }).click();

    // The row should disappear after approval
    await expect(requestRow).not.toBeVisible({ timeout: 10000 });
  });
});

test.describe('Team owner sees join requests in dashboard', () => {
  let ownerToken: string;
  let requesterToken: string;
  let teamId: string;

  test.beforeAll(async ({ request }) => {
    ownerToken = await getAuthToken(request, API_BASE_URL, E2E_USER);
    requesterToken = await getAuthToken(request, API_BASE_URL, E2E_SECOND_USER);

    const created = await createTeamViaApi(request, API_BASE_URL, `Owner Dashboard Team ${Date.now()}`, ownerToken);
    teamId = created.teamId;
    if (created.newToken) ownerToken = created.newToken;

    await requestJoinTeamViaApi(request, API_BASE_URL, teamId, requesterToken);
  });

  test.afterAll(async ({ request }) => {
    const adminToken = await getAuthToken(request, API_BASE_URL, E2E_ADMIN);
    if (teamId) await deleteTeamViaApi(request, API_BASE_URL, teamId, adminToken);
  });

  test('team owner sees pending requests in their team dashboard', async ({ page }) => {
    await ensureLoggedIn(page, E2E_USER);
    await page.goto(`/teams/${teamId}/dashboard`);

    await expect(page.locator('text=Pending Join Requests')).toBeVisible({ timeout: 10000 });
    await expect(page.getByRole('button', { name: /approve/i }).first()).toBeVisible({ timeout: 10000 });
  });

  test('team owner can reject a join request from dashboard', async ({ page }) => {
    await ensureLoggedIn(page, E2E_USER);
    await page.goto(`/teams/${teamId}/dashboard`);

    await expect(page.getByRole('button', { name: /reject/i }).first()).toBeVisible({ timeout: 10000 });

    const responsePromise = page.waitForResponse(res =>
      res.url().includes('/reject') && res.request().method() === 'PUT' && res.status() === 200
    );

    await page.getByRole('button', { name: /reject/i }).first().click();
    await responsePromise;

    await expect(page.locator('text=Pending Join Requests')).not.toBeVisible({ timeout: 10000 });
  });
});
