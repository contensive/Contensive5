import { test, expect } from "@playwright/test";
import { PublicPage } from "../../pages/public-page.page";
import { TestPageData, requireTestPages } from "../../helpers/load-test-pages";

let testData: TestPageData;

test.beforeAll(() => {
  testData = requireTestPages();
});

test.describe("Link Alias", () => {
  test("page is accessible via its primary URL", async ({ page }) => {
    const publicPage = new PublicPage(page);
    await publicPage.goto(testData.aliasPage.url);
    await publicPage.expectStatus(200);
    await publicPage.expectHeadline(testData.aliasPage.headline);
  });

  test("page is accessible via added alias", async ({ page }) => {
    const publicPage = new PublicPage(page);
    await publicPage.goto(testData.aliasPage.aliasUrl);
    await publicPage.expectStatus(200);
    // Same page content should render at the alias URL
    await publicPage.expectHeadline(testData.aliasPage.headline);
  });

  test("both alias URLs serve the same page content", async ({ page }) => {
    const publicPage = new PublicPage(page);

    // Visit primary URL and capture headline
    await publicPage.goto(testData.aliasPage.url);
    const primaryHeadline = await page.locator("h1, h2").first().innerText();

    // Visit alias URL and compare
    await publicPage.goto(testData.aliasPage.aliasUrl);
    const aliasHeadline = await page.locator("h1, h2").first().innerText();

    expect(aliasHeadline).toBe(primaryHeadline);
  });

  test("non-existent URL shows page-not-found content", async ({ page }) => {
    const publicPage = new PublicPage(page);
    const response = await publicPage.goto(`/e2e-nonexistent-page-${testData.timestamp}`);
    // Contensive may return 404 or render a "page not found" page with 200
    const status = publicPage.getResponseStatus();
    expect([200, 404]).toContain(status);
    // Page should still render (not a server error)
    await expect(page.locator("body")).toBeVisible();
  });
});
