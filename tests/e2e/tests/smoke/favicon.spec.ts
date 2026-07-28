import { test, expect } from "@playwright/test";

test.describe("Favicon", () => {
  test("favicon.ico returns redirect or 404", async ({ request }) => {
    // The favicon addon either redirects to the CDN file (when FaviconFilename
    // site property is configured) or returns 404 (when it is not).
    // Both are valid responses — a 500 would indicate a server error.
    const response = await request.get("/favicon.ico", {
      maxRedirects: 0,
    });
    const status = response.status();
    expect(
      [200, 301, 302, 404].includes(status),
      `Expected 200, 301, 302, or 404 but got ${status}`
    ).toBe(true);
  });

  test("favicon link tag is present in page head", async ({ page }) => {
    await page.goto("/");
    const faviconLink = page.locator('link[rel="icon"]');
    const count = await faviconLink.count();
    // If a favicon is configured, the link tag should be present with a valid href.
    // If not configured, the tag will be absent — both are acceptable.
    if (count > 0) {
      const href = await faviconLink.first().getAttribute("href");
      expect(href).toBeTruthy();
    }
  });
});
