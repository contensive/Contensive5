const { chromium } = require('@playwright/test');

(async () => {
  const browser = await chromium.launch({ headless: false });
  const context = await browser.newContext({
    userAgent: 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/122.0.0.0 Safari/537.36',
    viewport: { width: 1280, height: 800 },
    ignoreHTTPSErrors: true,
  });

  const page = await context.newPage();

  await page.goto('https://sprint7.sitefpo.com/admin');

  // ✅ Wait for YOUR real input fields
  await page.waitForSelector('#inputUsername');

  await page.fill('#inputUsername', 'Test');
  await page.fill('#inputPassword', 'Admintest@1');

  await page.click('button[type="submit"]');

  // Wait for admin page after login
  await page.waitForURL('**/admin?addonGuid=**', { timeout: 60000 });

  await context.storageState({ path: 'admin-auth.json' });

  console.log('✅ Login session saved to admin-auth.json');
  await browser.close();
})();
