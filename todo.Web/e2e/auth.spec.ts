import { test as base } from '@playwright/test';
import {
  apiUrl,
  clientHeaders,
  expect,
  registerUser,
  signInInBrowser,
  uniqueEmail,
  useApiUrl,
} from './fixtures';

const test = base;

test('a visitor can register, stay signed in after a reload, log out, and log back in', async ({
  page,
}) => {
  const email = uniqueEmail('ui');
  const password = 'UiSifre123';

  await useApiUrl(page);
  await page.goto('/');

  await expect(page).toHaveURL(/\/login$/);

  await page.getByRole('tab', { name: 'Kayıt ol' }).click();
  await page.getByLabel('Adın').fill('Arayüz Kullanıcısı');
  await page.getByLabel('E-posta').fill(email);
  await page.getByLabel('Şifre').fill(password);
  await page.getByRole('button', { name: 'Hesap oluştur' }).click();

  await expect(page).toHaveURL(/\/$/);
  await expect(page.locator('.account-name')).toHaveText('Arayüz Kullanıcısı');

  await page.reload();
  await expect(page.locator('.account-name')).toHaveText('Arayüz Kullanıcısı');

  const browserStorage = await page.evaluate(() => ({
    localStorageKeys: Object.keys(localStorage),
    sessionStorageKeys: Object.keys(sessionStorage),
    visibleCookies: document.cookie,
  }));
  expect(browserStorage.localStorageKeys).toEqual([]);
  expect(browserStorage.sessionStorageKeys).toEqual([]);
  expect(browserStorage.visibleCookies).not.toContain('todo_refresh');

  const refreshCookie = (await page.context().cookies()).find(
    (cookie) => cookie.name === 'todo_refresh',
  );
  expect(refreshCookie?.httpOnly).toBe(true);
  expect(refreshCookie?.secure).toBe(true);
  expect(refreshCookie?.sameSite).toBe('Strict');
  expect(refreshCookie?.path).toBe('/api/auth');

  await page.getByRole('button', { name: 'Çıkış yap' }).click();
  await expect(page).toHaveURL(/\/login$/);

  await page.reload();
  await expect(page).toHaveURL(/\/login$/);

  await page.getByLabel('E-posta').fill(email);
  await page.getByLabel('Şifre').fill(password);
  await page.getByRole('button', { name: 'Giriş yap' }).click();

  await expect(page).toHaveURL(/\/$/);
  await expect(page.locator('.account-name')).toHaveText('Arayüz Kullanıcısı');
});

test('the login page explains a wrong password and a weak new password', async ({
  page,
  request,
}) => {
  const session = await registerUser(request, 'Yanlış Şifre Testi');

  await useApiUrl(page);
  await page.goto('/login');

  await page.getByLabel('E-posta').fill(session.email);
  await page.getByLabel('Şifre').fill('YanlisSifre1');
  await page.getByRole('button', { name: 'Giriş yap' }).click();

  await expect(page.getByRole('alert')).toHaveText('E-posta adresi veya şifre hatalı.');
  await expect(page).toHaveURL(/\/login$/);

  await page.getByRole('tab', { name: 'Kayıt ol' }).click();
  await page.getByLabel('Adın').fill('Zayıf Şifre');
  await page.getByLabel('E-posta').fill(uniqueEmail('weak'));
  await page.getByLabel('Şifre').fill('sadeceharf');
  await page.getByRole('button', { name: 'Hesap oluştur' }).click();

  await expect(page.getByRole('alert')).toContainText('harf ve rakam');
});

test("a user cannot see or change another user's lists", async ({ page, playwright }) => {
  const anonymous = await playwright.request.newContext();
  const owner = await registerUser(anonymous, 'Liste Sahibi');
  const stranger = await registerUser(anonymous, 'Yabancı');
  await anonymous.dispose();

  const ownerApi = await playwright.request.newContext({
    extraHTTPHeaders: { Authorization: `Bearer ${owner.accessToken}` },
  });
  const strangerApi = await playwright.request.newContext({
    extraHTTPHeaders: { Authorization: `Bearer ${stranger.accessToken}` },
  });

  try {
    const createResponse = await ownerApi.post(`${apiUrl}/todo-lists`, {
      data: { name: `Gizli liste ${Date.now()}` },
    });
    expect(createResponse.status()).toBe(201);
    const ownersList = (await createResponse.json()) as { id: number; name: string };

    const readAttempt = await strangerApi.get(`${apiUrl}/todo-lists/${ownersList.id}`);
    expect(readAttempt.status()).toBe(404);

    const renameAttempt = await strangerApi.put(`${apiUrl}/todo-lists/${ownersList.id}`, {
      data: { name: 'Ele geçirildi' },
    });
    expect(renameAttempt.status()).toBe(404);

    const deleteAttempt = await strangerApi.delete(`${apiUrl}/todo-lists/${ownersList.id}`);
    expect(deleteAttempt.status()).toBe(404);

    const strangersLists = await strangerApi.get(`${apiUrl}/todo-lists`);
    expect(await strangersLists.json()).toEqual([]);

    const adminAttempt = await strangerApi.get(`${apiUrl}/admin/users`);
    expect(adminAttempt.status()).toBe(403);

    const ownerStillHasList = await ownerApi.get(`${apiUrl}/todo-lists/${ownersList.id}`);
    expect(ownerStillHasList.status()).toBe(200);
    expect((await ownerStillHasList.json()).name).toBe(ownersList.name);

    await signInInBrowser(page, stranger);
    await page.goto('/');
    await expect(page.locator('.account-name')).toHaveText('Yabancı');
    await expect(page.locator('.muted-message').filter({ hasText: 'Henüz liste yok' })).toBeVisible();
    await expect(page.getByText(ownersList.name)).toHaveCount(0);

    await page.goto('/admin/users');
    await expect(page).toHaveURL(/\/$/);

    await ownerApi.delete(`${apiUrl}/todo-lists/${ownersList.id}`);
  } finally {
    await ownerApi.dispose();
    await strangerApi.dispose();
  }
});

test('the API rejects requests without a valid token', async ({ request }) => {
  const withoutToken = await request.get(`${apiUrl}/todo-lists`);
  expect(withoutToken.status()).toBe(401);

  const withFakeToken = await request.get(`${apiUrl}/todo-lists`, {
    headers: { Authorization: 'Bearer bu.sahte.bir-token' },
  });
  expect(withFakeToken.status()).toBe(401);
});

test('cookie-based auth endpoints reject requests without the anti-CSRF header', async ({
  request,
}) => {
  const withoutHeader = await request.post(`${apiUrl}/auth/refresh`);
  expect(withoutHeader.status()).toBe(400);

  const withHeader = await request.post(`${apiUrl}/auth/refresh`, { headers: clientHeaders });
  expect(withHeader.status()).toBe(401);
});
