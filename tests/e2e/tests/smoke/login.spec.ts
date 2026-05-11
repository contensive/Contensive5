import { test, expect } from "@playwright/test";
import { LoginPage } from "../../pages/login.page";

test.describe("Login", () => {
  test("login page renders the form", async ({ page }) => {
    const loginPage = new LoginPage(page);
    await loginPage.goto();
    await loginPage.expectLoginFormVisible();
  });

  test("valid credentials log in successfully", async ({ page }) => {
    const loginPage = new LoginPage(page);
    await loginPage.goto();

    const username = process.env.TEST_USERNAME || "";
    const password = process.env.TEST_PASSWORD || "";

    await loginPage.login(username, password);
    await loginPage.expectLoginSuccess();
  });

  test("invalid credentials keep the form visible", async ({ page }) => {
    const loginPage = new LoginPage(page);
    await loginPage.goto();

    await loginPage.login("invalid-user-e2e-test", "invalid-password-e2e-test");
    await loginPage.expectLoginFormVisible();
  });
});
