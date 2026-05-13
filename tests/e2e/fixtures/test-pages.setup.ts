import { test as setup } from "@playwright/test";
import { ContentApi } from "../helpers/content-api";
import fs from "fs";
import path from "path";

const testDataDir = path.resolve(__dirname, "../.test-data");
const pagesFile = path.join(testDataDir, "pages.json");

/**
 * Public site test data setup — runs once before public site tests.
 *
 * Creates test pages via the Content API and saves their URLs
 * to .test-data/pages.json for test consumption.
 */
setup("create test pages", async ({ request }) => {
  const baseURL = process.env.STAGING_URL || "https://staging.example.com";
  const api = new ContentApi(request, baseURL);

  // Ensure .test-data directory exists
  if (!fs.existsSync(testDataDir)) {
    fs.mkdirSync(testDataDir, { recursive: true });
  }

  const timestamp = Date.now();

  // -- Check if parent page exists by listing pages
  const pageTree = await api.pageList();
  const parentExists = findPageByUrl(pageTree, "/e2e-test-pages");

  // -- Create parent page if it doesn't exist
  let parentUrl = "/e2e-test-pages";
  if (!parentExists) {
    const parent = await api.pageCreate("/", `E2E Test Pages`);
    parentUrl = parent.url;
  }

  // -- Create a basic test page with metadata
  const basicPage = await api.pageCreate(parentUrl, `Basic Page ${timestamp}`, {
    headline: `Basic Page Headline ${timestamp}`
  });

  // -- Update the page with metadata
  await api.pageUpdate(basicPage.url, {
    pageTitle: `Basic Page Title ${timestamp}`,
    metaDescription: `E2E test page description ${timestamp}`
  });

  // -- Create a child page under the basic page (for hierarchy tests)
  const childPage = await api.pageCreate(basicPage.url, `Child Page ${timestamp}`, {
    headline: `Child Page Headline ${timestamp}`
  });

  // -- Create a page for link alias tests
  const aliasPage = await api.pageCreate(parentUrl, `Alias Page ${timestamp}`, {
    headline: `Alias Page Headline ${timestamp}`
  });

  // -- Add a non-canonical alias to the alias page
  const aliasResult = await api.pageAliasAdd(
    aliasPage.url,
    `/e2e-test-pages/alias-redirect-${timestamp}`,
    false
  );

  // -- Create a page with no explicit pageTitle (for fallback tests)
  const noTitlePage = await api.pageCreate(parentUrl, `No Title Page ${timestamp}`, {
    headline: `No Title Headline ${timestamp}`
  });

  // -- Save all page data for test consumption
  const testData = {
    timestamp,
    parentUrl,
    basicPage: {
      url: basicPage.url,
      pageId: basicPage.pageId,
      headline: `Basic Page Headline ${timestamp}`,
      pageTitle: `Basic Page Title ${timestamp}`,
      metaDescription: `E2E test page description ${timestamp}`,
      navHeadline: `Basic Page ${timestamp}`
    },
    childPage: {
      url: childPage.url,
      pageId: childPage.pageId,
      headline: `Child Page Headline ${timestamp}`,
      navHeadline: `Child Page ${timestamp}`
    },
    aliasPage: {
      url: aliasPage.url,
      pageId: aliasPage.pageId,
      headline: `Alias Page Headline ${timestamp}`,
      aliasUrl: `/e2e-test-pages/alias-redirect-${timestamp}`
    },
    noTitlePage: {
      url: noTitlePage.url,
      pageId: noTitlePage.pageId,
      headline: `No Title Headline ${timestamp}`,
      navHeadline: `No Title Page ${timestamp}`
    }
  };

  fs.writeFileSync(pagesFile, JSON.stringify(testData, null, 2));
});

/** Recursively search a page tree for a URL. */
function findPageByUrl(
  nodes: Array<{ url: string; children: Array<{ url: string; children: unknown[] }> }>,
  url: string
): boolean {
  for (const node of nodes) {
    if (node.url === url) { return true; }
    if (node.children && findPageByUrl(node.children as typeof nodes, url)) {
      return true;
    }
  }
  return false;
}
