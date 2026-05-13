import { defineConfig, devices } from "@playwright/test";
import dotenv from "dotenv";
import path from "path";

dotenv.config({ path: path.resolve(__dirname, ".env") });

export default defineConfig({
  testDir: "./tests",
  fullyParallel: false,
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 2 : 0,
  workers: 1,
  reporter: [
    ["html", { open: "never" }],
    ["list"]
  ],
  use: {
    baseURL: process.env.STAGING_URL || "https://staging.example.com",
    trace: "on-first-retry",
    screenshot: "only-on-failure",
    video: "retain-on-failure",
  },
  projects: [
    {
      name: "setup",
      testDir: "./fixtures",
      testMatch: /auth\.setup\.ts/,
    },
    {
      name: "public-setup",
      testDir: "./fixtures",
      testMatch: /test-pages\.setup\.ts/,
      use: {
        storageState: ".auth/user.json",
      },
      dependencies: ["setup"],
    },
    {
      name: "chromium",
      use: {
        ...devices["Desktop Chrome"],
      },
      testMatch: /.*\.spec\.ts/,
      testIgnore: [/.*authenticated.*\.spec\.ts/, /.*\/public\/.*/],
    },
    {
      name: "chromium-authenticated",
      use: {
        ...devices["Desktop Chrome"],
        storageState: ".auth/user.json",
      },
      testMatch: /.*authenticated.*\.spec\.ts/,
      dependencies: ["setup"],
    },
    {
      name: "chromium-public",
      use: {
        ...devices["Desktop Chrome"],
      },
      testMatch: /.*\/public\/.*\.spec\.ts/,
      dependencies: ["public-setup"],
    },
  ],
});
