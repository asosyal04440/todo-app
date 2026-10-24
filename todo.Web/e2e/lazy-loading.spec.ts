import { test, expect } from '@playwright/test';

test('admin code arrives only on the first Users navigation', async ({ page }, testInfo) => {
  // This checks real Angular bundles in a fresh browser context. Only API data is mocked;
  // there is no real login, database write, or API server involved in this test.
  const user = {
    id: 1,
    email: 'lazy-test@example.invalid',
    displayName: 'Lazy Test Admin',
    role: 'Admin',
    hasPassword: true,
    hasGoogleLogin: false,
    createdAtUtc: '2026-01-01T00:00:00Z',
  };
  const emptyPage = { items: [], totalCount: 0, pageNumber: 1, pageSize: 20, totalPages: 0 };
  await page.route('**/api/**', async (route) => {
    const request = route.request();
    const url = new URL(request.url());
    if (url.pathname === '/api/auth/refresh' && request.method() === 'POST') {
      await route.fulfill({
        json: {
          accessToken: 'isolated-browser-test-only',
          expiresAtUtc: new Date(Date.now() + 3_600_000).toISOString(),
          user,
        },
      });
    } else if (url.pathname === '/api/auth/config') {
      await route.fulfill({ json: { googleClientId: null } });
    } else if (url.pathname === '/api/todo-lists' && request.method() === 'GET') {
      await route.fulfill({ json: emptyPage });
    } else if (url.pathname === '/api/admin/users' && request.method() === 'GET') {
      await route.fulfill({ json: { ...emptyPage, items: [user], totalCount: 1, totalPages: 1 } });
    } else {
      await route.fulfill({ status: 404, json: { title: 'Unexpected test API request' } });
    }
  });

  let phase = 'initial';
  const network: { phase: string; path: string; type: string }[] = [];
  page.on('request', (request) => {
    network.push({ phase, path: new URL(request.url()).pathname, type: request.resourceType() });
  });
  const isAdminScript = (url: string) =>
    /\/admin-users-page(?:-[\w-]+)?\.js$/.test(new URL(url).pathname);
  const adminScripts = () =>
    network.filter((request) => isAdminScript(new URL(request.path, 'http://localhost').href));

  await page.goto('/');
  await expect(page.getByRole('heading', { name: 'Görevlerin', exact: true })).toBeVisible();
  await expect(page.locator('.connection-state')).toHaveText('bağlı');
  expect(
    adminScripts(),
    'the initial task screen must not request admin component code',
  ).toHaveLength(0);
  expect(network.some((request) => request.path === '/runtime-config.json')).toBe(false);

  phase = 'first-admin-navigation';
  const adminResponse = page.waitForResponse((response) => isAdminScript(response.url()));
  await page.getByRole('link', { name: 'Kullanıcılar', exact: true }).click();
  const loadedScript = await adminResponse;
  expect(loadedScript.status()).toBe(200);
  await expect(page.getByRole('heading', { name: 'Kullanıcılar', exact: true })).toBeVisible();
  await expect(page.getByRole('cell', { name: 'Lazy Test Admin', exact: true })).toBeVisible();
  expect(adminScripts()).toHaveLength(1);
  expect(adminScripts()[0].phase).toBe('first-admin-navigation');
  await page.screenshot({
    path: testInfo.outputPath('users-first-navigation.png'),
    fullPage: true,
  });

  phase = 'return-to-tasks';
  await page.getByRole('link', { name: /Görevlerime dön/ }).click();
  await expect(page.getByRole('heading', { name: 'Görevlerin', exact: true })).toBeVisible();
  phase = 'second-admin-navigation';
  await page.getByRole('link', { name: 'Kullanıcılar', exact: true }).click();
  await expect(page.getByRole('heading', { name: 'Kullanıcılar', exact: true })).toBeVisible();
  await expect(page.getByRole('cell', { name: 'Lazy Test Admin', exact: true })).toBeVisible();
  expect(
    adminScripts(),
    'the same loaded module is reused on a second SPA navigation',
  ).toHaveLength(1);

  await testInfo.attach('lazy-loading-network.json', {
    body: JSON.stringify(
      {
        configuration: testInfo.project.name,
        adminScript: new URL(loadedScript.url()).pathname,
        network,
      },
      null,
      2,
    ),
    contentType: 'application/json',
  });
  console.log(
    `${testInfo.project.name}: initial admin JS=0, first Users click=1, second click=0 new requests; ${new URL(loadedScript.url()).pathname}`,
  );
});
