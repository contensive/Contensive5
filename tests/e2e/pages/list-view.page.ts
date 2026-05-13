import { Page, Locator, expect } from "@playwright/test";

/**
 * Page Object Model for the Contensive admin list/index view.
 *
 * The list view renders with a form (name="adminForm"), grid rows,
 * and action buttons (Add, Refresh, Delete, etc.).
 * The Refresh button is used as the primary visibility indicator
 * because the form element may not be exposed in the accessibility tree.
 */
export class ListViewPage {
  readonly refreshButton: Locator;
  readonly gridRows: Locator;
  readonly paginationContainer: Locator;
  readonly paginationLinks: Locator;
  readonly currentPageIndicator: Locator;

  constructor(private page: Page) {
    this.refreshButton = page.locator("input[value='Refresh']").first();
    // Data rows contain an edit link with af=4 in the URL
    this.gridRows = page.locator("tr:has(a[href*='af=4'])");
    this.paginationContainer = page.locator(".ccJumpCon");
    this.paginationLinks = page.locator(".paginationLink");
    this.currentPageIndicator = page.locator(".paginationLink.hit");
  }

  /** Navigate to the list view for a content definition by GUID. */
  async goto(cguid: string) {
    await this.page.goto(`/admin?cguid=${encodeURIComponent(cguid)}&af=1`);
  }

  /** Assert the list view is rendered. */
  async expectListVisible() {
    await expect(this.refreshButton).toBeVisible();
  }

  /** Get the count of visible data rows. */
  async getRowCount(): Promise<number> {
    return await this.gridRows.count();
  }

  /** Click the Add button to create a new record. */
  async clickAdd() {
    await this.page.locator("input[value='Add']").first().click();
  }

  /** Click a grid row's edit link by index (0-based) to open that record. */
  async clickRow(index: number) {
    const editLink = this.gridRows.nth(index).locator("a[href*='af=4']");
    await editLink.click();
    await this.page.waitForURL(/af=4/);
  }

  /** Click the Refresh button. */
  async clickRefresh() {
    await this.page.locator("input[value='Refresh']").first().click();
  }

  /** Click a pagination page number. */
  async clickPage(pageNumber: number) {
    await this.paginationLinks.filter({ hasText: String(pageNumber) }).click();
  }

  /** Assert the current active page number. */
  async expectPageActive(pageNumber: number) {
    await expect(this.currentPageIndicator).toHaveText(String(pageNumber));
  }

  /** Get the record count text (e.g., "1 record found" or "123 records found"). */
  async getRecordCountText(): Promise<string> {
    const item = this.page.locator("li").filter({ hasText: /records?\s+found/i }).first();
    return await item.innerText();
  }
}
