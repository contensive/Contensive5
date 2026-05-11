/**
 * Test data factories and constants.
 *
 * All test-created data uses the TEST_PREFIX so it can be identified and cleaned up.
 */

export const TEST_PREFIX = "TEST-";

/** Generate a unique test account name. */
export function testAccountName(): string {
  return `${TEST_PREFIX}Account-${Date.now()}`;
}

/** Generate a unique test email address. */
export function testEmail(): string {
  return `${TEST_PREFIX}${Date.now()}@test.example.com`;
}

/** Generate a unique test page name. */
export function testPageName(): string {
  return `${TEST_PREFIX}Page-${Date.now()}`;
}
