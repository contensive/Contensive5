const { chromium } = require('@playwright/test');

(async () => {
  const browser = await chromium.launch({ headless: false });
  const context = await browser.newContext({
    userAgent: 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/122.0.0.0 Safari/537.36',
    viewport: { width: 1280, height: 800 },
    ignoreHTTPSErrors: true,
  });

  const page = await context.newPage();

  try {
    console.log('🌐 Navigating to sprint9 admin login page...');
    await page.goto('https://sprint9.sitefpo.com/admin');

    console.log('⏳ Waiting for login form to load...');
    // Wait for the login form elements to be available
    await page.waitForSelector('#inputUsername', { timeout: 30000 });
    await page.waitForSelector('#inputPassword', { timeout: 30000 });

    console.log('📝 Filling in login credentials...');
    await page.fill('#inputUsername', 'testuser');
    await page.fill('#inputPassword', 'testPassword1234!');

    console.log('🔐 Submitting login form...');
    await page.click('button[type="submit"]');

    console.log('⏳ Waiting for successful login redirect...');
    // Wait for admin page after login - using a more flexible pattern
    await page.waitForURL('**/admin**', { timeout: 60000 });

    // Save the authentication state for reuse in other tests
    await context.storageState({ path: 'sprint9-admin-auth.json' });

    console.log('✅ Login successful! Session saved to sprint9-admin-auth.json');
    
    // Take a screenshot to verify login success
    await page.screenshot({ path: 'sprint9-login-success.png', fullPage: true });
    console.log('📸 Screenshot saved as sprint9-login-success.png');
    
  } catch (error) {
    console.error('❌ Login failed:', error.message);
    
    // Take a screenshot on failure for debugging
    await page.screenshot({ path: 'sprint9-login-failure.png', fullPage: true });
    console.log('📸 Failure screenshot saved as sprint9-login-failure.png');
    
    throw error;
  } finally {
    await browser.close();
  }
})();