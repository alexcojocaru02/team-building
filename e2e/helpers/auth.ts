import { Page, APIRequestContext } from '@playwright/test';

export const E2E_USER = {
  fullName: 'E2E Test User',
  email: 'e2e-user@teamconnect.test',
  password: 'E2eTest@1234!',
};

export const E2E_ADMIN = {
  fullName: 'E2E Admin User',
  email: 'e2e-admin@teamconnect.test',
  password: 'E2eAdmin@1234!',
};

const TIMEOUT = process.env['CI'] ? 60_000 : 30_000;

async function loginAndWait(page: Page, user: typeof E2E_USER): Promise<'success' | 'invalid'> {
  await page.goto('/login');
  await page.fill('#login-email', user.email);
  await page.fill('#login-password', user.password);
  await page.click('button[type="submit"]');
  return Promise.race([
    page.waitForURL('**/home', { timeout: TIMEOUT }).then(() => 'success' as const),
    page.waitForSelector('text=Invalid email or password', { timeout: TIMEOUT }).then(() => 'invalid' as const),
  ]);
}

export async function ensureLoggedIn(page: Page, user = E2E_USER): Promise<void> {
  const loginResult = await loginAndWait(page, user);
  if (loginResult === 'success') return;

  // User doesn't exist yet — register (which auto-logs in)
  await page.goto('/login');
  await page.locator('p button:has-text("Sign Up")').click();
  await page.fill('#register-full-name', user.fullName);
  await page.fill('#register-email', user.email);
  await page.fill('#register-password', user.password);
  await page.click('button[type="submit"]');

  const regResult = await Promise.race([
    page.waitForURL('**/home', { timeout: TIMEOUT }).then(() => 'success' as const),
    page.waitForSelector('text=Registration failed', { timeout: TIMEOUT }).then(() => 'exists' as const),
  ]);

  if (regResult === 'exists') {
    // Email already registered — retry login (race condition between parallel tests or previous run)
    const retryResult = await loginAndWait(page, user);
    if (retryResult !== 'success') {
      throw new Error(`ensureLoggedIn: could not log in as ${user.email} after register failed`);
    }
  }
}

export async function getAuthToken(request: APIRequestContext, apiBaseUrl: string, user = E2E_USER): Promise<string> {
  const response = await request.post(`${apiBaseUrl}/auth/login`, {
    data: { email: user.email, password: user.password },
  });

  if (response.ok()) {
    const body = await response.json();
    return body.token as string;
  }

  const regResponse = await request.post(`${apiBaseUrl}/auth/register`, {
    data: { fullName: user.fullName, email: user.email, password: user.password },
  });
  const body = await regResponse.json();
  return body.token as string;
}

export async function getTeamForUser(
  request: APIRequestContext,
  apiBaseUrl: string,
  user = E2E_USER
): Promise<string | null> {
  const token = await getAuthToken(request, apiBaseUrl, user);

  const meRes = await request.get(`${apiBaseUrl}/users/me`, {
    headers: { Authorization: `Bearer ${token}` },
  });
  if (!meRes.ok()) return null;
  const me = await meRes.json();

  const teamsRes = await request.get(`${apiBaseUrl}/teams`, {
    headers: { Authorization: `Bearer ${token}` },
  });
  if (!teamsRes.ok()) return null;
  const teams = await teamsRes.json();
  if (!Array.isArray(teams) || teams.length === 0) return null;

  // Prefer a team this user owns (so they have the TeamOwner JWT role for admin operations)
  const ownedTeam = (teams as any[]).find((t: any) => t.ownerId === me.id);
  if (ownedTeam) return ownedTeam.id;

  const memberTeam = (teams as any[]).find(
    (t: any) => Array.isArray(t.memberIds) && t.memberIds.includes(me.id)
  );
  return memberTeam?.id ?? null;
}

export async function createTeamViaApi(
  request: APIRequestContext,
  apiBaseUrl: string,
  name: string,
  token: string
): Promise<{ teamId: string; newToken?: string }> {
  const res = await request.post(`${apiBaseUrl}/teams`, {
    data: { name },
    headers: { Authorization: `Bearer ${token}` },
  });
  const body = await res.json();
  return { teamId: body.team?.id ?? body.id, newToken: body.newToken };
}

export async function deleteTeamViaApi(
  request: APIRequestContext,
  apiBaseUrl: string,
  teamId: string,
  token: string
): Promise<void> {
  await request.delete(`${apiBaseUrl}/teams/${teamId}`, {
    headers: { Authorization: `Bearer ${token}` },
  });
}

export async function requestJoinTeamViaApi(
  request: APIRequestContext,
  apiBaseUrl: string,
  teamId: string,
  token: string
): Promise<void> {
  await request.post(`${apiBaseUrl}/teams/${teamId}/join-requests`, {
    headers: { Authorization: `Bearer ${token}` },
  });
}

/**
 * Ensures `requester` and `approver` share a team so the feedback API's
 * AreTeammatesAsync check passes.  Strategy: the requester creates a fresh
 * shared team (gaining TeamOwner role + a new token), then immediately adds
 * the approver to it.
 *
 * Returns the created team ID so the caller can delete it after the test.
 */
export async function ensureTeammates(
  request: APIRequestContext,
  apiBaseUrl: string,
  _teamId: string,
  requester: typeof E2E_USER,
  approver: typeof E2E_USER
): Promise<string | null> {
  const requesterToken = await getAuthToken(request, apiBaseUrl, requester);
  const approverToken = await getAuthToken(request, apiBaseUrl, approver);

  // Resolve the approver's user ID
  const approverMeRes = await request.get(`${apiBaseUrl}/users/me`, {
    headers: { Authorization: `Bearer ${approverToken}` },
  });
  if (!approverMeRes.ok()) return null;
  const approverMe = await approverMeRes.json();

  // Requester creates a team → gains TeamOwner role and a fresh token
  const { teamId: sharedTeamId, newToken } = await createTeamViaApi(
    request, apiBaseUrl, `e2e-shared-${Date.now()}`, requesterToken
  );
  if (!sharedTeamId) return null;

  const ownerToken = newToken ?? requesterToken;

  // Add the approver (user) to the shared team using the TeamOwner token
  const addRes = await request.post(`${apiBaseUrl}/teams/${sharedTeamId}/add/${approverMe.id}`, {
    headers: { Authorization: `Bearer ${ownerToken}` },
  });
  if (!addRes.ok()) {
    console.error(`ensureTeammates: add user failed ${addRes.status()}: ${await addRes.text()}`);
    return null;
  }

  return sharedTeamId;
}

export async function deletePostViaApi(
  request: APIRequestContext,
  apiBaseUrl: string,
  postId: string,
  token: string
): Promise<void> {
  await request.delete(`${apiBaseUrl}/feed/${postId}`, {
    headers: { Authorization: `Bearer ${token}` },
  });
}
