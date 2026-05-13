import { Page, Locator, expect } from "@playwright/test";

/**
 * Page Object Model for the Contensive login form.
 *
 * Contensive renders login forms with input fields named "username" and "password"
 * inside a container with class "ccLoginFormCon". The exact form layout varies by
 * site settings (email vs username, password required vs optional, auto-login).
 *
 * Login errors render as:
 *   <div class="alert alert-danger" role="alert">Error message here.</div>
 *
 * If your staging site uses a custom login addon, update these selectors to match.
 */
export class LoginPage {
  readonly loginForm: Locator;
  readonly errorAlert: Locator;

  constructor(private page: Page) {
    this.loginForm = page.locator(".ccLoginFormCon");
    this.errorAlert = page.locator(".ccLoginFormCon .alert-danger[role='alert']");
  }

  /**
   * Navigate to a page that triggers the login form.
   * Contensive doesn't have a dedicated /login route — the login form is rendered
   * on any page that requires authentication. The /admin route always requires auth.
   */
  async goto() {
    await this.page.goto("/admin");
  }

  /** Fill in credentials and submit the login form. */
  async login(username: string, password: string) {
    await this.page.fill("input[name='username']", username);
    await this.page.fill("input[name='password']", password);
    await this.page.click("button[type='submit'], input[type='submit']");
  }

  /** Assert that login succeeded by checking for an authenticated indicator. */
  async expectLoginSuccess() {
    await expect(this.loginForm).not.toBeVisible({ timeout: 10000 });
  }

  /** Assert that login failed and the form is still displayed. */
  async expectLoginFormVisible() {
    await expect(this.loginForm).toBeVisible();
  }

  /** Assert that a login error message is visible. */
  async expectErrorVisible() {
    await expect(this.errorAlert).toBeVisible({ timeout: 10000 });
  }

  /** Assert that an error message contains the expected text. */
  async expectErrorContains(text: string | RegExp) {
    await expect(this.errorAlert).toBeVisible({ timeout: 10000 });
    await expect(this.errorAlert).toContainText(text);
  }

  /** Assert that no login error message is visible. */
  async expectNoError() {
    await expect(this.errorAlert).not.toBeVisible();
  }
}
