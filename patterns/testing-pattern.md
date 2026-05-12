# Contensive Testing Pattern

> All patterns and API reference: [Patterns Index](https://raw.githubusercontent.com/contensive/Contensive5/refs/heads/master/patterns/index.md)

## Overview

This document defines the testing strategy for the Contensive ecosystem — the core framework and all addon collections. It covers three testing layers:

1. **Unit/Integration Tests** (xUnit, C#) — test business logic, controllers, and models against a running Contensive application instance
2. **End-to-End Tests** (Playwright, TypeScript) — test rendered UI and user workflows through a real browser against a staging server
3. **How the layers relate** — what each layer covers, where they overlap, and when to use which

The goal is a testing framework that AI agents (Claude Code) can generate, run, and maintain as code evolves — while remaining useful for human developers.

---

## Testing Layers

### Layer 1: xUnit Integration Tests (existing)

These tests run business logic through the Contensive Processor without a browser. They exercise controllers, models, database operations, and external service integrations.

**What they cover:**
- Controller logic (order processing, payment, tax calculation, shipping)
- Database model CRUD operations
- API method request/response contracts
- External service integrations (Stripe, Avalara, ShipStation) with mocks
- Business rule validation

**What they do not cover:**
- HTML rendering and template correctness
- JavaScript behavior and client-side interactions
- Visual layout and CSS
- Multi-page user workflows (browse → cart → checkout → confirmation)
- Authentication flows through the browser

**Location convention:**
```
{repo}/
  source/c#-build/
    {project}Test/
      Tests/
        {Category}/
          {Feature}Tests.cs
```

**Example:**
```csharp
[Fact]
public void TestOrderCreate() {
    var app = new ApplicationModel(cp);
    var order = OrderController.create(app, accountId);
    Assert.True(order.id > 0);
    Assert.Equal(accountId, order.accountId);
}
```

**Running tests:**
```bash
cd source/c#-build
dotnet test {project}Test/{project}Tests.csproj

# Single test
dotnet test {project}Test/{project}Tests.csproj --filter "FullyQualifiedName=Tests.OrderTests.TestOrderCreate"
```

### Layer 2: End-to-End Tests (Playwright)

These tests automate a real browser against a deployed staging server. They verify that the full stack — server rendering, templates, JavaScript, CSS, and user interactions — works as a user would experience it.

**What they cover:**
- Page rendering (templates produce correct HTML)
- User workflows (multi-step processes like checkout)
- Form validation and submission
- JavaScript-driven interactions (cart updates, AJAX calls)
- Authentication and session management
- Admin portal navigation and operations
- Visual regressions (optional, via screenshot comparison)

**What they do not cover:**
- Internal controller logic (tested by xUnit)
- Database state beyond what's visible in the UI
- External service API contract details (tested by xUnit with mocks)

### How the Layers Relate

| Concern | xUnit | Playwright |
|---|---|---|
| Controller creates order correctly | Yes | No |
| Checkout page renders all fields | No | Yes |
| Tax calculation returns correct amount | Yes | No |
| User can complete a purchase end-to-end | No | Yes |
| API returns correct JSON structure | Yes | No |
| Admin can view and edit an account | No | Yes |
| Payment failure shows error message | Partial (logic) | Yes (UI) |
| Template variables are populated | No | Yes |

**Rule of thumb:** If the concern is about *business logic*, test it in xUnit. If the concern is about *what the user sees or does*, test it in Playwright. Some behaviors (like error handling) benefit from both — xUnit verifies the logic produces the right error, Playwright verifies the user sees it.

---

## End-to-End Testing with Playwright

### Technology Choice

**Playwright with TypeScript** is the standard for Contensive E2E testing.

Rationale:
- Free and open source (MIT license) — no additional charges
- First-class headless execution for CI and AI agent use
- Cross-browser support (Chromium, Firefox, WebKit) from one test suite
- Built-in `codegen` tool for generating tests from manual interaction
- Strong AI agent compatibility — Claude Code can generate, run, and debug tests
- TypeScript provides type safety while matching the JavaScript used in Contensive UI assets

### Project Structure

Each addon repo that includes E2E tests follows this structure:

```
{repo}/
  tests/e2e/
    playwright.config.ts          # Configuration (baseURL, timeouts, browsers)
    package.json                  # Dependencies (playwright, typescript)
    tsconfig.json                 # TypeScript configuration
    fixtures/
      auth.setup.ts               # Login once, reuse session state
    pages/                        # Page Object Models
      login.page.ts
      {feature}.page.ts
    helpers/
      test-data.ts                # Test data factories and cleanup
      api-helpers.ts              # API calls for test setup/teardown
    tests/
      smoke/                      # Fast sanity checks (run first)
        homepage.spec.ts
        login.spec.ts
      {feature}/                  # Feature-specific tests
        {feature}.spec.ts
      regression/                 # Comprehensive regression suite
        ...
```

### Configuration

**`playwright.config.ts`:**

```typescript
import { defineConfig, devices } from "@playwright/test";

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
      testMatch: /.*\.setup\.ts/,
    },
    {
      name: "chromium",
      use: {
        ...devices["Desktop Chrome"],
        storageState: ".auth/user.json",
      },
      dependencies: ["setup"],
    },
  ],
});
```

Key configuration decisions:
- **`fullyParallel: false`** and **`workers: 1`** — tests run sequentially to avoid conflicts on shared staging data. Increase workers only when tests use isolated data.
- **`baseURL` from environment variable** — each addon points to its own staging server
- **`storageState`** — login runs once in the setup project, all tests reuse the authenticated session
- **`trace: "on-first-retry"`** — captures detailed trace files only when a test fails and retries, keeping normal runs fast

**`package.json`:**

```json
{
  "name": "{addon-name}-e2e-tests",
  "private": true,
  "scripts": {
    "test": "npx playwright test",
    "test:smoke": "npx playwright test tests/smoke/",
    "test:headed": "npx playwright test --headed",
    "test:debug": "npx playwright test --debug",
    "codegen": "npx playwright codegen",
    "report": "npx playwright show-report"
  },
  "devDependencies": {
    "@playwright/test": "^1.50.0",
    "typescript": "^5.7.0"
  }
}
```

### Authentication Setup

Contensive uses session-based authentication. The setup fixture logs in once and saves the session state for all tests to reuse.

**`fixtures/auth.setup.ts`:**

```typescript
import { test as setup, expect } from "@playwright/test";

const authFile = ".auth/user.json";

setup("authenticate", async ({ page }) => {
  await page.goto("/");
  // -- click the login button/link (adjust selector to match site template)
  await page.click(".js-login-button");
  // -- fill credentials from environment variables
  await page.fill("#inputUsername", process.env.TEST_USERNAME || "");
  await page.fill("#inputPassword", process.env.TEST_PASSWORD || "");
  await page.click("button[type='submit']");
  // -- wait for successful login indicator
  await expect(page.locator(".js-user-greeting")).toBeVisible();
  // -- save session state
  await page.context().storageState({ path: authFile });
});
```

**Credentials are never committed to the repo.** Use environment variables or a `.env` file (added to `.gitignore`).

### Page Object Model (POM) Pattern

Each page or major component gets a POM class that encapsulates selectors and interactions. Tests use POMs instead of raw selectors.

**Why:**
- When a template changes, update one POM file instead of every test
- Tests read like user actions, not DOM queries
- AI agents can generate tests from POMs without understanding template internals

**`pages/login.page.ts`:**

```typescript
import { Page, expect } from "@playwright/test";

export class LoginPage {
  constructor(private page: Page) {}

  async goto() {
    await this.page.goto("/login");
  }

  async login(username: string, password: string) {
    await this.page.fill("#inputUsername", username);
    await this.page.fill("#inputPassword", password);
    await this.page.click("button[type='submit']");
  }

  async expectLoginSuccess() {
    await expect(this.page.locator(".js-user-greeting")).toBeVisible();
  }

  async expectLoginError(message: string) {
    await expect(this.page.locator(".js-login-error")).toContainText(message);
  }
}
```

**POM naming convention:** `{page-name}.page.ts` — matches the feature or addon it represents.

**Selector strategy (in priority order):**
1. `data-testid` attributes — most stable, add to templates when possible
2. `js-` prefixed classes — already used for JavaScript binding (see [Best Practices](best-practices-pattern.md))
3. Semantic selectors — `role`, `label`, `placeholder`, `text`
4. CSS selectors — use only when the above are unavailable

When adding `data-testid` attributes to Mustache templates:

```html
<!-- preferred: explicit test identifier -->
<button data-testid="add-to-cart" class="btn btn-primary js-add-to-cart">
  Add to Cart
</button>
```

### Writing Tests

**Test file naming:** `{feature}.spec.ts`

**Test structure:**

```typescript
import { test, expect } from "@playwright/test";
import { CatalogPage } from "../pages/catalog.page";

test.describe("Product Catalog", () => {
  let catalog: CatalogPage;

  test.beforeEach(async ({ page }) => {
    catalog = new CatalogPage(page);
    await catalog.goto();
  });

  test("displays product list", async () => {
    await catalog.expectProductsVisible();
  });

  test("can add item to cart", async ({ page }) => {
    await catalog.addFirstProductToCart();
    await expect(page.locator(".js-cart-count")).toHaveText("1");
  });
});
```

**Test categories:**

| Category | Directory | Purpose | Run Frequency |
|---|---|---|---|
| Smoke | `tests/smoke/` | Site is up, login works, key pages load | Every deploy |
| Feature | `tests/{feature}/` | Specific feature workflows | Every deploy |
| Regression | `tests/regression/` | Comprehensive coverage, edge cases | Nightly or on-demand |

### Test Data Management

Tests run against a shared staging server, so test data management is critical.

**Principles:**
- Tests create their own data when possible (via API helpers or UI)
- Test data uses identifiable prefixes (e.g., `TEST-` prefix on account names)
- Tests clean up after themselves in `afterAll` hooks
- Never depend on specific pre-existing data that another test might modify

**`helpers/test-data.ts`:**

```typescript
export const TEST_PREFIX = "TEST-";

export function testAccountName(): string {
  return `${TEST_PREFIX}Account-${Date.now()}`;
}

export function testEmail(): string {
  return `${TEST_PREFIX}${Date.now()}@test.example.com`;
}
```

**`helpers/api-helpers.ts`:**

```typescript
import { APIRequestContext } from "@playwright/test";

export class ApiHelper {
  constructor(private request: APIRequestContext, private baseURL: string) {}

  async createTestAccount(name: string): Promise<number> {
    const response = await this.request.post(`${this.baseURL}/api/createAccount`, {
      data: { accountName: name }
    });
    const result = await response.json();
    return result.accountId;
  }

  async deleteTestAccount(accountId: number): Promise<void> {
    await this.request.post(`${this.baseURL}/api/deleteAccount`, {
      data: { accountId }
    });
  }
}
```

### Running Tests

```bash
# Navigate to the e2e test directory
cd tests/e2e

# Install dependencies (first time only)
npm install
npx playwright install chromium

# Run all tests
npx playwright test

# Run smoke tests only
npx playwright test tests/smoke/

# Run a specific test file
npx playwright test tests/catalog/browse-products.spec.ts

# Run in headed mode (see the browser)
npx playwright test --headed

# Run with Playwright UI (interactive)
npx playwright test --ui

# Generate a test from manual interaction
npx playwright codegen https://staging.example.com

# View the HTML report after a test run
npx playwright show-report
```

**Environment variables:**

```bash
# Required
export STAGING_URL="https://staging.example.com"
export TEST_USERNAME="testuser"
export TEST_PASSWORD="testpassword"

# Optional
export CI=true  # Enables retries, disables .only()
```

On Windows, set these in a `.env` file in `tests/e2e/` (add `.env` to `.gitignore`) or use `set` commands before running.

### Debugging Failures

Playwright provides several tools for diagnosing test failures:

- **HTML report** — `npx playwright show-report` opens an interactive report with screenshots, traces, and error details
- **Trace viewer** — when a test fails with `trace: "on-first-retry"`, open the trace file with `npx playwright show-trace trace.zip` to step through every action, network request, and DOM snapshot
- **Screenshots** — captured automatically on failure (configured in `playwright.config.ts`)
- **Video** — retained on failure for visual review of what happened
- **Debug mode** — `npx playwright test --debug` opens the Playwright Inspector for step-by-step execution

---

## Contensive Platform-Specific Knowledge

These are practical details learned from building real tests against the Contensive platform. AI agents must understand these before writing tests.

### Admin Site Selectors

The admin site HTML is rendered server-side by C# view classes in `source/Processor/Addons/AdminSite/Views/`. Do not guess selectors -- read the C# source to find the actual element structure.

**Key facts:**
- **Buttons are `<input type="submit">`, not `<button>`.** Save, OK, Cancel, Delete, Refresh, and Add are all rendered by `HtmlController.inputSubmit()`. Use `page.locator("input[value='Save']")`, not `page.getByRole("button")`.
- **Forms use `name` attributes, not `id`.** `HtmlController.form()` sets `name="adminForm"` but not `id="adminForm"`. Do not use `form#adminForm` as a selector.
- **The login form container is `.ccLoginFormCon`.** Login errors render inside `.ccLoginFormCon .alert-danger[role='alert']`.
- **Admin URLs use query parameters:** `af=1` (list view), `af=4` (edit view), `cguid` (content GUID), `cid` (content ID), `method=logout` (logout).
- **Grid data rows** can be found with `tr:has(a[href*='af=4'])` -- rows that contain an edit link.
- **The admin nav sidebar** uses standard `<a>` links. Use `page.getByRole("link", { name: "Dashboard" })` for nav elements.

### Content API Endpoints

The Contensive Content API provides JSON endpoints for managing pages, link aliases, and resources. These are useful for test data setup.

**URL format:** Endpoints are path segments, not query strings.
```
GET  {baseURL}/content-api-page-list
GET  {baseURL}/content-api-page-get?url=/about
POST {baseURL}/content-api-page-create      (JSON body)
POST {baseURL}/content-api-page-update      (JSON body)
POST {baseURL}/content-api-page-alias-add   (JSON body)
POST {baseURL}/content-api-resource-upload   (JSON body)
GET  {baseURL}/content-api-resource-list
```

**Authentication:** All Content API endpoints require admin session cookies (`cp.User.IsAdmin`). Use Playwright's `storageState` to pass the authenticated session to `APIRequestContext`.

**Response format:**
```json
{
  "success": true,
  "message": "OK",
  "data": { ... }
}
```

The Contensive5 repo contains a reference implementation of a Content API helper class at `tests/e2e/helpers/content-api.ts` that wraps these endpoints for Playwright.

### Session and Authentication Gotchas

- **Failed login triggers a 3-second server delay** (`Thread.Sleep`). Tests that submit bad credentials need `timeout: 10000` on subsequent assertions.
- **Logout invalidates the server-side session.** The `?method=logout` URL destroys the session on the server. If a test logs out using the shared `.auth/user.json` session, ALL subsequent authenticated tests fail. Logout tests must do their own login/logout cycle in an unauthenticated project (`.spec.ts`, not `.authenticated.spec.ts`).
- **The `.auth/user.json` file stores session cookies.** It's created by the `setup` project and reused by all authenticated projects. If the server restarts between test runs, the session may be invalid -- the setup fixture re-authenticates automatically.
- **Empty form fields:** HTML `required` attributes on login inputs prevent browser-level submission. Testing server-side empty-field validation would require `page.evaluate()` to remove `required`, which tests an artificial scenario -- skip these tests.

### Contensive URL Normalization

Link aliases follow these rules (see `UrlResolverHelper.cs`):
- Always start with a leading `/`
- No trailing `/` (except root `/`)
- Case-sensitive matching
- The most recently created alias record (highest ID) is canonical
- Root URL `/` resolves via the domain record's `rootPageId`

### Responsive Navbar Elements

The admin navbar uses Bootstrap's collapse pattern. Elements inside the collapsed navbar (like the Logout button) may not be visible in headless mode or accessible in the accessibility tree. Prefer URL navigation (`page.goto("/admin?method=logout")`) over clicking collapsed UI elements.

---

## AI Agent Workflow

A primary design goal of this testing pattern is AI agent compatibility. Claude Code (or similar tools) should be able to:

1. **Generate tests from code review** — read an addon's C# controller, view model, and Mustache template, then write the corresponding POM and test file
2. **Add tests alongside new features** — when building a feature, also write the E2E test
3. **Run tests and interpret results** — execute `npx playwright test` via shell, parse output, diagnose and fix failures
4. **Maintain the regression suite** — update POMs when templates change, add tests for new behavior

### AI Test Generation Workflow

When Claude Code is asked to generate E2E tests for an addon feature:

1. **Read this pattern first** — understand the project structure, authentication, POM conventions, and Contensive-specific selector rules
2. **Read the addon's C# source** — the `Execute()` method, the view model, and the Mustache layout template. Selectors come from here, not from guessing
3. **Read existing tests and POMs** in the addon's `tests/e2e/` folder — follow the established patterns
4. **Identify testable user actions** — form submissions, navigation, data display, error states
5. **Check for an existing POM** — update it if the page already has one, create a new one if not
6. **Write the test** — use the POM, follow the test structure conventions above
7. **Run the test** — execute `npx playwright test` via shell, verify it passes
8. **Diagnose failures from page snapshots** — when a test fails, Playwright captures the page state. Compare the actual HTML against the C# source to fix selectors
9. **Report results** — summarize what was tested and any issues found

### Reference Implementation

The Contensive5 core repo contains a fully working E2E test suite that serves as the reference implementation for all addon projects:

```
Contensive5/tests/e2e/
  fixtures/auth.setup.ts              # Authentication setup
  fixtures/test-pages.setup.ts        # Content API test data creation
  helpers/content-api.ts              # Content API client
  helpers/load-test-pages.ts          # Test data loader
  helpers/test-data.ts                # Test data factories
  pages/login.page.ts                 # Login POM
  pages/admin-nav.page.ts             # Admin navigation POM
  pages/list-view.page.ts             # Admin list view POM
  pages/edit-view.page.ts             # Admin edit view POM
  pages/public-page.page.ts           # Public page POM
  tests/smoke/                        # Health checks
  tests/auth/                         # Authentication workflows
  tests/admin/                        # Admin site features
  tests/public/                       # Public site page rendering
  README.md                           # Full documentation
```

When starting E2E tests for a new addon, read the reference implementation's README and source files to understand the patterns before writing new tests.

### CLAUDE.md Reference

Each addon repo's `CLAUDE.md` should include a testing section that points to this pattern and describes the addon-specific test setup:

```markdown
## Testing

Follow the [Contensive Testing Pattern](https://raw.githubusercontent.com/contensive/Contensive5/refs/heads/master/patterns/testing-pattern.md) for all testing conventions.

For the reference E2E implementation, see [Contensive5 E2E README](https://raw.githubusercontent.com/contensive/Contensive5/refs/heads/master/tests/e2e/README.md).

- E2E tests: `tests/e2e/` (Playwright, TypeScript)
- Integration tests: `source/c#-build/{project}Test/` (xUnit, C#)
- Staging URL: `https://{addon-staging-site}`
- Key pages to test: {list the addon's main UI pages/features}
```

---

## Implementation Phases

When adding E2E testing to an addon repo for the first time:

### Phase 1 — Foundation

- Create `tests/e2e/` directory with `playwright.config.ts`, `package.json`, `tsconfig.json`
- Implement authentication setup fixture
- Write 3-5 smoke tests (site loads, login works, key pages render)
- Add `.env.example` with required variable names
- Update `.gitignore` to exclude `.env`, `.auth/`, `node_modules/`, `test-results/`, `playwright-report/`

### Phase 2 — Page Objects and Core Flows

- Build POMs for the addon's major pages
- Write tests for the primary user workflows (the "happy path")
- Add `data-testid` attributes to Mustache templates where selectors are fragile

### Phase 3 — Regression Coverage

- AI agent reviews each addon/controller and generates corresponding E2E tests
- Cover error states, edge cases, and less-common workflows
- Add visual regression tests (screenshot comparison) for critical pages

### Phase 4 — CI Integration (optional)

- GitHub Actions workflow: build → deploy to staging → run Playwright → report
- Run smoke tests on every deploy, full regression suite nightly

---

## xUnit Test Conventions

This section documents the conventions for the existing xUnit integration test layer, for reference and consistency across addons.

### Project Setup

Test projects use xUnit with the following dependencies:

```xml
<PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.*" />
<PackageReference Include="xunit" Version="2.*" />
<PackageReference Include="xunit.runner.visualstudio" Version="3.*" />
```

### Test Organization

```
{project}Test/
  Tests/
    {Category}/
      {Feature}Tests.cs
  Constants.cs           # Test app name and shared constants
  TestConfig.cs          # Assembly-level configuration
```

Categories mirror the business domain: `Orders/`, `Accounts/`, `Catalog/`, `Payments/`, `Subscriptions/`, etc.

### Test Configuration

Disable parallel execution to avoid shared state conflicts:

```csharp
[assembly: CollectionBehavior(DisableTestParallelization = true)]
```

### Test Structure

```csharp
public class OrderController_Tests {
    [Fact]
    public void TestOrderCreate() {
        // -- arrange
        using var cp = new CPClass(Constants.testAppName);
        var app = new ApplicationModel(cp);

        // -- act
        var order = OrderController.create(app, accountId);

        // -- assert
        Assert.True(order.id > 0);
        Assert.Equal(accountId, order.accountId);
    }
}
```

### Mocking External Services

Use `ApplicationModel` flags to mock external dependencies:

```csharp
app.failNonTestPaymentProcess = true;   // Simulate payment failures
app.email.mock = true;                   // Prevent sending real emails
```

### Naming Conventions

- Test class: `{Feature}Tests` or `{Controller}_Tests`
- Test method: `Test{Action}{Condition}` (e.g., `TestOrderCreateWithDiscount`)
- Use `[Fact]` for tests without parameters, `[Theory]` with `[InlineData]` for parameterized tests

---

## Summary

| Layer | Technology | Runs Against | Covers | Managed By |
|---|---|---|---|---|
| Integration | xUnit (C#) | Contensive test app | Business logic, controllers, models, API contracts | Developers, AI agents |
| End-to-End | Playwright (TypeScript) | Staging server (browser) | UI rendering, user workflows, JavaScript, templates | Developers, AI agents |

Both layers are designed to be generated and maintained by AI agents while remaining readable and useful for human developers. The xUnit layer validates that the code does the right thing; the Playwright layer validates that the user sees and experiences the right thing.
