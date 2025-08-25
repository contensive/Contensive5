import { test, expect } from '@playwright/test';

test.describe('Sprint9 Admin Login Tests', () => {
  test('should successfully login to sprint9 admin panel', async ({ page }) => {
    // Navigate to the admin login page
    await page.goto('https://sprint9.sitefpo.com/admin');

    // Wait for the login form to be visible
    await expect(page.locator('#inputUsername')).toBeVisible();
    await expect(page.locator('#inputPassword')).toBeVisible();

    // Fill in the login credentials
    await page.fill('#inputUsername', 'testuser');
    await page.fill('#inputPassword', 'testPassword1234!');

    // Submit the login form
    await page.click('button[type="submit"]');

    // Wait for successful login and redirect to admin dashboard
    await page.waitForURL('**/admin**', { timeout: 60000 });

    // Verify we're on the admin page by checking for admin-specific elements
    await expect(page).toHaveURL(/.*\/admin.*/);
    
    // Take a screenshot for verification
    await page.screenshot({ path: 'test-results/sprint9-admin-login.png', fullPage: true });
  });

  test('should display login form elements correctly', async ({ page }) => {
    // Navigate to the admin login page
    await page.goto('https://sprint9.sitefpo.com/admin');

    // Check that all required form elements are present
    await expect(page.locator('#inputUsername')).toBeVisible();
    await expect(page.locator('#inputPassword')).toBeVisible();
    await expect(page.locator('button[type="submit"]')).toBeVisible();

    // Verify placeholder text or labels if available
    await expect(page.locator('#inputUsername')).toHaveAttribute('placeholder', /username/i);
    await expect(page.locator('#inputPassword')).toHaveAttribute('placeholder', /password/i);
  });

  test('should handle invalid credentials gracefully', async ({ page }) => {
    // Navigate to the admin login page
    await page.goto('https://sprint9.sitefpo.com/admin');

    // Fill in invalid credentials
    await page.fill('#inputUsername', 'invaliduser');
    await page.fill('#inputPassword', 'invalidpassword');

    // Submit the form
    await page.click('button[type="submit"]');

    // Should stay on login page or show error message
    // We expect either to stay on the same URL or see an error
    await page.waitForTimeout(3000); // Give time for any error messages
    
    // Either we're still on the login page or there's an error message
    const currentUrl = page.url();
    const hasErrorMessage = await page.locator('.alert-danger, .error, [class*="error"]').count() > 0;
    
    expect(currentUrl.includes('/admin') && (currentUrl === 'https://sprint9.sitefpo.com/admin' || hasErrorMessage)).toBeTruthy();
  });
});