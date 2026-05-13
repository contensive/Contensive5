import { test, expect } from "@playwright/test";
import { PublicPage } from "../../pages/public-page.page";
import { TestPageData, requireTestPages } from "../../helpers/load-test-pages";

let testData: TestPageData;

test.beforeAll(() => {
  testData = requireTestPages();
});

test.describe("Page Hierarchy", () => {
  test("child page URL includes parent path", async ({ page }) => {
    // The child page URL should start with the basic page URL (its parent)
    expect(testData.childPage.url).toContain(
      testData.basicPage.url.replace(/\/$/, "")
    );
  });

  test("child page renders independently", async ({ page }) => {
    const publicPage = new PublicPage(page);
    await publicPage.goto(testData.childPage.url);
    await publicPage.expectStatus(200);
    await publicPage.expectHeadline(testData.childPage.headline);
  });

  test("parent page renders independently", async ({ page }) => {
    const publicPage = new PublicPage(page);
    await publicPage.goto(testData.basicPage.url);
    await publicPage.expectStatus(200);
    await publicPage.expectHeadline(testData.basicPage.headline);
  });
});
