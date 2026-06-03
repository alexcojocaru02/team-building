import { test, expect } from '@playwright/test';
import { ensureLoggedIn, getAuthToken, deletePostViaApi, E2E_USER } from '../helpers/auth';
import { API_BASE_URL } from '../helpers/config';

test.describe('Feed', () => {
  let authToken: string;
  const createdPostIds: string[] = [];

  test.beforeAll(async ({ request }) => {
    authToken = await getAuthToken(request, API_BASE_URL, E2E_USER);
  });

  test.beforeEach(async ({ page }) => {
    await ensureLoggedIn(page, E2E_USER);
    await page.goto('/feed');
    await expect(page.locator('h1')).toHaveText('Team Feed', { timeout: 10000 });
  });

  test.afterEach(async ({ request }) => {
    // Sterge posturile create in test via API
    for (const postId of createdPostIds) {
      await deletePostViaApi(request, API_BASE_URL, postId, authToken);
    }
    createdPostIds.length = 0;
  });

  test('pagina de feed se incarca', async ({ page }) => {
    await expect(page.locator('textarea[name="content"]')).toBeVisible();
  });

  test('poate posta un update nou', async ({ page }) => {
    const postText = `E2E post ${Date.now()}`;

    // Interceptam raspunsul ca sa capturem postId pentru cleanup
    const responsePromise = page.waitForResponse(res =>
      res.url().includes('/api/feed') && res.request().method() === 'POST' && res.status() === 200
    );

    await page.fill('textarea[name="content"]', postText);
    await page.click('button:has-text("Post update")');

    const response = await responsePromise;
    const body = await response.json();
    createdPostIds.push(body.id);

    await expect(page.locator(`text=${postText}`)).toBeVisible({ timeout: 10000 });
  });

  test('butonul Post update este dezactivat cand textul e gol', async ({ page }) => {
    const submitBtn = page.locator('button:has-text("Post update")');
    await expect(submitBtn).toBeDisabled();

    await page.fill('textarea[name="content"]', 'ceva text');
    await expect(submitBtn).toBeEnabled();

    await page.fill('textarea[name="content"]', '');
    await expect(submitBtn).toBeDisabled();
  });

  test('poate da like unui post existent', async ({ page }) => {
    const responsePromise = page.waitForResponse(res =>
      res.url().includes('/api/feed') && res.request().method() === 'POST' && res.status() === 200
    );

    await page.fill('textarea[name="content"]', `E2E like test ${Date.now()}`);
    await page.click('button:has-text("Post update")');

    const response = await responsePromise;
    const body = await response.json();
    createdPostIds.push(body.id);

    await page.waitForTimeout(300);
    const likeBtn = page.locator('button:has-text("Like")').first();
    await likeBtn.click();
    await expect(page.locator('button:has-text("Liked")').first()).toBeVisible({ timeout: 5000 });
  });

  test('poate sterge propriul post', async ({ page }) => {
    const responsePromise = page.waitForResponse(res =>
      res.url().includes('/api/feed') && res.request().method() === 'POST' && res.status() === 200
    );

    const postText = `E2E delete test ${Date.now()}`;
    await page.fill('textarea[name="content"]', postText);
    await page.click('button:has-text("Post update")');
    await responsePromise;

    await page.waitForTimeout(300);

    // Apasa butonul de stergere (trash icon) pe primul post
    await page.locator('article').first().locator('button[aria-label="Delete post"]').click();

    // Confirma in dialog (butonul din mat-dialog-actions)
    await page.locator('mat-dialog-actions button:has-text("Delete")').click();

    await expect(page.locator(`text=${postText}`)).not.toBeVisible({ timeout: 5000 });
    // Postul a fost sters din UI, nu mai e nevoie de cleanup via API
  });
});
