import { request } from '@playwright/test';
import { E2E_ADMIN } from './helpers/auth';
import { API_BASE_URL } from './helpers/config';

export default async function globalSetup() {
  const ctx = await request.newContext({ ignoreHTTPSErrors: true });

  // Ensure E2E_ADMIN user exists
  await ctx.post(`${API_BASE_URL}/auth/register`, {
    data: { fullName: E2E_ADMIN.fullName, email: E2E_ADMIN.email, password: E2E_ADMIN.password },
  });

  // Promote to Admin (dev-only endpoint, no-op if already Admin)
  const res = await ctx.post(`${API_BASE_URL}/auth/dev/promote-admin`, {
    data: { email: E2E_ADMIN.email },
  });

  if (!res.ok()) {
    console.warn(`[globalSetup] promote-admin returned ${res.status()} — admin tests may fail`);
  }

  await ctx.dispose();
}
