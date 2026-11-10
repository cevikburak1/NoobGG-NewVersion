import { demoUsername } from './demoAccounts';
import { expect, test } from '@playwright/test';

test('public layouts fit the viewport in both languages', async ({ page }, info) => {
  await page.emulateMedia({ reducedMotion: 'reduce' });
  for (const path of ['/', '/login', '/register']) {
    await page.goto(path);
    await expect(page.getByRole('heading', { level: 1 })).toBeVisible();
    expect(await page.evaluate(() => document.documentElement.scrollWidth)).toBeLessThanOrEqual(page.viewportSize()!.width);
    await page.getByRole('combobox', { name: 'Language / Dil' }).selectOption('en');
    await expect(page.locator('html')).toHaveAttribute('lang', 'en');
    await page.waitForTimeout(650); // Let the entrance animation settle for visual review.
    await page.screenshot({ path: info.outputPath(`public-${path.replaceAll('/', '') || 'landing'}.png`) });
    await page.getByRole('combobox', { name: 'Language / Dil' }).selectOption('tr');
  }
});

test('redesigned application layouts stay within the viewport', async ({ page }, info) => {
  test.skip(!process.env.E2E_API, 'Requires seeded development API');
  test.setTimeout(120_000);
  await page.emulateMedia({ reducedMotion: 'reduce' });
  await page.addInitScript(() => localStorage.setItem('noobgg_language', 'en'));
  await page.goto('/login');
  await page.getByLabel('Email or Username').fill(demoUsername(info.project.name === 'desktop' ? 'Player_0966' : 'Player_0967'));
  await page.getByLabel('Password', { exact: true }).fill('Test1234!');
  await page.getByRole('button', { name: 'Sign in', exact: true }).click();
  await expect(page).toHaveURL(/\/rooms$/);
  for (const path of ['/rooms', '/discover', '/guilds', '/community', '/messages', '/profile/edit', '/settings', '/subscriptions']) {
    await test.step(path, async () => {
      await page.evaluate(target => { history.pushState({}, '', target); window.dispatchEvent(new PopStateEvent('popstate')); window.scrollTo(0, 0); }, path);
      await expect(page.locator('main').getByRole('heading').first()).toBeVisible();
      await page.waitForTimeout(400); // The page entrance transition lasts 300ms.
      const overflow = await page.evaluate(() => [...document.querySelectorAll<HTMLElement>('main *')].filter(e => e.getBoundingClientRect().right > innerWidth + 1).slice(0, 10).map(e => [e.tagName, e.className]));
      await page.screenshot({ path: info.outputPath(`${path.replaceAll('/', '-')}.png`) });
      expect(await page.evaluate(() => document.documentElement.scrollWidth), `${path} ${JSON.stringify(overflow)}`).toBeLessThanOrEqual(page.viewportSize()!.width);
    });
  }
  if (info.project.name === 'mobile') {
    await page.getByRole('button', { name: 'More', exact: true }).click();
    const dialog = page.getByRole('dialog', { name: 'More destinations' });
    await expect(dialog).toBeVisible();
    await dialog.getByRole('link', { name: 'Settings', exact: true }).click();
    await expect(page).toHaveURL(/\/settings$/);
    await expect(dialog).toHaveCount(0);
    expect(await page.evaluate(() => document.body.style.overflow)).not.toBe('hidden');
  }
});

test('room artwork filters and create dialog remain functional', async ({ page }, info) => {
  test.skip(!process.env.E2E_API, 'Requires seeded development API');
  await page.emulateMedia({ reducedMotion: 'reduce' });
  await page.addInitScript(() => localStorage.setItem('noobgg_language', 'en'));
  await page.goto('/login');
  await page.getByLabel('Email or Username').fill(demoUsername(info.project.name === 'desktop' ? 'Player_0966' : 'Player_0967'));
  await page.getByLabel('Password', { exact: true }).fill('Test1234!');
  await page.getByRole('button', { name: 'Sign in', exact: true }).click();
  await expect(page).toHaveURL(/\/rooms$/);
  const game = page.locator('button[aria-pressed]').first();
  await expect(game).toBeVisible();
  await game.click();
  await expect(game).toHaveAttribute('aria-pressed', 'true');
  await page.getByRole('button', { name: /All games/ }).click();
  await expect(game).toHaveAttribute('aria-pressed', 'false');
  await page.getByRole('button', { name: 'Create Room', exact: true }).first().click();
  await expect(page.getByLabel('Room Title')).toBeVisible();
  await page.keyboard.press('Escape');
  await expect(page.getByLabel('Room Title')).toHaveCount(0);
});
