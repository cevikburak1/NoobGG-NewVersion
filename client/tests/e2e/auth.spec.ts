import { demoUsername } from './demoAccounts';
import { expect, test } from '@playwright/test';

const API = process.env.E2E_API_URL || 'http://localhost:5000';

async function signIn(request: import('@playwright/test').APIRequestContext, username: string) {
  const response = await request.post(`${API}/api/auth/login`, {
    data: { emailOrUsername: demoUsername(username), password: 'Test1234!' },
  });
  expect(response.ok()).toBe(true);
  return response.json() as Promise<{ accessToken: string; user: { id: string } }>;
}

test('demo account can sign in and protected routes enforce payment boundary', async ({ page, request }, testInfo) => {
  test.skip(!process.env.E2E_API, 'Requires the local Docker API with explicit development seed');

  const { accessToken } = await signIn(request, 'Player_0006');
  const headers = { Authorization: `Bearer ${accessToken}` };
  const sessions = await request.get(`${API}/api/sessions/mine`, { headers });
  expect(sessions.status()).toBe(200);
  const checkout = await request.post(`${API}/api/subscriptions/checkout`, {
    headers, data: { planId: 'not-a-plan' },
  });
  expect(checkout.status()).toBe(503);
  const moderation = await request.get(`${API}/api/moderation/reports`, { headers });
  expect(moderation.status()).toBe(403);

  await page.goto('/login');
  await page.getByLabel(/e-posta veya kullanıcı adı|email or username/i).fill('Player_0006');
  await page.getByLabel(/şifre|password/i).fill('Test1234!');
  await page.getByRole('button', { name: /giriş yap|sign in/i }).click();
  await expect(page).toHaveURL(/\/rooms$/);
  await expect(page.getByRole('heading', { name: /odalar|rooms/i }).first()).toBeVisible();
  await expect(page.locator('canvas').first()).toBeVisible();
  await page.screenshot({ path: testInfo.outputPath('rooms.png'), fullPage: true });
});

test('report and temporary suspension revoke existing access', async ({ request }, testInfo) => {
  test.skip(!process.env.E2E_API, 'Requires the local Docker API with explicit development seed');
  test.skip(testInfo.project.name !== 'desktop', 'The sanction is shared state and runs once');
  const reporter = await signIn(request, 'Player_0007');
  const target = await signIn(request, 'Player_0999');
  const moderator = await signIn(request, 'Player_0002');
  const marker = `e2e-${Date.now()}`;
  const created = await request.post(`${API}/api/reports`, {
    headers: { Authorization: `Bearer ${reporter.accessToken}` },
    data: { targetType: 'User', reportedUserId: target.user.id, reason: 'Harassment', description: marker },
  });
  expect(created.ok()).toBe(true);

  const listed = await request.get(`${API}/api/moderation/reports?status=Pending&pageSize=100`, {
    headers: { Authorization: `Bearer ${moderator.accessToken}` },
  });
  expect(listed.ok()).toBe(true);
  const reports = await listed.json() as { items: { id: string; description: string }[] };
  const report = reports.items.find((item) => item.description === marker);
  expect(report).toBeDefined();
  const reviewed = await request.post(`${API}/api/moderation/reports/${report!.id}/review`, {
    headers: { Authorization: `Bearer ${moderator.accessToken}` },
    data: { action: 'suspend', reason: 'Automated moderation test', endsAt: new Date(Date.now() + 5_000).toISOString() },
  });
  expect(reviewed.ok()).toBe(true);

  const existingAccess = await request.get(`${API}/api/auth/me`, {
    headers: { Authorization: `Bearer ${target.accessToken}` },
  });
  expect(existingAccess.status()).toBe(401);
  const login = await request.post(`${API}/api/auth/login`, {
    data: { emailOrUsername: demoUsername('Player_0999'), password: 'Test1234!' },
  });
  expect(login.status()).toBe(403);
});
