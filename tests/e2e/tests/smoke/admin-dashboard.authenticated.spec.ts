import { test, expect } from "@playwright/test";

/**
 * These tests run with the authenticated session from auth.setup.ts.
 * They verify that an authenticated admin user can access the admin area.
 */
test.describe("Admin Dashboard", () => {
  test("admin area is accessible", async ({ page }) => {
    const response = await page.goto("/admin");
    expect(response?.status()).toBe(200);
  });

  test("admin area renders navigation", async ({ page }) => {
    await page.goto("/admin");
    // The Contensive admin site renders a navigation area.
    // Look for common admin UI indicators.
    const body = page.locator("body");
    await expect(body).not.toBeEmpty();
    // Verify the page is not the login form (we should be authenticated)
    await expect(page.locator(".ccLoginFormCon")).not.toBeVisible({ timeout: 5000 });
  });
});
