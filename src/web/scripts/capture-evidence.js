const { chromium } = require('@playwright/test');
const path = require('path');

const artifactDir = '/Users/silveriobernal/.gemini/antigravity-ide/brain/45849823-289d-4086-af38-7524a7422026';

const viewports = [
  { name: 'desktop_1920x1080_dark', width: 1920, height: 1080, dark: true },
  { name: 'desktop_1920x1080_light', width: 1920, height: 1080, dark: false },
  { name: 'desktop_1440x900_dark', width: 1440, height: 900, dark: true },
  { name: 'tablet_768x1024_dark', width: 768, height: 1024, dark: true },
  { name: 'mobile_390x844_dark', width: 390, height: 844, dark: true },
];

async function capture() {
  const browser = await chromium.launch();
  
  for (const vp of viewports) {
    const context = await browser.newContext({
      viewport: { width: vp.width, height: vp.height }
    });
    const page = await context.newPage();

    // Canonical API state for screenshot evidence
    await page.route('**/api/identity/me', route => route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({
        id: '00000000-0000-0000-0000-000000000001',
        email: 'silverio.bernal@gmail.com',
        isOwner: true,
        isActive: true,
        roles: ['TECHNICAL', 'EDITORIAL'],
        createdAtUtc: '2026-08-14T20:00:00Z'
      })
    }));

    await page.route('**/api/dashboard/summary', route => route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({
        factoryHealth: {
          status: 'healthy',
          activeChannelsCount: 0,
          pilotChannelsCount: 1,
          totalChannelsCount: 1,
          databaseStatus: 'Connected (MySQL/content_factory_dev)',
          backupStatus: 'Not Configured (CF-001 Scope)',
          environment: 'Development'
        },
        channels: [
          {
            id: '00000000-0000-0000-0000-000000000010',
            slug: 'ia-simple-es',
            name: 'IA Simple ES',
            language: 'es',
            niche: 'AI tools and future of work for Spanish speakers',
            status: 'pilot',
            createdAtUtc: '2026-08-14T20:20:00Z',
            updatedAtUtc: '2026-08-14T20:20:00Z'
          }
        ],
        attentionItems: [
          {
            id: '11111111-1111-1111-1111-111111111111',
            severity: 'info',
            title: 'Pilot Channel Initialized',
            description: "Pilot channel 'IA Simple ES' is registered and awaiting editorial idea discovery.",
            actionPath: '/channels',
            isRepresentativeDemo: true,
            timestampUtc: '2026-08-14T20:20:00Z'
          },
          {
            id: '22222222-2222-2222-2222-222222222222',
            severity: 'warning',
            title: 'Channel Configuration Check',
            description: 'Verify target audience profile and language parameters for Spanish AI niche.',
            actionPath: '/channels',
            isRepresentativeDemo: true,
            timestampUtc: '2026-08-14T19:30:00Z'
          }
        ]
      })
    }));

    await page.goto('http://localhost:4300/dashboard');
    await page.waitForLoadState('networkidle');

    if (vp.dark) {
      await page.evaluate(() => {
        document.documentElement.classList.add('dark');
        localStorage.setItem('cf-theme-preference', 'dark');
      });
    } else {
      await page.evaluate(() => {
        document.documentElement.classList.remove('dark');
        localStorage.setItem('cf-theme-preference', 'light');
      });
    }
    await page.waitForTimeout(400);

    const screenshotPath = path.join(artifactDir, `${vp.name}.png`);
    await page.screenshot({ path: screenshotPath, fullPage: false });
    console.log(`Saved screenshot: ${screenshotPath}`);
    await context.close();
  }

  await browser.close();
}

capture().catch(err => {
  console.error(err);
  process.exit(1);
});
