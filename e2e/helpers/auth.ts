import { Page, APIRequestContext } from '@playwright/test';

export const E2E_USER = {
  fullName: 'E2E Test User',
  email: 'e2e-user@teamconnect.test',
  password: 'Passw0rd',
};

export const E2E_ADMIN = {
  fullName: 'E2E Admin User',
  email: 'e2e-admin@teamconnect.test',
  password: 'Passw0rd',
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
  const loginRes = await request.post(`${apiBaseUrl}/auth/login`, {
    data: { email: user.email, password: user.password },
  });

  if (loginRes.ok()) {
    return ((await loginRes.json()) as { token: string }).token;
  }

  const regRes = await request.post(`${apiBaseUrl}/auth/register`, {
    data: { fullName: user.fullName, email: user.email, password: user.password },
  });

  if (regRes.ok()) {
    return ((await regRes.json()) as { token: string }).token;
  }

  // Registration failed (email already taken) — retry login
  const retryRes = await request.post(`${apiBaseUrl}/auth/login`, {
    data: { email: user.email, password: user.password },
  });
  if (!retryRes.ok()) throw new Error(`getAuthToken: login failed for ${user.email} (${retryRes.status()})`);
  return ((await retryRes.json()) as { token: string }).token;
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
