import { test, expect } from "@playwright/test";
import { PublicPage } from "../../pages/public-page.page";
import { TestPageData, requireTestPages } from "../../helpers/load-test-pages";

let testData: TestPageData;

test.beforeAll(() => {
  testData = requireTestPages();
});

test.describe("Page Metadata", () => {
  test("page title renders in document.title", async ({ page }) => {
    const publicPage = new PublicPage(page);
    await publicPage.goto(testData.basicPage.url);
    await publicPage.expectTitle(new RegExp(testData.basicPage.pageTitle));
  });

  test("meta description renders", async ({ page }) => {
    const publicPage = new PublicPage(page);
    await publicPage.goto(testData.basicPage.url);
    await publicPage.expectMetaDescription(testData.basicPage.metaDescription);
  });

  test("page without explicit title uses fallback", async ({ page }) => {
    const publicPage = new PublicPage(page);
    await publicPage.goto(testData.noTitlePage.url);
    // When pageTitle is empty, Contensive falls back to the page name or site name
    // The title should not be empty
    const title = await page.title();
    expect(title.length).toBeGreaterThan(0);
  });
});
