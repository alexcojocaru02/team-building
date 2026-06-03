import { Page, APIRequestContext } from '@playwright/test';

// Useri ficsi reutilizati intre rulari — nu se sterg intre teste
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

export async function ensureLoggedIn(
  page: Page,
  user = E2E_USER
): Promise<void> {
  await page.goto('/login');

  // Incearca login — daca userul nu exista, il inregistreaza
  await page.fill('#login-email', user.email);
  await page.fill('#login-password', user.password);
  await page.click('button[type="submit"]');

  // Asteapta fie home (succes) fie eroare (user inexistent)
  await page.waitForTimeout(1000);
  const url = page.url();

  if (!url.includes('/home')) {
    // Userul nu exista — inregistreaza-l (care face si auto-login)
    await page.goto('/login');
    await page.locator('p button:has-text("Sign Up")').click();
    await page.fill('#register-full-name', user.fullName);
    await page.fill('#register-email', user.email);
    await page.fill('#register-password', user.password);
    await page.click('button[type="submit"]');
    await page.waitForURL('**/home', { timeout: 10000 });
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

  // Userul nu exista — inregistreaza-l
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
