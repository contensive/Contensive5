import { test, expect } from "@playwright/test";
import { LoginPage } from "../../pages/login.page";

test.describe("Session Persistence", () => {
  test("session persists across page reload", async ({ page }) => {
    const loginPage = new LoginPage(page);
    await page.goto("/admin");
    await loginPage.expectLoginSuccess();

    await page.reload();
    await loginPage.expectLoginSuccess();
  });

  test("session persists across navigation", async ({ page }) => {
    const loginPage = new LoginPage(page);
    await page.goto("/admin");
    await loginPage.expectLoginSuccess();

    // Navigate away and back
    await page.goto("/");
    await page.goto("/admin");
    await loginPage.expectLoginSuccess();
  });

  test("clearing cookies requires re-authentication", async ({ page, context }) => {
    const loginPage = new LoginPage(page);
    await page.goto("/admin");
    await loginPage.expectLoginSuccess();

    // Clear all cookies
    await context.clearCookies();
    await page.reload();
    await loginPage.expectLoginFormVisible();
  });
});
