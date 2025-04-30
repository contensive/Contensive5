
const { test, expect } = require('@playwright/test');

test('basic page test', async ({ page }) => {
  // Navigate to a webpage
  await page.goto('https://example.com');

  // Check the title of the page
  await expect(page).toHaveTitle(/Example Domain/);

  // Take a screenshot
  await page.screenshot({ path: 'example.png' });
});
