import { test, expect } from '@playwright/test';

const channel = { id: '11111111-1111-1111-1111-111111111111', name: 'IA Simple ES', language: 'es', niche: 'Tecnología', status: 'Active' };
const analysis = {
  analysisId: '22222222-2222-2222-2222-222222222222', contentItemId: '22222222-2222-2222-2222-222222222222', channelId: channel.id,
  status: 'Completed', normalizedUrl: 'https://example.com/source', title: 'Fuente Mock verificable', language: 'es', textOrigin: 'Mock fixture',
  locator: 'https://example.com/source', provider: 'Mock Editorial Provider', isSimulated: true,
  draft: { id: 't', contentItemId: '22222222-2222-2222-2222-222222222222', status: 'Draft', summary: 'Resumen Mock para prueba de estados.', keyIdeas: ['Una idea verificable'], verifiableClaims: [], evidenceReferences: [], riskNotes: '', doNotSayConstraints: [], possibleAngles: ['Ángulo educativo'], localizationNotes: '', rejectionReason: null, rejectedAtUtc: null, rejectedByEmail: null, approvedAtUtc: null, approvedByEmail: null, version: 1, createdAtUtc: '', createdByEmail: 'mock@test', updatedAtUtc: '', updatedByEmail: 'mock@test' },
  findings: [{ id: '33333333-3333-3333-3333-333333333333', title: 'Falla de privacidad', editorialValue: 'La configuración requiere revisión.', evidenceId: '44444444-4444-4444-4444-444444444444', locator: 'https://example.com/source#evidence', contentType: 'Paraphrase', originalText: null, spanishTranslation: 'La configuración requiere revisión.', speaker: null, timestamp: null, reviewStatus: 'Verified' }], updatedAtUtc: ''
};

