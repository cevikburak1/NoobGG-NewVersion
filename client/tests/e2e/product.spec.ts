import { demoUsername } from './demoAccounts';
import { expect, test, type APIRequestContext, type Page } from '@playwright/test';

const API = process.env.E2E_API_URL || 'http://localhost:5000';
test.skip(!process.env.E2E_API, 'Requires an isolated seeded development API');

async function login(request: APIRequestContext, username: string) {
  const response = await request.post(`${API}/api/auth/login`, {
    data: { emailOrUsername: demoUsername(username), password: 'Test1234!' },
  });
  expect(response.ok(), await response.text()).toBe(true);
  return response.json() as Promise<{ accessToken: string; refreshToken: string; user: { id: string } }>;
}

async function uiLogin(page: Page, username: string) {
  await page.goto('/login');
  await page.getByLabel(/e-posta veya kullanıcı adı|email or username/i).fill(demoUsername(username));
  await page.getByLabel(/şifre|password/i).fill('Test1234!');
  await page.getByRole('button', { name: /giriş yap|sign in/i }).click();
  await expect(page).toHaveURL(/\/rooms$/);
}

test('authenticated product routes load without server or JavaScript errors', async ({ page }, info) => {
  test.setTimeout(120_000);
  const errors: string[] = [];
  page.on('pageerror', error => errors.push(error.message));
  page.on('response', response => {
    if (response.url().includes('/api/') && response.status() >= 500) {
      errors.push(`${response.status()} ${response.url()}`);
    }
  });
  await uiLogin(page, info.project.name === 'desktop' ? 'Player_0960' : 'Player_0962');
  for (const path of ['/rooms', '/discover', '/guilds', '/community', '/compare', '/tournaments',
    '/messages', '/friends', '/favorites', '/notifications', '/settings', '/profile/edit',
    '/profile/games', '/leaderboard', '/subscriptions']) {
    await test.step(path, async () => {
      await page.evaluate((target) => {
        window.history.pushState({}, '', target);
        window.dispatchEvent(new PopStateEvent('popstate'));
      }, path);
      await expect(page).toHaveURL(new RegExp(`${path}$`));
      await expect(page.locator('main')).toBeVisible();
      await expect(page.locator('body')).not.toContainText('Unexpected Application Error');
      await expect(page.locator('main').getByRole('heading').first()).toBeVisible();
    });
  }
  await page.reload();
  await expect(page).toHaveURL(/\/subscriptions$/);
  await expect(page.getByRole('heading', { name: /choose your plan/i })).toBeVisible();
  await expect(page.getByRole('button', { name: 'Yearly', exact: true })).toHaveCount(0);
  expect(errors).toEqual([]);
});

test('private room is hidden and invite acceptance remains available', async ({ request }, info) => {
  const owner = await login(request, info.project.name === 'desktop' ? 'Player_0970' : 'Player_0972');
  const stranger = await login(request, info.project.name === 'desktop' ? 'Player_0971' : 'Player_0973');
  const ownerHeaders = { Authorization: `Bearer ${owner.accessToken}` };
  const strangerHeaders = { Authorization: `Bearer ${stranger.accessToken}` };
  const oldRooms = await (await request.get(`${API}/api/rooms?creatorId=${owner.user.id}`, { headers: ownerHeaders })).json();
  for (const room of oldRooms.items) await request.delete(`${API}/api/rooms/${room.id}`, { headers: ownerHeaders });
  const ownerGames = await (await request.get(`${API}/api/profiles/${owner.user.id}/games`, { headers: ownerHeaders })).json();
  const strangerGames = await (await request.get(`${API}/api/profiles/${stranger.user.id}/games`, { headers: strangerHeaders })).json();
  const gameId = ownerGames.find((g: { gameId: string }) => strangerGames.some((s: { gameId: string }) => s.gameId === g.gameId))?.gameId;
  expect(gameId).toBeDefined();
  const created = await request.post(`${API}/api/rooms`, { headers: ownerHeaders,
    data: { title: 'Private regression room', gameId, isPublic: false, region: 'EU', language: 'English' } });
  expect(created.ok(), await created.text()).toBe(true);
  const room = await created.json();
  try {
    expect((await request.get(`${API}/api/rooms/${room.id}`)).status()).toBe(404);
    expect((await request.get(`${API}/api/rooms/${room.id}`, { headers: strangerHeaders })).status()).toBe(404);
    const leaked = await (await request.get(`${API}/api/rooms?creatorId=${owner.user.id}`)).json();
    expect(leaked.items.some((item: { id: string }) => item.id === room.id)).toBe(false);
    expect((await request.post(`${API}/api/rooms/${room.id}/join`, { headers: strangerHeaders })).status()).toBe(403);
    const invited = await request.post(`${API}/api/rooms/${room.id}/invite/${stranger.user.id}`, { headers: ownerHeaders });
    expect(invited.ok(), await invited.text()).toBe(true);
    const invites = await (await request.get(`${API}/api/rooms/invites`, { headers: strangerHeaders })).json();
    const invite = invites.find((item: { roomId: string }) => item.roomId === room.id);
    expect(invite).toBeDefined();
    const accepted = await request.post(`${API}/api/rooms/invites/${invite.id}/accept`, { headers: strangerHeaders });
    expect(accepted.ok(), await accepted.text()).toBe(true);
    expect((await request.get(`${API}/api/rooms/${room.id}`, { headers: strangerHeaders })).status()).toBe(200);
  } finally {
    await request.delete(`${API}/api/rooms/${room.id}`, { headers: ownerHeaders });
  }
});

