# Sprint9 Login Tests

This directory contains Playwright scripts and tests for logging into the Sprint9 admin control panel.

## Files

### `sprint9-login.setup.js`
A standalone script that logs into the Sprint9 admin panel and saves the authentication state for reuse in other tests.

**Usage:**
```bash
node tests/sprint9-login.setup.js
```

**Features:**
- Navigates to https://sprint9.sitefpo.com/admin
- Logs in with test credentials (testuser / testPassword1234!)
- Saves authentication state to `sprint9-admin-auth.json`
- Takes screenshots on success/failure for debugging
- Comprehensive error handling and logging

### `sprint9-login.spec.ts`
Playwright test file with multiple test scenarios for the login functionality.

**Usage:**
```bash
npx playwright test sprint9-login.spec.ts
```

**Test Cases:**
1. **Successful Login Test** - Verifies login with valid credentials
2. **Form Elements Test** - Validates presence and attributes of login form elements
3. **Invalid Credentials Test** - Tests error handling with invalid credentials

## Prerequisites

1. Install dependencies:
```bash
npm install
```

2. Install Playwright browsers:
```bash
npx playwright install
```

## Test Credentials

- **URL:** https://sprint9.sitefpo.com/admin
- **Username:** testuser
- **Password:** testPassword1234!

## Configuration

The Playwright configuration (`playwright.config.ts`) has been updated to look for tests in the `./tests` directory instead of `./e2e`.

## Expected Form Elements

The scripts expect the following form elements on the login page:
- Username field: `#inputUsername`
- Password field: `#inputPassword`
- Submit button: `button[type="submit"]`

These selectors are based on the existing login form structure found in the repository templates.