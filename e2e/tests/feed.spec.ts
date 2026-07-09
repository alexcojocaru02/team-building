import { test, expect, Page } from '@playwright/test';
import { ensureLoggedIn, getAuthToken, deletePostViaApi, E2E_USER } from '../helpers/auth';
import { API_BASE_URL } from '../helpers/config';

// The compose textarea is collapsed on load; expand it first.
async function openCompose(page: Page): Promise<void> {
  const trigger = page.locator('.compose-trigger');
  if (await trigger.isVisible()) {
    await trigger.click();
  }
  await expect(page.locator('textarea[name="content"]')).toBeVisible({ timeout: 5_000 });
}

async function createPost(page: Page, text: string): Promise<string> {
  await openCompose(page);
  const responsePromise = page.waitForResponse(res =>
    res.url().includes('/api/feed') && res.request().method() === 'POST' && res.status() === 200
  );
  await page.fill('textarea[name="content"]', text);
  await page.click('button:has-text("Post update")');
  const response = await responsePromise;
  const body = await response.json();
  return body.id as string;
}

test.describe('Feed', () => {
  let authToken: string;
  const createdPostIds: string[] = [];

  test.beforeAll(async ({ request }) => {
    authToken = await getAuthToken(request, API_BASE_URL, E2E_USER);
  });

  test.beforeEach(async ({ page }) => {
    await ensureLoggedIn(page, E2E_USER);
    await page.goto('/feed');
    await expect(page.locator('h1')).toHaveText('Team Feed', { timeout: 10_000 });
  });

  test.afterEach(async ({ request }) => {
    for (const postId of createdPostIds) {
      await deletePostViaApi(request, API_BASE_URL, postId, authToken);
    }
    createdPostIds.length = 0;
  });

  test('feed page loads', async ({ page }) => {
    await openCompose(page);
    await expect(page.locator('textarea[name="content"]')).toBeVisible();
  });

  test('Post update button is disabled when text is empty', async ({ page }) => {
    await openCompose(page);
    const submitBtn = page.locator('button:has-text("Post update")');
    await expect(submitBtn).toBeDisabled();

    await page.fill('textarea[name="content"]', 'some text');
    await expect(submitBtn).toBeEnabled();

    await page.fill('textarea[name="content"]', '');
    await expect(submitBtn).toBeDisabled();
  });

  test('can post a new update', async ({ page }) => {
    const postText = `E2E post ${Date.now()}`;
    const id = await createPost(page, postText);
    createdPostIds.push(id);
    await expect(page.locator(`text=${postText}`)).toBeVisible({ timeout: 10_000 });
  });

  test('can like a post', async ({ page }) => {
    const id = await createPost(page, `E2E like test ${Date.now()}`);
    createdPostIds.push(id);

    await page.waitForTimeout(300);
    await page.locator('button:has-text("Like")').first().click();
    await expect(page.locator('button:has-text("Liked")').first()).toBeVisible({ timeout: 5_000 });
  });

  test('can delete own post', async ({ page }) => {
    const postText = `E2E delete test ${Date.now()}`;
    await createPost(page, postText);
    // not pushed to createdPostIds — deleted within the test

    await page.waitForTimeout(300);
    // Open the 3-dot options menu on the first article
    await page.locator('article').first().locator('button[aria-label="Post options"]').click();
    // Delete is rendered in an overlay outside the article
    await page.locator('button[aria-label="Delete post"]').click();
    await page.locator('mat-dialog-actions button:has-text("Delete")').click();

    await expect(page.locator(`text=${postText}`)).not.toBeVisible({ timeout: 5_000 });
  });
});