test('scheduled cancellation keeps paid access until expiry', async ({ request }, info) => {
  const admin = await login(request, 'Player_0001');
  const member = await login(request, info.project.name === 'desktop' ? 'Player_0980' : 'Player_0982');
  const headers = { Authorization: `Bearer ${member.accessToken}` };
  const plans = await (await request.get(`${API}/api/subscriptions/plans`)).json();
  const plan = plans.plans.find((item: { tier: string }) => item.tier === 'Plus');
  const assigned = await request.post(`${API}/api/subscriptions/assign`, {
    headers: { Authorization: `Bearer ${admin.accessToken}` }, data: { userId: member.user.id, planId: plan.id },
  });
  expect(assigned.ok(), await assigned.text()).toBe(true);
  try {
    const cancelled = await request.post(`${API}/api/subscriptions/cancel`, { headers, data: { immediate: false } });
    expect(cancelled.ok(), await cancelled.text()).toBe(true);
    const subscription = await (await request.get(`${API}/api/subscriptions/me`, { headers })).json();
    expect(subscription.tier).toBe('Plus');
    expect(subscription.autoRenew).toBe(false);
    expect(subscription.entitlements.tier).toBe('Plus');
  } finally {
    await request.post(`${API}/api/subscriptions/assign`, {
      headers: { Authorization: `Bearer ${admin.accessToken}` },
      data: { userId: member.user.id, planId: plans.plans.find((item: { tier: string }) => item.tier === 'Free').id },
    });
  }
});

test('in-game name survives update and a fresh profile read', async ({ request }, info) => {
  const member = await login(request, info.project.name === 'desktop' ? 'Player_0006' : 'Player_0007');
  const headers = { Authorization: `Bearer ${member.accessToken}` };
  const read = async () => {
    const response = await request.get(`${API}/api/profiles/${member.user.id}/games`, { headers });
    expect(response.ok(), await response.text()).toBe(true);
    return response.json();
  };
  const profile = (await read())[0];
  const update = (name: string) => request.put(`${API}/api/profiles/me/games/${profile.id}`, { headers, data: { inGameName: name } });
  try {
    const response = await update('  NoobGG#TR  ');
    expect(response.ok(), await response.text()).toBe(true);
    expect((await read()).find((p: { id: string }) => p.id === profile.id).inGameName).toBe('NoobGG#TR');
    const full = await request.get(`${API}/api/profiles/${member.user.id}`, { headers });
    expect(full.ok(), await full.text()).toBe(true);
    expect(await full.text()).toContain('NoobGG#TR');
  } finally {
    expect((await update(profile.inGameName ?? '')).ok()).toBe(true);
  }
});

