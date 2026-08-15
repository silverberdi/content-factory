const { chromium } = require('@playwright/test');

(async () => {
  const browser = await chromium.launch({ headless: true });
  const context = await browser.newContext();
  const page = await context.newPage();

  console.log('=== ATTACHING EVENT LISTENERS ===');

  page.on('console', msg => {
    console.log(`[BROWSER CONSOLE] ${msg.type().toUpperCase()}: ${msg.text()}`);
  });

  page.on('pageerror', error => {
    console.log(`[BROWSER UNHANDLED ERROR]:`, error);
  });

  page.on('request', req => {
    console.log(`[NETWORK REQ] ${req.method()} ${req.url()}`);
  });

  page.on('response', async res => {
    let body = '';
    try {
      if (res.url().includes('/api/')) {
        body = await res.text();
      }
    } catch (e) {
      body = '<unable to read body>';
    }
    console.log(`[NETWORK RES] ${res.status()} ${res.url()} -> Body: ${body.substring(0, 300)}`);
  });

  page.on('requestfailed', req => {
    console.log(`[NETWORK FAILED] ${req.method()} ${req.url()} - ${req.failure()?.errorText}`);
  });

  console.log('=== NAVIGATING TO CONTENT WORKSPACE ===');
  await page.goto('http://localhost:4200/content/items', { waitUntil: 'networkidle' });
  console.log('Workspace loaded, URL:', page.url());

  console.log('=== CLICKING "Nueva Pieza" ===');
  const newPieceBtn = page.getByRole('button', { name: /Nueva Pieza/i });
  await newPieceBtn.click();
  await page.waitForTimeout(500);

  console.log('=== FILLING TITLE ===');
  const titleInput = page.getByPlaceholder(/Ej:/i);
  await titleInput.fill('Playwright Trace Test Piece');

  console.log('=== CLICKING "Crear y Continuar" ===');
  const createBtn = page.getByRole('button', { name: /Crear y Continuar/i });
  await createBtn.click();

  console.log('=== WAITING 4 SECONDS FOR DETAIL PAGE NAVIGATION & RENDER ===');
  await page.waitForTimeout(4000);

  console.log('=== FINAL PAGE STATE ===');
  console.log('Final URL:', page.url());
  const bodyText = await page.evaluate(() => document.body.innerText);
  console.log('Body Text Snippet:\n', bodyText.substring(0, 800));

  await browser.close();
})();
