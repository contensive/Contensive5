import { Page, Response, expect } from "@playwright/test";

/**
 * Page Object Model for public-facing Contensive pages.
 *
 * Used to navigate to public URLs and assert rendered content,
 * metadata, and HTTP response behavior.
 */
export class PublicPage {
  private lastResponse: Response | null = null;

  constructor(private page: Page) {}

  /** Navigate to a public URL and store the response. */
  async goto(url: string): Promise<Response | null> {
    this.lastResponse = await this.page.goto(url);
    return this.lastResponse;
  }

  /** Assert the HTTP response status from the last goto(). */
  async expectStatus(status: number) {
    expect(this.lastResponse).not.toBeNull();
    expect(this.lastResponse!.status()).toBe(status);
  }

  /** Assert the document title matches. */
  async expectTitle(title: string | RegExp) {
    await expect(this.page).toHaveTitle(title);
  }

  /** Assert the meta description content. */
  async expectMetaDescription(desc: string | RegExp) {
    const metaDesc = this.page.locator('meta[name="description"]');
    if (typeof desc === "string") {
      await expect(metaDesc).toHaveAttribute("content", desc);
    } else {
      const content = await metaDesc.getAttribute("content");
      expect(content).toMatch(desc);
    }
  }

  /** Assert the page headline text (looks for common heading elements). */
  async expectHeadline(text: string | RegExp) {
    const heading = this.page.locator("h1, h2").first();
    await expect(heading).toContainText(text);
  }

  /** Assert the page body contains the given text. */
  async expectBodyContains(text: string | RegExp) {
    if (typeof text === "string") {
      await expect(this.page.locator("body")).toContainText(text);
    } else {
      const bodyText = await this.page.locator("body").innerText();
      expect(bodyText).toMatch(text);
    }
  }

  /** Assert the canonical URL in link[rel=canonical]. */
  async expectCanonicalUrl(url: string | RegExp) {
    const link = this.page.locator('link[rel="canonical"]');
    if (typeof url === "string") {
      await expect(link).toHaveAttribute("href", url);
    } else {
      const href = await link.getAttribute("href");
      expect(href).toMatch(url);
    }
  }

  /** Return the HTTP status code from the last goto(). */
  getResponseStatus(): number {
    if (!this.lastResponse) {
      throw new Error("No response captured. Call goto() first.");
    }
    return this.lastResponse.status();
  }
}