test('private profile game endpoint enforces the same visibility rules', async ({ request }, info) => {
  const owner = await login(request, info.project.name === 'desktop' ? 'Player_0940' : 'Player_0942');
  const headers = { Authorization: `Bearer ${owner.accessToken}` };
  const settings = await (await request.get(`${API}/api/settings`, { headers })).json();
  try {
    const updated = await request.put(`${API}/api/settings/privacy`, { headers,
      data: { ...settings, profileVisibility: 'Private' } });
    expect(updated.ok()).toBe(true);
    const profile = await (await request.get(`${API}/api/profiles/${owner.user.id}`)).json();
    expect(profile.isRestricted).toBe(true);
    expect((await request.get(`${API}/api/profiles/${owner.user.id}/games`)).status()).toBe(403);
    expect((await request.get(`${API}/api/profiles/${owner.user.id}/games`, { headers })).status()).toBe(200);
  } finally {
    await request.put(`${API}/api/settings/privacy`, { headers, data: settings });
  }
});

test('direct message reaches a second browser and composer fits on mobile', async ({ browser, request }, info) => {
  const firstUsername = info.project.name === 'desktop' ? 'Player_0950' : 'Player_0952';
  const secondUsername = info.project.name === 'desktop' ? 'Player_0951' : 'Player_0953';
  const first = await login(request, firstUsername);
  const second = await login(request, secondUsername);
  const headers = { Authorization: `Bearer ${first.accessToken}` };
  const headers2 = { Authorization: `Bearer ${second.accessToken}` };
  for (const h of [headers, headers2]) {
    expect((await request.put(`${API}/api/settings/privacy`, { headers: h,
      data: { profileVisibility: 'Public', dmPermission: 'Everyone', showOnlineStatus: true } })).ok()).toBe(true);
  }
  const conversation = await request.post(`${API}/api/dm/conversations`, { headers,
    data: { participantId: second.user.id } });
  expect(conversation.ok(), await conversation.text()).toBe(true);
  const context1 = await browser.newContext({ viewport: info.project.use.viewport });
  const context2 = await browser.newContext({ viewport: info.project.use.viewport });
  try {
    const page1 = await context1.newPage();
    const page2 = await context2.newPage();
    await uiLogin(page1, firstUsername);
    await uiLogin(page2, secondUsername);
    await page1.goto(`/messages?user=${second.user.id}`);
    await page2.goto(`/messages?user=${first.user.id}`);
    const input = page1.getByPlaceholder('Type a message...');
    await expect(input).toBeVisible();
    const message = `Realtime check ${Date.now()} ${info.project.name}`;
    await input.fill(message);
    const send = page1.getByRole('button', { name: 'Send message', exact: true });
    await expect(send).toBeEnabled();
    const box = await input.boundingBox();
    expect(box!.width).toBeGreaterThan(100);
    expect(box!.x + box!.width).toBeLessThanOrEqual(page1.viewportSize()!.width);
    expect(box!.y + box!.height).toBeLessThan(page1.viewportSize()!.height - 40);
    await send.click();
    await expect(page2.getByTestId('dm-message').filter({ hasText: message })).toBeVisible();
    const history = await request.get(`${API}/api/dm/conversations/${(await conversation.json()).id}/messages`, { headers: headers2 });
    expect((await history.json()).some((m: { content: string }) => m.content === message)).toBe(true);
    await page1.screenshot({ path: info.outputPath('messages.png'), fullPage: true });
    if (info.project.name === 'mobile') {
      const back = page1.getByRole('button', { name: 'Back to conversations' });
      const backBox = await back.boundingBox();
      const headerBox = await page1.getByRole('banner').boundingBox();
      expect(backBox!.y).toBeGreaterThanOrEqual(headerBox!.y + headerBox!.height);
      await back.click();
      await expect(page1.getByRole('heading', { name: 'Messages', exact: true })).toBeVisible();
    }
  } finally {
    await context1.close();
    await context2.close();
  }
});

test('a refresh token cannot create multiple sessions concurrently', async ({ request }, info) => {
  const user = await login(request, info.project.name === 'desktop' ? 'Player_0930' : 'Player_0932');
  const responses = await Promise.all(Array.from({ length: 5 }, () =>
    request.post(`${API}/api/auth/refresh`, { data: { token: user.refreshToken } })));
  expect(responses.filter(response => response.ok())).toHaveLength(1);
  expect(responses.filter(response => response.status() === 401)).toHaveLength(4);
});
