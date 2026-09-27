import { expect, test } from '@playwright/test';
import { PNG } from 'pngjs';

function visiblePixels(buffer: Buffer): number {
  const image = PNG.sync.read(buffer);
  let visible = 0;
  for (let index = 3; index < image.data.length; index += 4) {
    if (image.data[index] > 20) visible++;
  }
  return visible;
}

test('landing scene renders on desktop and mobile', async ({ page }, testInfo) => {
  await page.goto('/');
  await expect(page.getByRole('heading', { name: 'NoobGg' })).toBeVisible();
  const canvas = page.locator('canvas').first();
  await expect(canvas).toBeVisible();
  await page.waitForTimeout(700);
  const first = await canvas.screenshot();
  expect(visiblePixels(first)).toBeGreaterThan(500);
  await page.screenshot({ path: testInfo.outputPath('landing.png'), fullPage: true });
  await page.waitForTimeout(700);
  const second = await canvas.screenshot();
  expect(first.equals(second)).toBe(false);
});

test('reduced motion freezes scene without blocking controls', async ({ page }) => {
  await page.emulateMedia({ reducedMotion: 'reduce' });
  await page.goto('/');
  const canvas = page.locator('canvas').first();
  await expect(canvas).toBeVisible();
  await expect(canvas).toHaveAttribute('data-motion', 'paused');
  await page.getByRole('link', { name: /giriş yap|sign in/i }).click();
  await expect(page).toHaveURL(/\/login$/);
});

test('static scene appears when WebGL is unavailable', async ({ page }) => {
  await page.addInitScript(() => {
    const getContext = HTMLCanvasElement.prototype.getContext;
    HTMLCanvasElement.prototype.getContext = function (kind, ...args) {
      if (kind === 'webgl' || kind === 'webgl2' || kind === 'experimental-webgl') return null;
      return getContext.call(this, kind, ...args);
    } as typeof HTMLCanvasElement.prototype.getContext;
  });
  await page.goto('/');
  await expect(page.getByTestId('scene-fallback')).toBeVisible();
  await expect(page.getByRole('heading', { name: 'NoobGg' })).toBeVisible();
});
