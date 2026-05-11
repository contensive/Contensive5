# Contensive E2E Tests

End-to-end tests for the Contensive platform using [Playwright](https://playwright.dev/) and TypeScript.

## Prerequisites

- Node.js 18+
- A running Contensive staging site
- An admin user account on that site

## Setup

```bash
cd tests/e2e
npm install
npx playwright install chromium
```

Create a `.env` file in `tests/e2e/`:

```
STAGING_URL=https://your-staging-site.com
TEST_USERNAME=your-admin-username
TEST_PASSWORD=your-admin-password
```

## Running Tests

```bash
# Run all tests
npx playwright test

# Run a specific test suite
npx playwright test tests/smoke/
npx playwright test tests/auth/
npx playwright test tests/admin/
npx playwright test tests/public/

# Run a single test file
npx playwright test tests/public/link-alias.spec.ts

# Run in headed mode (see the browser)
npx playwright test --headed

# Run in debug mode (step through tests)
npx playwright test --debug

# View the HTML report after a run
npx playwright show-report
```

## Project Structure

```
tests/e2e/
  fixtures/           # Setup scripts that run before test suites
    auth.setup.ts         # Logs in and saves session to .auth/user.json
    test-pages.setup.ts   # Creates test pages via Content API
  helpers/            # Shared utilities
    content-api.ts        # Content API client (page CRUD, link aliases)
    load-test-pages.ts    # Loads test page data created by setup
    test-data.ts          # Test data factories (names, emails)
  pages/              # Page Object Models (POMs)
    login.page.ts         # Login form
    admin-nav.page.ts     # Admin navigation sidebar
    list-view.page.ts     # Admin list view (content tables)
    edit-view.page.ts     # Admin edit view (record forms)
    public-page.page.ts   # Public-facing page (title, meta, headline)
  tests/              # Test files
    smoke/                # Basic health checks
    auth/                 # Authentication workflows
    admin/                # Admin site features
    public/               # Public site page rendering
```

## Playwright Projects

Tests are organized into Playwright projects that control authentication state and execution order:

| Project | Auth State | Runs | Description |
|---------|-----------|------|-------------|
| `setup` | None | First | Logs in, saves session cookies |
| `public-setup` | Authenticated | After setup | Creates test pages via Content API |
| `chromium` | Unauthenticated | After setup | Login, logout, protected access tests |
| `chromium-authenticated` | Authenticated | After setup | Admin UI tests (nav, list, edit) |
| `chromium-public` | Unauthenticated | After public-setup | Public page rendering tests |

## File Naming Conventions

- `*.spec.ts` -- Unauthenticated tests (run in `chromium` project)
- `*.authenticated.spec.ts` -- Authenticated tests (run in `chromium-authenticated` project)
- Tests in `tests/public/` -- Public site tests (run in `chromium-public` project)
- `*.setup.ts` -- Setup fixtures (run before test suites)
- `*.page.ts` -- Page Object Models

## Writing New Tests

### 1. Choose the right project

- **Unauthenticated test** (visitor perspective): Name the file `*.spec.ts` and place it outside `tests/public/`.
- **Authenticated test** (admin perspective): Name the file `*.authenticated.spec.ts`.
- **Public site test** (requires test data from Content API): Place the file in `tests/public/`.

### 2. Use Page Object Models

Always use POMs instead of writing selectors directly in tests:

```typescript
import { test, expect } from "@playwright/test";
import { PublicPage } from "../../pages/public-page.page";

test("page renders correctly", async ({ page }) => {
  const publicPage = new PublicPage(page);
  await publicPage.goto("/about");
  await publicPage.expectStatus(200);
  await publicPage.expectHeadline("About Us");
});
```

### 3. Use the Content API helper for test data

When tests need specific content to exist, use the Content API helper rather than creating content through the admin UI:

```typescript
import { ContentApi } from "../../helpers/content-api";

test("example using content api", async ({ request }) => {
  const baseURL = process.env.STAGING_URL || "https://staging.example.com";
  const api = new ContentApi(request, baseURL);

  const page = await api.pageCreate("/parent", "My Test Page");
  await api.pageUpdate(page.url, { metaDescription: "test" });
  const data = await api.pageGet(page.url);
});
```

### 4. Add a new POM when testing a new area

If you're testing a new part of the UI, create a new POM file in `pages/`. Follow the existing pattern:

```typescript
import { Page, Locator, expect } from "@playwright/test";

export class MyNewPage {
  readonly someElement: Locator;

  constructor(private page: Page) {
    this.someElement = page.locator(".my-selector");
  }

  async goto() {
    await this.page.goto("/my-page");
  }

  async expectVisible() {
    await expect(this.someElement).toBeVisible();
  }
}
```

## Prompting AI to Create Tests

When using Claude Code or another AI assistant to write E2E tests for this project, use the following prompt structure. Copy and adapt the examples below.

### Prompt template for a new test area

```
Write Playwright E2E tests for [AREA] in the Contensive platform.

Read the existing test infrastructure first:
- tests/e2e/playwright.config.ts (project setup)
- tests/e2e/pages/*.page.ts (existing POMs)
- tests/e2e/helpers/ (shared utilities)
- tests/e2e/fixtures/ (setup scripts)

Then read the relevant C# source code to understand the HTML output:
- source/Processor/Addons/AdminSite/Views/ (admin views)
- source/Processor/Controllers/ (rendering controllers)

Follow these conventions:
- Use the Page Object Model pattern (one POM per page/component)
- Put POMs in tests/e2e/pages/
- Use existing helpers (ContentApi, test-data factories)
- Name files *.spec.ts for unauthenticated, *.authenticated.spec.ts for admin
- Put public site tests in tests/public/
- Check actual HTML selectors in the C# source -- don't guess class names
- Account for server-side delays (e.g. 3s Thread.Sleep on failed login)
- Run the tests after writing them to verify they pass
```

### Example prompts

**Adding admin feature tests:**
```
Write E2E tests for the admin search functionality. Read the admin site
source code to understand how search works, then create a POM and test file.
Tests should run as authenticated (*.authenticated.spec.ts).
```

**Adding public site tests:**
```
Write E2E tests for page template rendering. Use the Content API helper
to create test pages with specific templates, then verify the public page
renders with the correct template structure. Place tests in tests/public/.
```

**Adding tests for a specific content type:**
```
Write E2E tests for the admin list view and edit view of the People
content type. Use the existing list-view.page.ts and edit-view.page.ts
POMs. Tests should verify field rendering, save, and delete operations.
```

### Key things the AI needs to know

1. **Read C# source before writing selectors.** Contensive renders HTML server-side. The actual CSS classes and element structure are defined in C# view code (`source/Processor/Addons/AdminSite/Views/`), Mustache templates (`source/Processor/Resources/`), and the base assets ZIP. Never guess selectors.

2. **Content API endpoints use path segments, not query strings.** The URL pattern is `{baseURL}/content-api-page-list`, not `?method=content-api-page-list`.

3. **The admin site uses form inputs for buttons.** Buttons like Save, OK, Cancel, Delete, Refresh, and Add are `<input type="submit">` elements, not `<button>`. Use `page.locator("input[value='Save']")` not `page.getByRole("button")`.

4. **Forms don't have reliable id attributes.** The admin forms use `name` attributes (e.g., `name="adminForm"`) but not `id`. Use content-based selectors (button values, field labels) instead of form IDs.

5. **Logout invalidates the server-side session.** Tests that call logout must NOT use the shared authenticated session (`*.authenticated.spec.ts`). They should do their own login/logout cycle in a `*.spec.ts` file.

6. **The `.auth/user.json` file stores session cookies.** Authenticated projects reuse this file. The `setup` project creates it, and `public-setup` uses it for Content API calls.

7. **Public site tests depend on test data.** Tests in `tests/public/` load page URLs from `.test-data/pages.json`, which is created by `test-pages.setup.ts`. Use `requireTestPages()` inside `test.beforeAll()`, not at module level.