test.describe('Editorial Studio (explicit Mock fixture)', () => {
  test.beforeEach(async ({ page }) => {
    await page.route('**/api/channels', route => route.fulfill({ json: [channel] }));
    await page.route('**/api/editorial-url-analyses', async route => route.fulfill({ json: analysis }));
    await page.route('**/api/content-items/22222222-2222-2222-2222-222222222222', route => route.fulfill({ json: { id: analysis.contentItemId, channelId: channel.id, channelName: channel.name, title: analysis.title, slug: 'mock', stage: 'DraftingEvidence', status: 'Active', version: 1, createdAtUtc: '', createdByEmail: 'mock@test', updatedAtUtc: '', updatedByEmail: 'mock@test', evidences: [], truthSource: analysis.draft } }));
    await page.route('**/api/content-items/22222222-2222-2222-2222-222222222222/ideas', route => route.fulfill({ json: [] }));
    await page.route('**/api/content-items/22222222-2222-2222-2222-222222222222/script', route => route.fulfill({ status: 404, body: '{}' }));
  });

  test('navigates, analyzes, separates evidence, and keeps Mock provider visible', async ({ page }) => {
    await page.goto('/editorial/studio');
    await expect(page.getByRole('heading', { name: 'Editorial Studio' })).toBeVisible();
    await page.getByLabel('URL pública').fill('https://example.com/source');
    await page.getByRole('button', { name: 'Analizar fuente' }).click();
    await expect(page.getByText('Mock Editorial Provider · Mock')).toBeVisible();
    await expect(page.getByText('Falla de privacidad')).toBeVisible();
    await page.getByRole('button', { name: /Falla de privacidad/ }).click();
    await expect(page.getByText('Paráfrasis editorial')).toBeVisible();
    await expect(page.getByText('Verbatim verificado')).not.toBeVisible();
    await expect(page.getByText('Aprobar antes de generar ideas.')).toBeVisible();
  });

  test('supports the responsive workspace without horizontal overflow', async ({ page }) => {
    await page.goto('/editorial/studio');
    const overflow = await page.evaluate(() => document.documentElement.scrollWidth > document.documentElement.clientWidth);
    expect(overflow).toBe(false);
    const themeButton = page.locator('button[aria-label*="Switch to"]').first();
    const initialDark = await page.evaluate(() => document.documentElement.classList.contains('dark'));
    await themeButton.click();
    await expect.poll(() => page.evaluate(() => document.documentElement.classList.contains('dark'))).toBe(!initialDark);
    await themeButton.click();
    await expect.poll(() => page.evaluate(() => document.documentElement.classList.contains('dark'))).toBe(initialDark);
  });

  test('reuses the existing Storyboard route for downstream Visual and Voice studies', async ({ page }) => {
    await page.goto('/editorial/studio');
    await page.getByLabel('URL pública').fill('https://example.com/source');
    await page.getByRole('button', { name: 'Analizar fuente' }).click();
    const link = page.getByRole('link', { name: /Storyboard · Visual · Voice/ });
    await expect(link).toHaveAttribute('href', /\/content\/items\/.*\/storyboard/);
    await expect(page.getByText(/no afirma render, descarga ni publicación de un MP4/i)).toBeVisible();
  });

  test('persists selected idea and recovers the generated script after reload', async ({ page }) => {
    const selectedIdea = {
      id: '55555555-5555-5555-5555-555555555555', contentItemId: analysis.contentItemId,
      truthSourceId: 't', truthSourceVersionId: 'tv1', title: 'Video independiente', angle: 'Ángulo educativo',
      hookStrategy: 'Pregunta', audienceValue: 'Valor', format: 'Short', intendedOutcome: 'Aprender',
      freshnessClass: 'Evergreen', priority: 'Normal', rationale: 'Mock', status: 'Selected',
      version: 2, createdAtUtc: '', createdByEmail: 'mock@test', updatedAtUtc: '', updatedByEmail: 'mock@test'
    };
    const script = {
      id: '66666666-6666-6666-6666-666666666666', contentItemId: analysis.contentItemId, channelId: channel.id,
      contentIdeaId: selectedIdea.id, contentIdeaVersionId: selectedIdea.id, truthSourceId: 't', truthSourceVersionId: 'tv1',
      title: 'Guion persistido', targetDurationSeconds: 45, pacingWpm: 140, estimatedDurationSeconds: 42,
      totalWordCount: 98, language: 'es-ES', status: 'Draft', isStale: false, version: 1,
      createdAtUtc: '', createdByEmail: 'mock@test', updatedAtUtc: '', updatedByEmail: 'mock@test', scenes: []
    };
    await page.route('**/api/content-items/22222222-2222-2222-2222-222222222222/truth-source/approve', route => route.fulfill({ json: { ...analysis.draft, status: 'Approved' } }));
    await page.route('**/api/content-items/22222222-2222-2222-2222-222222222222/ideas/generate', route => route.fulfill({ json: [{ ...selectedIdea, status: 'Proposed' }] }));
    await page.route('**/api/content-items/22222222-2222-2222-2222-222222222222/ideas/*/select', route => route.fulfill({ json: selectedIdea }));
    await page.route('**/api/content-items/22222222-2222-2222-2222-222222222222/script/generate', route => route.fulfill({ json: script }));

    await page.goto('/editorial/studio');
    await page.getByLabel('URL pública').fill('https://example.com/source');
    await page.getByRole('button', { name: 'Analizar fuente' }).click();
    await page.getByRole('button', { name: 'Aprobar TruthSource' }).click();
    await page.getByRole('button', { name: 'Generar ideas' }).click();
    await page.route('**/api/content-items/22222222-2222-2222-2222-222222222222/ideas', route => route.fulfill({ json: [selectedIdea] }));
    await page.getByRole('button', { name: 'Crear este video' }).click();
    await expect(page.getByText('Seleccionada')).toBeVisible();
    await page.getByRole('button', { name: 'Generar guion' }).click();
    await expect(page.getByText('Guión · Draft')).toBeVisible();

    await page.route('**/api/editorial-url-analyses/22222222-2222-2222-2222-222222222222', route => route.fulfill({ json: { ...analysis, draft: { ...analysis.draft, status: 'Approved' } } }));
    await page.route('**/api/content-items/22222222-2222-2222-2222-222222222222/script', route => route.fulfill({ json: script }));
    await page.goto('/editorial/studio/22222222-2222-2222-2222-222222222222');
    await expect(page.getByText('Guión · Draft')).toBeVisible();
    await expect(page.getByText('Video independiente')).toBeVisible();
  });

  test("displays unsupported state when URL cannot be analyzed", async ({ page }) => {
    await page.route("**/api/editorial-url-analyses", route => route.fulfill({
      json: { ...analysis, status: "NotAnalyzable", findings: [] }
    }));
    await page.goto("/editorial/studio");
    await page.getByLabel("URL pública").fill("https://example.com/unsupported");
    await page.getByRole("button", { name: "Analizar fuente" }).click();
    await expect(page.getByText("Fuente no utilizable").first()).toBeVisible();
  });

  test("displays permission denied state on 403 error", async ({ page }) => {
    await page.route("**/api/editorial-url-analyses", route => route.fulfill({
      status: 403,
      json: { message: "No tiene permiso para analizar fuentes editoriales." }
    }));
    await page.goto("/editorial/studio");
    await page.getByLabel("URL pública").fill("https://example.com/forbidden");
    await page.getByRole("button", { name: "Analizar fuente" }).click();
    await expect(page.getByText("Sin permiso").first()).toBeVisible();
    await expect(page.getByText("No tiene permiso para analizar fuentes editoriales.")).toBeVisible();
  });

  test("proves two proposals can be selected independently without overwriting source context or other proposals", async ({ page }) => {
    let idea1 = {
      id: "55555555-5555-5555-5555-555555555551", contentItemId: analysis.contentItemId,
      truthSourceId: "t", truthSourceVersionId: "tv1", title: "Ángulo Educativo", angle: "Explicación detallada",
      hookStrategy: "Pregunta", audienceValue: "Valor 1", format: "Short", intendedOutcome: "Aprender",
      freshnessClass: "Evergreen", priority: "Normal", rationale: "Mock 1", status: "Proposed",
      version: 1, createdAtUtc: "", createdByEmail: "mock@test", updatedAtUtc: "", updatedByEmail: "mock@test"
    };
    let idea2 = {
      id: "55555555-5555-5555-5555-555555555552", contentItemId: analysis.contentItemId,
      truthSourceId: "t", truthSourceVersionId: "tv1", title: "Ángulo Polémico", angle: "Debate abierto",
      hookStrategy: "Controversia", audienceValue: "Valor 2", format: "Short", intendedOutcome: "Opinar",
      freshnessClass: "Evergreen", priority: "Normal", rationale: "Mock 2", status: "Proposed",
      version: 1, createdAtUtc: "", createdByEmail: "mock@test", updatedAtUtc: "", updatedByEmail: "mock@test"
    };

    let ideasList = [idea1, idea2];
    await page.route("**/api/content-items/22222222-2222-2222-2222-222222222222/truth-source/approve", route => route.fulfill({ json: { ...analysis.draft, status: "Approved" } }));
    await page.route("**/api/content-items/22222222-2222-2222-2222-222222222222/ideas/generate", route => route.fulfill({ json: ideasList }));
    await page.route("**/api/content-items/22222222-2222-2222-2222-222222222222/ideas", route => route.fulfill({ json: ideasList }));
    await page.route("**/api/content-items/22222222-2222-2222-2222-222222222222/ideas/" + idea1.id + "/select", route => {
      idea1 = { ...idea1, status: "Selected", version: 2 };
      ideasList = [idea1, idea2];
      return route.fulfill({ json: idea1 });
    });
    await page.route("**/api/content-items/22222222-2222-2222-2222-222222222222/ideas/" + idea2.id + "/select", route => {
      idea2 = { ...idea2, status: "Selected", version: 2 };
      ideasList = [{ ...idea1, status: "Proposed" }, idea2];
      return route.fulfill({ json: idea2 });
    });

    await page.goto("/editorial/studio");
    await page.getByLabel("URL pública").fill("https://example.com/source");
    await page.getByRole("button", { name: "Analizar fuente" }).click();
    await page.getByRole("button", { name: "Aprobar TruthSource" }).click();
    await page.getByRole("button", { name: "Generar ideas" }).click();

    await expect(page.getByRole("heading", { name: "Ángulo Educativo" })).toBeVisible();
    await expect(page.getByRole("heading", { name: "Ángulo Polémico" })).toBeVisible();

    await page.locator("article.proposal-card").filter({ hasText: "Ángulo Educativo" }).getByRole("button", { name: "Crear este video" }).click();
    await expect(page.locator("article.proposal-card").filter({ hasText: "Ángulo Educativo" }).getByRole("button", { name: "Seleccionada" })).toBeVisible();
    await expect(page.locator("article.proposal-card").filter({ hasText: "Ángulo Polémico" }).getByRole("button", { name: "Crear este video" })).toBeVisible();

    await expect(page.getByText("Falla de privacidad")).toBeVisible();
  });
});