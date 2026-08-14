import { test, expect } from '@playwright/test';

test.describe('Content Factory - Discovery Triage & Source Registry', () => {

  test('should render Discovery navigation links in control center header', async ({ page }) => {
    await page.goto('/dashboard');
    const triageLink = page.locator('nav a[href*="discovery/triage"]');
    await expect(triageLink).toBeVisible();

    const sourcesLink = page.locator('nav a[href*="discovery/sources"]');
    await expect(sourcesLink).toBeVisible();
  });

  test('should render Discovery Summary widget on dashboard with Quick Submit', async ({ page }) => {
    await page.goto('/dashboard');
    await expect(page.getByText('Discovery & Ingesta')).toBeVisible();
    await expect(page.getByText('Triage Pendiente')).toBeVisible();

    const quickSubmitBtn = page.getByRole('button', { name: 'Quick Submit' }).first();
    await expect(quickSubmitBtn).toBeVisible();
  });

  test('should open Quick Submit modal and display required fields', async ({ page }) => {
    await page.goto('/dashboard');
    const quickSubmitBtn = page.getByRole('button', { name: 'Quick Submit' }).first();
    await quickSubmitBtn.click();

    await expect(page.getByText('Add a URL or note for discovery.')).toBeVisible();
    await expect(page.getByPlaceholder('https://...')).toBeVisible();
    await expect(page.getByPlaceholder('Ej: Nuevo avance en agentes autónomos para pymes')).toBeVisible();
    await expect(page.getByRole('button', { name: 'Enviar a Triage' })).toBeVisible();
  });

  test('should navigate to Discovery Triage and render operational filters', async ({ page }) => {
    await page.goto('/discovery/triage');
    await expect(page.getByText('Pendientes Triage')).toBeVisible();
    await expect(page.getByText('Promovidos')).toBeVisible();
    await expect(page.getByText('Descartados')).toBeVisible();
    await expect(page.getByText('Fuentes Activas')).toBeVisible();

    // Verify filter pills
    await expect(page.getByRole('button', { name: /Pendientes/ })).toBeVisible();
    await expect(page.getByRole('button', { name: /Promovidos/ })).toBeVisible();
    await expect(page.getByRole('button', { name: /Descartados/ })).toBeVisible();
  });

  test('should navigate to Discovery Sources catalog and show source table', async ({ page }) => {
    await page.goto('/discovery/sources');
    await expect(page.getByText('Catálogo de Fuentes de Discovery')).toBeVisible();
    await expect(page.getByRole('button', { name: 'Nueva Fuente' })).toBeVisible();

    // Open source creation drawer
    await page.getByRole('button', { name: 'Nueva Fuente' }).click();
    await expect(page.getByText('Registrar Nueva Fuente')).toBeVisible();
    await expect(page.getByPlaceholder('Ej: Xataka Inteligencia Artificial')).toBeVisible();
  });
});
