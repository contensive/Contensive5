import { Page, Locator, expect } from "@playwright/test";

/**
 * Page Object Model for the Contensive admin navigation sidebar.
 *
 * The admin site renders a navigation element with portal links
 * (Dashboard, Accounts, Content, etc.).
 */
export class AdminNavPage {
  readonly nav: Locator;
  readonly dashboardLink: Locator;
  constructor(private page: Page) {
    this.nav = page.locator("nav");
    this.dashboardLink = page.getByRole("link", { name: "Dashboard" });
  }

  /** Navigate to the admin root. */
  async goto() {
    await this.page.goto("/admin");
  }

  /** Assert the navigation sidebar is visible. */
  async expectNavVisible() {
    await expect(this.dashboardLink).toBeVisible();
  }

  /** Assert the main content area is visible. */
  async expectContentArea() {
    await expect(this.nav).toBeVisible();
  }

  /** Click a portal link by its visible text. */
  async clickPortal(name: string) {
    await this.nav.getByRole("link", { name }).click();
  }

  /** Navigate directly to a content list view by content GUID. */
  async gotoContentList(cguid: string) {
    await this.page.goto(`/admin?cguid=${encodeURIComponent(cguid)}&af=1`);
  }

  /** Navigate directly to edit a specific record by content GUID and record ID. */
  async gotoEditRecord(cguid: string, id: number) {
    await this.page.goto(`/admin?cguid=${encodeURIComponent(cguid)}&id=${id}&af=4`);
  }

  /** Navigate directly to add a new record by content GUID. */
  async gotoAddRecord(cguid: string) {
    await this.page.goto(`/admin?cguid=${encodeURIComponent(cguid)}&id=0&af=4`);
  }

  /** Logout by navigating to the logout URL. */
  async logout() {
    await this.page.goto("/admin?method=logout");
    await expect(this.page.locator(".ccLoginFormCon")).toBeVisible({ timeout: 10000 });
  }

  /** Assert the user is logged out (login form is visible). */
  async expectLoggedOut() {
    await expect(this.page.locator(".ccLoginFormCon")).toBeVisible();
  }
}
