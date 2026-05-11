import { test, expect } from "@playwright/test";
import { LoginPage } from "../../pages/login.page";

test.describe("Login Errors", () => {
  test("shows error for invalid credentials", async ({ page }) => {
    const loginPage = new LoginPage(page);
    await loginPage.goto();
    await loginPage.login("invalid-e2e-user", "invalid-e2e-pass");
    // Server has a 3-second delay on failed login
    await loginPage.expectErrorVisible();
  });

  test("login form stays visible after failure", async ({ page }) => {
    const loginPage = new LoginPage(page);
    await loginPage.goto();
    await loginPage.login("invalid-e2e-user", "invalid-e2e-pass");
    await loginPage.expectLoginFormVisible();
  });

  test("error clears on successful login", async ({ page }) => {
    const loginPage = new LoginPage(page);
    await loginPage.goto();

    // First, trigger an error
    await loginPage.login("invalid-e2e-user", "invalid-e2e-pass");
    await loginPage.expectErrorVisible();

    // Then login with valid credentials
    const username = process.env.TEST_USERNAME || "";
    const password = process.env.TEST_PASSWORD || "";
    await loginPage.login(username, password);
    await loginPage.expectLoginSuccess();
  });
});
