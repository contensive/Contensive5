import { test, expect } from "@playwright/test";
import { EditViewPage } from "../../pages/edit-view.page";
import { ListViewPage } from "../../pages/list-view.page";

const contentGuid = process.env.TEST_CONTENT_GUID || "";

test.describe("Edit View", () => {
  test.beforeEach(({ }, testInfo) => {
    test.skip(!contentGuid, "TEST_CONTENT_GUID not set");
  });

  test("edit form renders with field labels", async ({ page }) => {
    // Navigate to list, then click the first row to get a valid record
    const listView = new ListViewPage(page);
    await listView.goto(contentGuid);
    await listView.clickRow(0);
    const editView = new EditViewPage(page);
    await editView.expectEditVisible();
  });

  test("add new record form renders", async ({ page }) => {
    const editView = new EditViewPage(page);
    await editView.gotoNew(contentGuid);
    await editView.expectEditVisible();
  });

  test("cancel returns to list without saving", async ({ page }) => {
    // Open a record for editing
    const listView = new ListViewPage(page);
    await listView.goto(contentGuid);
    await listView.clickRow(0);
    // Cancel the edit
    const editView = new EditViewPage(page);
    await editView.expectEditVisible();
    await editView.clickCancel();
    // Should return to list view
    await listView.expectListVisible();
  });
});
