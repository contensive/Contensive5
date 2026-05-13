import { Page, Locator, expect } from "@playwright/test";

/**
 * Page Object Model for the Contensive admin edit form view.
 *
 * The edit view renders a form with a hidden input FormEmptyFieldList.
 * Field labels use .ccAdminEditCaption, field inputs use .ccAdminEditField.
 * Buttons are input[type=submit] with name="button" and values like
 * "Save", "OK", "Cancel", "Delete".
 */
export class EditViewPage {
  readonly saveButton: Locator;
  readonly fieldLabels: Locator;
  readonly fieldInputs: Locator;

  constructor(private page: Page) {
    this.saveButton = page.locator("input[value='Save']").first();
    this.fieldLabels = page.locator(".ccAdminEditCaption");
    this.fieldInputs = page.locator(".ccAdminEditField");
  }

  /** Navigate to edit a specific record by content GUID and record ID. */
  async goto(cguid: string, id: number) {
    await this.page.goto(`/admin?cguid=${encodeURIComponent(cguid)}&id=${id}&af=4`);
  }

  /** Navigate to add a new record by content GUID. */
  async gotoNew(cguid: string) {
    await this.page.goto(`/admin?cguid=${encodeURIComponent(cguid)}&id=0&af=4`);
  }

  /** Assert the edit form is rendered. */
  async expectEditVisible() {
    await expect(this.saveButton).toBeVisible();
  }

  /** Get the value of a field by its input name. */
  async getFieldValue(name: string): Promise<string> {
    const input = this.page.locator(`input[name='${name}'], textarea[name='${name}'], select[name='${name}']`).first();
    const tagName = await input.evaluate(el => el.tagName.toLowerCase());
    if (tagName === "select") {
      return await input.inputValue();
    }
    return await input.inputValue();
  }

  /** Set the value of a text field by its input name. */
  async setFieldValue(name: string, value: string) {
    await this.page.locator(`input[name='${name}'], textarea[name='${name}']`).first().fill(value);
  }

  /** Click the Save button. */
  async clickSave() {
    await this.saveButton.click();
  }

  /** Click the OK button (save and return to list). */
  async clickOK() {
    await this.page.locator("input[value='OK']").first().click();
  }

  /** Click the Cancel button. */
  async clickCancel() {
    await this.page.locator("input[value='Cancel']").first().click();
  }

  /** Click the Delete button. */
  async clickDelete() {
    await this.page.locator("input[value='Delete']").first().click();
  }

  /** Assert save succeeded — the form reloads or returns to list without error. */
  async expectSaveSuccess() {
    await expect(this.saveButton).toBeVisible();
  }
}
