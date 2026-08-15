const { chromium } = require('@playwright/test');

(async () => {
  const browser = await chromium.launch({ headless: true });
  const context = await browser.newContext();
  const page = await context.newPage();

  console.log('=== STARTING COMPLETE E2E VERIFICATION ===');

  let postResponse = null;
  let getDetailResponse = null;

  page.on('response', async res => {
    if (res.url().endsWith('/api/content-items') && res.request().method() === 'POST') {
      try {
        postResponse = { status: res.status(), body: await res.json() };
      } catch (e) {}
    }
    if (res.url().includes('/api/content-items/') && res.request().method() === 'GET' && !res.url().includes('/truth-source')) {
      try {
        getDetailResponse = { status: res.status(), body: await res.json() };
      } catch (e) {}
    }
  });

  // Step 1: Open Content Workspace
  console.log('1. Navigating to Content Workspace...');
  await page.goto('http://localhost:4200/content/items', { waitUntil: 'networkidle' });

  // Step 2: Click "Nueva Pieza"
  console.log('2. Opening Create Modal...');
  const newPieceBtn = page.getByRole('button', { name: /Nueva Pieza/i });
  await newPieceBtn.click();
  await page.waitForTimeout(500);

  // Step 3: Enter Title
  const uniqueTitle = 'Guia Definitiva de IA y Verificacion 2026';
  console.log('3. Entering title:', uniqueTitle);
  const titleInput = page.getByPlaceholder(/Ej:/i);
  await titleInput.fill(uniqueTitle);

  // Step 4: Click "Crear y Continuar"
  console.log('4. Clicking "Crear y Continuar"...');
  const createBtn = page.getByRole('button', { name: /Crear y Continuar/i });
  await createBtn.click();

  // Step 5: Wait for navigation and detail render
  console.log('5. Waiting for Content Detail render...');
  await page.waitForSelector('h1:has-text("' + uniqueTitle + '")', { timeout: 8000 });

  const detailUrl = page.url();
  console.log('-> Navigated to URL:', detailUrl);

  const detailText = await page.evaluate(() => document.body.innerText);
  console.log('-> Verifying Content Detail text assertions:');
  const hasTitle = detailText.includes(uniqueTitle);
  const hasStage = detailText.includes('DraftingEvidence') || detailText.includes('DRAFTINGEVIDENCE');
  const hasEvidenceBundle = detailText.includes('Bundle de Evidencias Capturadas');
  const hasTruthSourceSection = detailText.includes('TruthSource Factual');
  const hasNoEvidenceMsg = detailText.includes('No hay evidencias adjuntas a esta pieza');
  const hasAiDraftBtn = detailText.includes('Generar Borrador con IA');

  console.log('   - Title rendered:', hasTitle);
  console.log('   - Stage rendered:', hasStage);
  console.log('   - Evidence Bundle rendered:', hasEvidenceBundle);
  console.log('   - TruthSource section rendered:', hasTruthSourceSection);
  console.log('   - Empty evidence notice rendered:', hasNoEvidenceMsg);
  console.log('   - AI Draft button rendered:', hasAiDraftBtn);

  if (!hasTitle || !hasStage || !hasEvidenceBundle || !hasTruthSourceSection) {
    console.error('FAILED: Content Detail assertions not met.');
    await browser.close();
    process.exit(1);
  }

  // Step 6: Navigate into Review Studio
  console.log('6. Clicking "Abrir TruthSource Review Studio"...');
  const studioLink = page.getByRole('link', { name: /Abrir TruthSource Review Studio/i });
  await studioLink.click();

  await page.waitForSelector('h1:has-text("TruthSource Review Studio")', { timeout: 8000 });
  const studioUrl = page.url();
  console.log('-> Review Studio loaded at URL:', studioUrl);

  const studioText = await page.evaluate(() => document.body.innerText);
  const hasStudioTitle = /TruthSource Review Studio/i.test(studioText);
  const hasLeftPane = /Evidencias Capturadas/i.test(studioText);
  const hasRightPane = /Verdad Factual y Guardrails/i.test(studioText);
  const hasStudioDraftMsg = /Borrador de TruthSource no generado/i.test(studioText);

  console.log('   - Studio title rendered:', hasStudioTitle);
  console.log('   - Left pane rendered:', hasLeftPane);
  console.log('   - Right pane rendered:', hasRightPane);
  console.log('   - Draft prompt rendered:', hasStudioDraftMsg);

  if (!hasStudioTitle || !hasLeftPane || !hasRightPane || !hasStudioDraftMsg) {
    console.error('FAILED: Review Studio assertions not met.');
    await browser.close();
    process.exit(1);
  }

  console.log('\n=== E2E FLOW SUCCESSFUL ===');
  console.log('POST Response Status:', postResponse?.status, 'Created ID:', postResponse?.body?.id);
  console.log('GET Detail Response Status:', getDetailResponse?.status, 'Retrieved ID:', getDetailResponse?.body?.id);

  await browser.close();
})();
