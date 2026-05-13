import { test, expect } from "@playwright/test";
import { LoginPage } from "../../pages/login.page";

test.describe("Protected Access", () => {
  test("/admin shows login form for unauthenticated user", async ({ page }) => {
    const loginPage = new LoginPage(page);
    await loginPage.goto();
    await loginPage.expectLoginFormVisible();
  });

  test("/admin returns 200 with login form", async ({ page }) => {
    const response = await page.goto("/admin");
    expect(response?.status()).toBe(200);
    const loginPage = new LoginPage(page);
    await loginPage.expectLoginFormVisible();
  });
});
