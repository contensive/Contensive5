import { test as setup, expect } from "@playwright/test";
import { LoginPage } from "../pages/login.page";
import fs from "fs";
import path from "path";

const authFile = ".auth/user.json";

/**
 * Authentication setup — runs once before all tests.
 * Logs in with TEST_USERNAME / TEST_PASSWORD and saves the session state
 * so all test projects can reuse the authenticated session.
 */
setup("authenticate", async ({ page }) => {
  // Ensure the .auth directory exists before saving state
  const authDir = path.dirname(authFile);
  if (!fs.existsSync(authDir)) {
    fs.mkdirSync(authDir, { recursive: true });
  }

  const loginPage = new LoginPage(page);
  await loginPage.goto();

  const username = process.env.TEST_USERNAME || "";
  const password = process.env.TEST_PASSWORD || "";

  if (!username || !password) {
    throw new Error(
      "TEST_USERNAME and TEST_PASSWORD environment variables are required. " +
      "Set them in tests/e2e/.env or export them before running tests."
    );
  }

  await loginPage.login(username, password);
  await loginPage.expectLoginSuccess();

  // Save the authenticated session state for reuse by all test projects
  await page.context().storageState({ path: authFile });
});
