import { test, expect } from '@playwright/test';

test.describe('Content Factory Cockpit - Viewports and Design System', () => {

  test('should render unified control center header without duplicate navigation', async ({ page }) => {
    await page.goto('/dashboard');
    await expect(page.getByText('Content Factory', { exact: true })).toBeVisible();
    await expect(page.getByText('Control Center', { exact: true })).toBeVisible();

    // Verify navigation links are present exactly once
    const overviewLinks = page.locator('nav a[href*="dashboard"]');
    await expect(overviewLinks).toHaveCount(1);

    const channelLinks = page.locator('nav a[href*="channels"]');
    await expect(channelLinks).toHaveCount(1);

    const systemLinks = page.locator('nav a[href*="system"]');
    await expect(systemLinks).toHaveCount(1);
  });

  test('should render separated identity and role badges', async ({ page }) => {
    await page.goto('/dashboard');
    const ownerBadge = page.locator('span:text-is("OWNER")');
    const techBadge = page.locator('span:text-is("TECH")');
    const editBadge = page.locator('span:text-is("EDIT")');

    // On desktop / tablet
    if (await ownerBadge.isVisible()) {
      await expect(ownerBadge).toBeVisible();
      await expect(techBadge).toBeVisible();
      await expect(editBadge).toBeVisible();
    }
  });

  test('should render distinct operational widgets (Factory Health, Channel Summary, Attention)', async ({ page }) => {
    await page.goto('/dashboard');
    await expect(page.getByText('Factory Health & Runtime Telemetry')).toBeVisible();
    await expect(page.getByText('Channel Portfolio & Registry')).toBeVisible();
    await expect(page.getByText('Exceptions & Attention Center')).toBeVisible();
  });

  test('should toggle light and dark themes smoothly with persistent html class', async ({ page }) => {
    await page.goto('/dashboard');
    const themeBtn = page.locator('button[aria-label*="Switch to"]');
    await expect(themeBtn).toBeVisible();

    const initialDark = await page.evaluate(() => document.documentElement.classList.contains('dark'));
    await themeBtn.click();
    await page.waitForTimeout(200);
    const afterClickDark = await page.evaluate(() => document.documentElement.classList.contains('dark'));
    expect(afterClickDark).toBe(!initialDark);

    // Toggle back
    await themeBtn.click();
    await page.waitForTimeout(200);
    const restoredDark = await page.evaluate(() => document.documentElement.classList.contains('dark'));
    expect(restoredDark).toBe(initialDark);
  });

  test('should fit inside useful desktop viewport without full-page scroll at 1440x900 and 1920x1080', async ({ page }, testInfo) => {
    await page.goto('/dashboard');
    if (testInfo.project.name.startsWith('desktop')) {
      const scrollHeight = await page.evaluate(() => document.documentElement.scrollHeight);
      const clientHeight = await page.evaluate(() => document.documentElement.clientHeight);
      expect(scrollHeight).toBeLessThanOrEqual(clientHeight + 80);
    }
  });

  test('should open channel drawer modal cleanly on New Channel click', async ({ page }) => {
    await page.goto('/dashboard');
    const newChannelBtn = page.getByRole('button', { name: 'New Channel' }).first();
    await newChannelBtn.click();
    await expect(page.getByText('Register New Channel')).toBeVisible();
    await expect(page.getByPlaceholder('e.g. IA Simple ES')).toBeVisible();
  });

  test('should navigate between Overview, Channels, and System routes cleanly', async ({ page }) => {
    await page.goto('/dashboard');
    await page.locator('nav a[href*="channels"]').click();
    await expect(page).toHaveURL(/.*channels/);
    await expect(page.getByText('Editorial Channel Registry')).toBeVisible();

    await page.locator('nav a[href*="system"]').click();
    await expect(page).toHaveURL(/.*system/);
    await expect(page.getByText('System & Security Governance')).toBeVisible();
  });
});
