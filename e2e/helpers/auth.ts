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

const TIMEOUT = 30_000;

export async function ensureLoggedIn(page: Page, user = E2E_USER): Promise<void> {
  await page.goto('/login');
  await page.fill('#login-email', user.email);
  await page.fill('#login-password', user.password);
  await page.click('button[type="submit"]');

  // Wait for either successful redirect or an error message
  const result = await Promise.race([
    page.waitForURL('**/home', { timeout: TIMEOUT }).then(() => 'success'),
    page.waitForSelector('text=Invalid email or password', { timeout: TIMEOUT }).then(() => 'invalid'),
  ]);

  if (result === 'invalid') {
    // User doesn't exist yet — register (which auto-logs in)
    await page.goto('/login');
    await page.locator('p button:has-text("Sign Up")').click();
    await page.fill('#register-full-name', user.fullName);
    await page.fill('#register-email', user.email);
    await page.fill('#register-password', user.password);
    await page.click('button[type="submit"]');
    await page.waitForURL('**/home', { timeout: TIMEOUT });
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
