import { test, expect } from "@playwright/test";
import { AdminNavPage } from "../../pages/admin-nav.page";
import { ListViewPage } from "../../pages/list-view.page";

const contentGuid = process.env.TEST_CONTENT_GUID || "";

test.describe("Admin Navigation", () => {
  let adminNav: AdminNavPage;

  test.beforeEach(async ({ page }) => {
    adminNav = new AdminNavPage(page);
    await adminNav.goto();
  });

  test("admin sidebar renders", async () => {
    await adminNav.expectNavVisible();
  });

  test("admin content area renders", async () => {
    await adminNav.expectContentArea();
  });

  test("can navigate to content list via cguid", async ({ page }) => {
    test.skip(!contentGuid, "TEST_CONTENT_GUID not set");
    await adminNav.gotoContentList(contentGuid);
    const listView = new ListViewPage(page);
    await listView.expectListVisible();
  });
});
