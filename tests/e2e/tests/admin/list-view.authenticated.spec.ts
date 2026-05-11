import { test, expect } from "@playwright/test";
import { ListViewPage } from "../../pages/list-view.page";
import { EditViewPage } from "../../pages/edit-view.page";

const contentGuid = process.env.TEST_CONTENT_GUID || "";

test.describe("List View", () => {
  test.beforeEach(({ }, testInfo) => {
    test.skip(!contentGuid, "TEST_CONTENT_GUID not set");
  });

  test("list view renders with data rows", async ({ page }) => {
    const listView = new ListViewPage(page);
    await listView.goto(contentGuid);
    await listView.expectListVisible();
    const rowCount = await listView.getRowCount();
    expect(rowCount).toBeGreaterThan(0);
  });

  test("record count is displayed", async ({ page }) => {
    const listView = new ListViewPage(page);
    await listView.goto(contentGuid);
    const text = await listView.getRecordCountText();
    expect(text).toMatch(/\d+.*records?\s+found/i);
  });

  test("clicking Add navigates to edit form", async ({ page }) => {
    const listView = new ListViewPage(page);
    await listView.goto(contentGuid);
    await listView.clickAdd();
    const editView = new EditViewPage(page);
    await editView.expectEditVisible();
  });

  test("Refresh button reloads the list", async ({ page }) => {
    const listView = new ListViewPage(page);
    await listView.goto(contentGuid);
    await listView.expectListVisible();
    await listView.clickRefresh();
    await listView.expectListVisible();
  });
});
