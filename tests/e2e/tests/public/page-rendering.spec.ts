import { test, expect } from "@playwright/test";
import { PublicPage } from "../../pages/public-page.page";
import { TestPageData, requireTestPages } from "../../helpers/load-test-pages";

let testData: TestPageData;

test.beforeAll(() => {
  testData = requireTestPages();
});

test.describe("Page Rendering", () => {
  test("page renders with correct headline", async ({ page }) => {
    const publicPage = new PublicPage(page);
    await publicPage.goto(testData.basicPage.url);
    await publicPage.expectHeadline(testData.basicPage.headline);
  });

  test("page renders within a template", async ({ page }) => {
    const publicPage = new PublicPage(page);
    await publicPage.goto(testData.basicPage.url);
    // Template wraps content in full HTML structure
    await expect(page.locator("html")).toBeVisible();
    await expect(page.locator("head")).toBeAttached();
    await expect(page.locator("body")).toBeVisible();
  });

  test("page returns 200 status", async ({ page }) => {
    const publicPage = new PublicPage(page);
    await publicPage.goto(testData.basicPage.url);
    await publicPage.expectStatus(200);
  });

  test("root page renders", async ({ page }) => {
    const publicPage = new PublicPage(page);
    await publicPage.goto("/");
    await publicPage.expectStatus(200);
    await expect(page.locator("body")).toBeVisible();
  });
});
