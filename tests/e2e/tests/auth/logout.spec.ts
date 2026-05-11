import { test, expect } from "@playwright/test";
import { LoginPage } from "../../pages/login.page";

/**
 * Logout tests perform their own login rather than using the shared
 * authenticated session, because logout invalidates the server-side session
 * which would break other authenticated tests that share the same cookies.
 */
test.describe("Logout", () => {
  const username = process.env.TEST_USERNAME || "";
  const password = process.env.TEST_PASSWORD || "";

  test.beforeEach(({ }, testInfo) => {
    test.skip(!username || !password, "TEST_USERNAME and TEST_PASSWORD required");
  });

  test("logout shows login form", async ({ page }) => {
    // Login first
    const loginPage = new LoginPage(page);
    await loginPage.goto();
    await loginPage.login(username, password);
    await loginPage.expectLoginSuccess();

    // Logout
    await page.goto("/admin?method=logout");
    await loginPage.expectLoginFormVisible();
  });

  test("after logout, admin page requires login", async ({ page }) => {
    // Login first
    const loginPage = new LoginPage(page);
    await loginPage.goto();
    await loginPage.login(username, password);
    await loginPage.expectLoginSuccess();

    // Logout
    await page.goto("/admin?method=logout");
    await loginPage.expectLoginFormVisible();

    // Navigate to /admin again — should still show login form
    await page.goto("/admin");
    await loginPage.expectLoginFormVisible();
  });
});
