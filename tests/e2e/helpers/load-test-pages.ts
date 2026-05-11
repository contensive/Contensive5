import fs from "fs";
import path from "path";

export interface TestPageData {
  timestamp: number;
  parentUrl: string;
  basicPage: {
    url: string;
    pageId: number;
    headline: string;
    pageTitle: string;
    metaDescription: string;
    navHeadline: string;
  };
  childPage: {
    url: string;
    pageId: number;
    headline: string;
    navHeadline: string;
  };
  aliasPage: {
    url: string;
    pageId: number;
    headline: string;
    aliasUrl: string;
  };
  noTitlePage: {
    url: string;
    pageId: number;
    headline: string;
    navHeadline: string;
  };
}

/**
 * Load test page data created by the test-pages.setup.ts fixture.
 * Returns null if the data file doesn't exist (setup hasn't run yet).
 * This allows module-level calls during test discovery without throwing.
 */
export function loadTestPages(): TestPageData | null {
  const filePath = path.resolve(__dirname, "../.test-data/pages.json");
  if (!fs.existsSync(filePath)) {
    return null;
  }
  return JSON.parse(fs.readFileSync(filePath, "utf-8"));
}

/**
 * Load test page data, throwing if not available.
 * Use inside test.beforeAll() or test() blocks, not at module level.
 */
export function requireTestPages(): TestPageData {
  const data = loadTestPages();
  if (!data) {
    throw new Error(
      "Test page data not found at .test-data/pages.json. " +
      "Ensure the public-setup project ran first."
    );
  }
  return data;
}
