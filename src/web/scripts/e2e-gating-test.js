const { chromium } = require('@playwright/test');

(async () => {
  const browser = await chromium.launch({ headless: true });
  const context = await browser.newContext();
  const page = await context.newPage();

  console.log('=== STARTING GATING VERIFICATION E2E TEST ===');

  page.on('console', msg => console.log(`[CONSOLE] ${msg.type()}: ${msg.text()}`));
  page.on('response', async res => {
    if (res.url().includes('/api/')) {
      let body = '';
      try { body = (await res.text()).substring(0, 150); } catch (e) {}
      console.log(`[HTTP ${res.status()}] ${res.request().method()} ${res.url()} -> ${body}`);
    }
  });

  // Step 1: Open Content Workspace & create a piece
  console.log('1. Navigating to Content Workspace...');
  await page.goto('http://localhost:4200/content/items', { waitUntil: 'networkidle' });

  const newPieceBtn = page.getByRole('button', { name: /Nueva Pieza/i });
  await newPieceBtn.click();
  await page.waitForTimeout(300);

  const titleInput = page.getByPlaceholder(/Ej:/i);
  await titleInput.fill('Gating Verification 404 Piece');

  const createBtn = page.getByRole('button', { name: /Crear y Continuar/i });
  await createBtn.click();
  await page.waitForTimeout(2000);

  console.log('2. Navigated to Content Detail page:', page.url());

  // Step 2: Attach a dead-link URL
  console.log('3. Attaching dead link URL evidence...');
  const attachBtn = page.getByRole('button', { name: /Adjuntar Evidencia/i });
  await attachBtn.click();
  await page.waitForTimeout(300);

  const evTitleInput = page.getByPlaceholder(/Ej: Informe de adopción/i);
  await evTitleInput.fill('Failed Dead Link Evidence');

  const urlInput = page.getByPlaceholder(/https:\/\/elpais\.com/i);
  await urlInput.fill('https://example.invalid/dead-link-404-simulation');

  const saveUrlBtn = page.getByRole('button', { name: /Adjuntar y Hashear/i });
  await saveUrlBtn.click();
  await page.waitForTimeout(3000);

  // Step 3: Verify Content Detail gating state with only CaptureFailed evidence
  console.log('4. Verifying Content Detail gating state...');
  const detailText = await page.evaluate(() => document.body.innerText);

  const hasCaptureFailed = /CaptureFailed/i.test(detailText);
  const hasGatingMsg = /Requiere al menos 1 evidencia capturada con éxito/i.test(detailText);
  console.log('   - Shows CaptureFailed status:', hasCaptureFailed);
  console.log('   - Shows gating explanation message:', hasGatingMsg);

  const aiDraftBtnDisabled = await page.locator('button:has-text("Generar Borrador con IA")').getAttribute('disabled');
  console.log('   - "Generar Borrador con IA" button disabled:', aiDraftBtnDisabled !== null);

  if (!hasGatingMsg || aiDraftBtnDisabled === null) {
    console.error('FAILED: Gating rule not enforced on ContentDetailComponent!');
    await browser.close();
    process.exit(1);
  }

  // Step 4: Check Review Studio gating state
  console.log('5. Navigating to Review Studio...');
  const studioLink = page.getByRole('link', { name: /Abrir TruthSource Review Studio/i });
  await studioLink.click();
  await page.waitForTimeout(1500);

  const studioText = await page.evaluate(() => document.body.innerText);
  const studioGatingMsg = /Requiere al menos 1 evidencia capturada con éxito/i.test(studioText);
  const studioAiBtnDisabled = await page.locator('button:has-text("Generar IA")').getAttribute('disabled');

  console.log('   - Studio shows gating explanation:', studioGatingMsg);
  console.log('   - Studio "Generar IA" button disabled:', studioAiBtnDisabled !== null);

  if (!studioGatingMsg || studioAiBtnDisabled === null) {
    console.error('FAILED: Gating rule not enforced on TruthSourceReviewStudioComponent!');
    await browser.close();
    process.exit(1);
  }

  // Step 5: Go back to Content Detail and attach valid context note
  console.log('6. Going back and attaching valid textual evidence...');
  const backLink = page.locator('a:has-text("Gating Verification 404 Piece")').first();
  await backLink.click();
  await page.waitForTimeout(1000);

  const attachBtn2 = page.getByRole('button', { name: /Adjuntar Evidencia/i });
  await attachBtn2.click();
  await page.waitForTimeout(300);

  // Switch to manual text note tab
  const noteTab = page.getByRole('button', { name: /Nota \/ Texto Directo/i });
  await noteTab.click();
  await page.waitForTimeout(200);

  const noteTitleInput = page.getByPlaceholder(/Ej: Informe de adopción/i);
  await noteTitleInput.fill('Valid Verified Context Note');

  const noteContentInput = page.getByPlaceholder(/Extractos textuales o notas directas/i);
  await noteContentInput.fill('Evidencia factual comprobada para habilitar síntesis de TruthSource.');

  const saveNoteBtn = page.getByRole('button', { name: /Adjuntar y Hashear/i });
  await saveNoteBtn.click();
  await page.waitForTimeout(2000);

  // Step 6: Verify "Generar Borrador con IA" becomes active
  console.log('7. Verifying "Generar Borrador con IA" is now enabled...');
  const updatedAiDraftBtnDisabled = await page.locator('button:has-text("Generar Borrador con IA")').getAttribute('disabled');
  console.log('   - Button disabled state:', updatedAiDraftBtnDisabled);

  if (updatedAiDraftBtnDisabled !== null) {
    console.error('FAILED: "Generar Borrador con IA" remained disabled after attaching valid evidence!');
    await browser.close();
    process.exit(1);
  }

  // Step 7: Click "Generar Borrador con IA" and verify synthesis succeeds
  console.log('8. Clicking "Generar Borrador con IA"...');
  await page.waitForTimeout(500);
  const aiDraftBtn = page.getByRole('button', { name: /Generar Borrador con IA/i });
  await aiDraftBtn.click();
  
  console.log('9. Waiting for navigation to TruthSource Review Studio...');
  await page.waitForURL(/\/truth-source$/, { timeout: 25000 });
  await page.waitForSelector('h1:has-text("TruthSource Review Studio")', { timeout: 10000 });

  console.log('10. Checking Review Studio URL after synthesis:', page.url());
  const finalStudioText = await page.evaluate(() => document.body.innerText);
  const hasDraftSummary = /Resumen Factual/i.test(finalStudioText);
  console.log('   - TruthSource draft summary rendered:', hasDraftSummary);

  if (!hasDraftSummary) {
    console.error('FAILED: TruthSource draft not rendered after synthesis!');
    await browser.close();
    process.exit(1);
  }

  console.log('\n=== GATING E2E TEST PASSED PERFECTLY ===');
  await browser.close();
})();
