import { test, expect } from "@playwright/test";

test.describe("Homepage", () => {
  test("responds with 200 and has a title", async ({ page }) => {
    const response = await page.goto("/");
    expect(response?.status()).toBe(200);
    await expect(page).toHaveTitle(/.+/);
  });

  test("renders page content", async ({ page }) => {
    await page.goto("/");
    // The page body should have visible content (not a blank page)
    const body = page.locator("body");
    await expect(body).not.toBeEmpty();
  });
});
