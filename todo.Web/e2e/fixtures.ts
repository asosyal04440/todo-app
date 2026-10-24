import { test as base, expect, type APIRequestContext, type Page } from '@playwright/test';

export const apiUrl = process.env.TODO_E2E_API_URL ?? 'http://localhost:17070/api';

export const clientHeaders = { 'X-Todo-Client': '1' };

export interface AuthSession {
  email: string;
  password: string;
  displayName: string;
  accessToken: string;
  expiresAtUtc: string;
  user: {
    id: number;
    email: string;
    displayName: string;
    role: 'User' | 'Admin';
  };
}

export function uniqueEmail(prefix: string): string {
  return `${prefix}-${Date.now()}-${Math.random().toString(36).slice(2, 8)}@example.com`;
}

export async function registerUser(
  request: APIRequestContext,
  displayName: string,
): Promise<AuthSession> {
  const email = uniqueEmail('e2e');
  const password = 'E2eSifre123';
  const response = await request.post(`${apiUrl}/auth/register`, {
    headers: clientHeaders,
    data: { email, displayName, password },
  });

  expect(response.status()).toBe(201);

  const result = (await response.json()) as Omit<AuthSession, 'email' | 'password' | 'displayName'>;

  return { email, password, displayName, ...result };
}

async function authenticateInBrowser(
  page: Page,
  endpoint: 'register' | 'login',
  body: Record<string, string>,
): Promise<Omit<AuthSession, 'email' | 'password' | 'displayName'>> {
  await page.goto('/login');

  const result = await page.evaluate(
    async ({ url, payload, headers }) => {
      const response = await fetch(url, {
        method: 'POST',
        credentials: 'include',
        headers: { 'Content-Type': 'application/json', ...headers },
        body: JSON.stringify(payload),
      });

      return { status: response.status, body: await response.json() };
    },
    { url: `/api/auth/${endpoint}`, payload: body, headers: clientHeaders },
  );

  expect([200, 201]).toContain(result.status);

  return result.body as Omit<AuthSession, 'email' | 'password' | 'displayName'>;
}

export async function registerInBrowser(page: Page, displayName: string): Promise<AuthSession> {
  const email = uniqueEmail('e2e');
  const password = 'E2eSifre123';
  const result = await authenticateInBrowser(page, 'register', { email, displayName, password });

  return { email, password, displayName, ...result };
}

export async function signInInBrowser(page: Page, session: AuthSession): Promise<void> {
  await authenticateInBrowser(page, 'login', { email: session.email, password: session.password });
}

export const test = base.extend<{ session: AuthSession; request: APIRequestContext }>({
  session: async ({ page }, use) => {
    const session = await registerInBrowser(page, 'E2E Kullanıcısı');

    await use(session);
  },
  request: async ({ playwright, session }, use) => {
    const authenticatedRequest = await playwright.request.newContext({
      extraHTTPHeaders: { Authorization: `Bearer ${session.accessToken}` },
    });

    await use(authenticatedRequest);
    await authenticatedRequest.dispose();
  },
});

export { expect };
