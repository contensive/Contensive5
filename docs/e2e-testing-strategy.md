# End-to-End (E2E) Testing Strategy

How Contensive and its addons use browser tests (Playwright) as a release gate.
aoEcommerce is the pilot. Other addons follow the same pattern later.

Audience: a developer who has not written e2e tests before. Read sections 1-4 first.
Related: [release-workflow.md](release-workflow.md), [../patterns/testing-pattern.md](../patterns/testing-pattern.md),
[../tests/e2e/README.md](../tests/e2e/README.md), [../plans/cicd-pipeline.md](../plans/cicd-pipeline.md).

---

## 1. Why e2e, and where it sits

### The test pyramid in plain words

| Layer | What it checks | Speed | Where it lives | Example |
|---|---|---|---|---|
| Unit | One method, no database, no web server | milliseconds | `*Tests.csproj` (xUnit/MSTest) | `getRightNow()` returns the override date |
| Integration | Several classes together, with a real database | seconds | `accountBillingTest` (~328 tests, needs app "test" + SQL) | `OrderCalculateController` totals an order with tax |
| E2E | The whole product, as a customer or admin sees it, in a real browser against a real site | minutes | `tests/e2e/` (Playwright + TypeScript) | Guest adds a widget to the cart, enters a promo code, pays, sees the receipt |

Most checks belong at the bottom of the pyramid: many fast unit tests, fewer integration tests, and a
small number of e2e tests. E2E tests are slow and break more easily, but they are the only layer
that proves the parts work *together*. That includes the collection XML, layouts, JavaScript, site
properties, the installed DLLs and the payment gateway redirect.

### Why aoEcommerce needs it

aoEcommerce runs live billing on several production sites. A broken checkout or a broken renewal
costs real money and trust. The unit and integration tests cannot see a layout id renamed in
`CatalogCartBs5Layout.html`, a missing field in `Ecommerce.xml`, or a redirect to the Stripe hosted
page that no longer works. A short browser suite run on every release catches these before
customers do.

### What e2e is not

- Not a replacement for unit/integration tests. Business rules (tax math, proration, GL entries)
  are tested lower down, where failures are cheap to diagnose.
- Not a load test or a security scan.
- Not run against production, ever (section 10).

---

## 2. Suite layout per repo

Every repo that has e2e tests keeps them in `tests/e2e/`, a self-contained npm project:

```
tests/e2e/
  playwright.config.ts     projects, reporters, env loading, host allow-list
  package.json             @playwright/test, dotenv, typescript
  .env.example             names of every variable (no values) - .env itself is never committed
  fixtures/                setup and teardown "tests" (auth, seed, cleanup, site lock)
  helpers/                 env, test data, admin record helper, site settings, urls
  pages/                   Page Object Models (POMs), one class per page or component
    admin/                 POMs for admin screens (list view, edit view)
  tests/
    smoke/                 @smoke   - is the site up, can I log in
    catalog/ cart/ checkout/ myaccount/ promo/ ...  @regression - workflow packs
    gateway/               @gateway - real sandbox checkouts through hosted pages
  e2e-results/             results.json + run-info.json + site-lock.json (generated, git-ignored)
  playwright-report/       HTML report (generated, git-ignored)
```

### Projects (run order)

Playwright "projects" are named groups of tests with their own settings and dependencies.
The order is enforced with `dependencies`, and clean-up with `teardown`:

```
setup-admin ──► smoke ──► seed ──► setup-customers ──► regression ──► gateway
                           │
                           └── teardown: cleanup (always runs after its dependents finish)
```

| Project | Does | Changes the site? |
|---|---|---|
| `setup-admin` | Logs in as the admin, saves `.auth/user.json` | no |
| `smoke` | `@smoke` tests: home page, login form | no |
| `seed` | Takes the site lock, saves site properties, sets e2e values, creates/refreshes `TEST-E2E-` data | yes |
| `setup-customers` | Logs in each seeded customer, saves `.auth/customer-<key>.json` | no |
| `regression` | `@regression` workflow tests | yes (orders) |
| `gateway` | `@gateway` sandbox tests; skip themselves when credentials are missing | yes (orders, gateway properties) |
| `cleanup` | Restores site properties, deletes this run's open orders, releases the lock | restores |

Smoke runs **before** seed on purpose: if the site is down, the run stops before it changes anything.

### Tags

Tags go on the `describe` block, using Playwright's tag option:

```ts
test.describe("Cart operations", { tag: "@regression" }, () => {
  test("adding a product shows it in the cart", async ({ page }) => { /* ... */ });
});
```

| Tag | Meaning | Runs in release? |
|---|---|---|
| `@smoke` | Fast "is it alive" checks, no data changes | yes, first |
| `@regression` | Workflow tests with AcceptAll payments | yes |
| `@gateway` | Real Authorize.Net sandbox / Stripe test-mode checkout | yes, when credentials exist; otherwise skipped |
| `@quarantine` | Known-flaky, excluded until fixed (section 7) | no |

Projects select tags with `grep` **in the config**, not on the command line. A command-line
`--grep @regression` would also filter out the setup projects (their tests have no tag) and break
the run. To run a subset, use `--project`, for example `npx playwright test --project=regression`.
Playwright then runs that project's dependencies and teardown too.

### POM conventions

- One class per page or component, file `pages/<area>/<name>.page.ts`, class `<Name>Page`.
- Locators are created in the constructor. Tests never contain raw selectors.
- Take selectors from the source: layout files (`ui/layoutFiles/*.html`), C# views, or the
  collection XML. Never guess. Prefer ids that exist in the layout (`#scCheckoutTable`,
  `#input-promotion-code`, `#abCheckoutForm`), then `getByRole`.
- Methods are actions (`applyPromoCode(code)`) or assertions (`expectTotal(amount)`).
- Admin screens reuse the Contensive admin patterns: buttons are `input[type=submit]` with a
  `value` ("Save", "OK"), list views have per-column find inputs.

### Naming

- Spec files: `<feature>-<behaviour>.spec.ts`, e.g. `checkout-acceptall.spec.ts`.
- Test titles say what the user sees: `"guest with promo TESTE2E10 pays 10% less"`.
- Seeded records: `TEST-E2E-<Thing>` (stable, refreshed each run).
  Records created during a run: `TEST-E2E-<runId>-<Thing>`. Emails always use `@example.com`.

---

## 3. aoEcommerce coverage matrix

Rows are workflows, columns are customer types. Each cell says when the test is written:

- **pilot**: this job (phase 1)
- **next**: phase 2 workflow packs, in roughly the order listed
- **later**: after the next set, or needs a platform change first
- **n-a**: the combination does not exist

| Workflow \ Customer | Guest | Individual (logged in) | Organization account | Member pricing (group) | Invoice / net terms | Auto-pay (saved card) | Tax-exempt (unverified) | Admin / system |
|---|---|---|---|---|---|---|---|---|
| Catalog, cart, checkout | pilot | pilot | next | next | next | later (`@gateway`) | later | n-a |
| Promo code | pilot | next | later | later | later | n-a | n-a | n-a |
| Tax and shipping | next | next | later | n-a | later | n-a | later | n-a |
| Membership / subscription purchase | next | next | next | n-a | later | later | n-a | n-a |
| Recurring renewal (housekeep force-run + test date) | n-a | later | later | later | later | later | n-a | later |
| My Account (invoices, pay balance, update pay method, cancel/renew) | n-a | pilot (navigation and sections) / next (actions) | next | next | next | later | n-a | n-a |
| Pay invoice page | next | next | later | n-a | next | n-a | n-a | n-a |
| Donation | later | later | n-a | n-a | n-a | n-a | n-a | n-a |
| Admin money moves (invoice, charge, payment, refund, credit, chargeback, merge, GL export, housekeep force-run) | n-a | n-a | n-a | n-a | n-a | n-a | n-a | next (invoice, payment, credit) / later (refund, chargeback, merge, GL export) |
| Webhooks (Stripe, Authorize.Net silent post), API-level | n-a | n-a | n-a | n-a | n-a | n-a | n-a | later |
| Invoice PDF | n-a | next | later | n-a | next | n-a | n-a | next |

Notes:
- **Tax-exempt:** no tax-exempt account field was found in `Ecommerce.xml`. Only `Items.istaxable`
  exists. Confirm how tax exemption works before writing this column.
- **Donation:** the donation remote methods are guarded by a CSRF token, a honeypot, a per-visit
  cooldown and a per-IP hourly limit (`Controllers/DonationRemoteGuard.cs`). No reCAPTCHA was found
  in the repo. A test must post through the real form (so the CSRF token is present), leave the
  honeypot empty, and stay under the limits: one donation per run.
- **Recurring renewal** waits for the time-based approach in section 6 to be proven.
- **Webhooks** are tested with API requests (Playwright's `request` fixture), not a browser. Stripe
  payloads are signed with the sandbox webhook secret.

---

## 4. Test data and isolation on a shared site

test.sitefpo.com is shared. Jay also uses it for manual job testing, and a run can collide with that.
There is no database restore. The rules below keep runs repeatable and keep everyone else's state safe.

### Seed fixture (runs in the `seed` project)

1. **Lock** (see below).
2. **Save** the current values of the payment and test-mode site properties to
   `tests/e2e/.test-data/site-properties-snapshot.json`:
   `PaymentProcessMethod`, `Account Billing Test Mode`, `Account Billing Test Mode Date`,
   `Account Billing Test Mode Email to-address`, `OrderTestCCNumber`, `AuthorizeNetSandbox`,
   `AuthorizeNetTest`, `Account Billing Housekeep Enabled`. The `@gateway` pack also saves
   `StripeAccessKey`, `StripeWebhookSecret`, `AuthorizeNetLogin` and `AuthorizeNetTransactionKey`.
   Each property is read and written as its own core **Site Properties** record (`name` / `fieldvalue`),
   never by re-posting the whole Ecommerce Settings form, so unrelated settings and masked keys are never touched.
3. **Set** the e2e values: `PaymentProcessMethod=4` (AcceptAll), `Account Billing Test Mode` on,
   `Account Billing Test Mode Date` blank, `Account Billing Test Mode Email to-address` set to the
   redirect address.
4. **Create or refresh** the known records: items, a membership type, a promo code, people and
   accounts, all named `TEST-E2E-...`. "Refresh" means: find by name; create if missing; reset the
   fields the tests depend on (price, active, password) if present. Running the seed twice gives
   the same result.

If a snapshot file from an earlier run was never restored (for example, the machine crashed),
seed restores it **first**, before it takes a new snapshot. Otherwise the e2e values would be
saved as if they were the originals.

### Teardown (`cleanup` project, plus a `globalTeardown` safety net)

- Restore every saved property, read it back, and record `propertiesRestored: true` in
  `e2e-results/run-info.json`. This runs even when tests fail.
- Delete this run's **open (unpaid)** orders. Paid orders are kept. They belong to `TEST-E2E-`
  accounts, so they are easy to find, and deleting them would leave orphan transactions and GL rows.
- Never touch records without the `TEST-E2E-` prefix.
- A cleanup failure is reported, but it does not hide test results. A failed **property restore**
  fails the release step, because the shared site is then left in the wrong state.

### The lock

- Only one e2e run may use the site at a time. The lock is the site property `E2E Lock` on the
  test site. Its value is a small JSON string: who holds it, the repo, the machine, the run id and
  an expiry time (default 120 minutes).
- The release runner takes the lock **before** it deploys the build, and releases it in a
  `finally` block. A developer running the suite by hand gets the lock from the seed project.
- A lock past its expiry time is treated as stale and may be taken over, with a warning.
- The release machine also keeps a local lock file, so two runner windows cannot overlap.

### When a manual job build is installed

The agency-ops file `sites/test.sitefpo.com.md` is not on the release machine. Use one
site property instead:

- When Jay installs a job build for manual testing, he sets the site property `E2E Manual Job`
  to a short note, e.g. `20261105-aoEcommerce-authorizenet-rest, installed 2026-11-06`. He clears
  it when the job build is uninstalled.
- The runner reads it while taking the lock. If it is set, the runner prints a warning and writes
  it into the manifest notes. The run continues (advisory). Note that the e2e deploy **replaces**
  that job's build of the same collection. Agree with the job's tester before releasing.
- Payment settings changed for manual testing (for example Authorize.Net for the
  authorizenet-rest job) are safe: they are saved and restored by the seed and teardown.

---

## 5. Payments

### AcceptAll for workflow tests

Workflow tests use `PaymentProcessMethod=4` (AcceptAll, `Controllers/Gateways/AcceptAllGateway.cs`).
It approves any card synchronously, so the order completes inline. With `Account Billing Test Mode`
on, server-side charges (saved cards, refunds) also return "approved" without calling a gateway
(`Controllers/Gateways/GatewayController.cs:36`). The tests stay fast and do not depend on a
third party.

### The `@gateway` pack

A small number of tests prove that the real redirects still work:

- **Authorize.Net sandbox:** `PaymentProcessMethod=2`, `AuthorizeNetSandbox` on. Checkout
  redirects to `https://test.authorize.net/payment/payment?token=...`
  (`AuthorizeNetGateway.cs:108`). The test fills the hosted form with a public test card and
  checks the receipt back on the site.
- **Stripe test mode:** `PaymentProcessMethod=10` with a test-mode secret key. Checkout redirects
  to the hosted Stripe Checkout page. The test pays with `4242 4242 4242 4242`.
- Each test sets **test mode off** (otherwise server-side charges never reach the gateway), runs
  one checkout, and lets teardown restore everything.
- Hosted pages belong to Stripe and Authorize.Net and change without notice. Keep this pack small
  (one or two tests per gateway). If it breaks, look at their page first.

### Sandbox credentials

- Names: `E2E_STRIPE_SECRET_KEY`, `E2E_STRIPE_WEBHOOK_SECRET`, `E2E_AUTHNET_LOGIN_ID`,
  `E2E_AUTHNET_TRANSACTION_KEY`.
- They come from environment variables or `%LOCALAPPDATA%\contensive\e2e.env`, never from git.
- Sandbox/test-mode keys only. The pack refuses a Stripe key that does not start with `sk_test_`
  or `rk_test_`.
- When a gateway's credentials are missing, its tests call `test.skip()` with a clear message.
  The release step still passes and the report shows them as skipped.
- Test card numbers are public and may live in `helpers/test-data.ts`.

---

## 6. Time-based billing (renewals)

Renewals run only in the daily housekeep: `BatchProcessController.processBatch` creates orders
from recurring purchases, processes ready orders and retries unpaid ones. Waiting a year is not
possible, so tests move the clock instead:

1. Seed a `TEST-E2E-` account with a recurring purchase that renews soon.
2. Set the site property `Account Billing Test Mode Date` to a date after the renewal date.
   `GenericController.getRightNow()` returns this date (`GenericController.cs:192-201`), and
   `ApplicationModel.rightNow` calls it on every read (`ApplicationModel.cs:964-970`).
   `HousekeepController` and `BatchProcessController` use `app.rightNow` for their date checks.
3. Force the run: Account Manager > Tools > Housekeep > "Process Housekeep Now"
   (addon `Ecommerce Housekeep Tool` {D6776966-330D-48B8-8CAE-AE90E58B6227}). This sets
   `housekeepForceRun` and runs housekeep inline.
4. Check that the renewal order and charge exist, then clear the test date (teardown also does this).

**Risks (unverified):**
- Some code may still use `DateTime.Now` or SQL `getdate()` instead of `app.rightNow`.
  `DateTime.Now` appears in gateway, processing, UPS and log controllers. Before writing the
  subscription pack, check every date comparison on the renewal path.
- While the test date is set, the scheduled daily housekeep on the test site could process **all**
  accounts on the test site at that date. The subscription pack must turn
  `Account Billing Housekeep Enabled` off for the run. The seed already saves and restores it.
- Housekeep runs over the whole site, not only `TEST-E2E-` accounts. Keep the test site free of
  real-looking recurring data, or use `Account Billing Batch Process Only One` with care.

This is why the renewal row is "later" in the matrix.

---

## 7. Release integration

### New step order

```
merge → nuget → build → test → staging → e2e → smoke → publish
```

- `test` runs before `staging`. This matches `new-release.ps1` and the docs. Tier 0 also needs it,
  because its `test` step upgrades the local server that `staging` then skips. The runner's old
  order (`staging` before `test`) is corrected.
- `e2e` runs after `staging` and before the manual `smoke`. The automated check runs first. When
  e2e passes, smoke is a short look at the local site, or it can be marked skipped.
- Repos with no `e2e_path` in `repo-catalog.yml` get `e2e: skipped` automatically. A repo whose catalog entry declares a suite that is missing on the checked-out branch fails the step; it is never skipped.

### What the e2e step does

1. Loads secrets, checks that the target host is on the allow-list (`test.sitefpo.com`).
2. Takes the local lock and the site lock, and reads `E2E Manual Job`.
3. Deploys **the same collection zip that publish will upload** (the newest zip in the repo's
   deployment folder) to `https://test.sitefpo.com/installCollection`, using the bearer token
   from `CONTENSIVE_TEST_TOKEN`. It records the zip path and its SHA-256 in the manifest.
4. Waits until the site answers, runs `npm ci` and `npx playwright test`.
5. Reads `e2e-results/results.json`, copies the HTML report to
   `C:\Deployments\e2e-reports\<repo>\<runId>\`, and writes the report path into the manifest.
6. Releases the site lock and the local lock in `finally`, whatever happened.

### The gate and the override

- `publish` cannot start until `e2e` is `done` or `skipped`.
- Publish also checks that the zip it is about to upload has the same SHA-256 as the zip that e2e
  tested. If someone rebuilt in between, publish stops and asks for e2e to run again.
- To publish despite a failure (for example, a known flaky third-party page during an urgent
  release):

  ```powershell
  .\release-runner.ps1 -Mark done -Step e2e -Repo aoEcommerce -Reason "Stripe hosted page outage, AcceptAll regression passed"
  ```

  `-Reason` is required. It is written to the manifest as `override_reason` with the user name
  and time, shown on the dashboard as `OVRD`, and listed in `-Report`.

### Hotfix flow

Today: branch off `main`, build, deploy by hand, merge back to `dev`. New:

```powershell
.\release-runner.ps1 -Hotfix -Repo aoEcommerce -Branch hotfix/20261106-tax-rounding
```

This checks out the branch and runs build → test → e2e → publish from it, stopping at the first
failure. The state goes into its own `hotfix-manifest-<yyyyMMdd>-<repo>.yml`, so the monthly
manifest is not touched. Merge, nuget, staging and smoke are recorded as skipped. After publish,
the runner reminds you to merge the hotfix branch into `main` and `dev`. Hotfixes get the same
gate as monthly releases. That matters most for a hotfix, because it is the release made in a hurry.

### Reports and artifacts

| What | Where |
|---|---|
| HTML report (screenshots, video, traces on failure) | `C:\Deployments\e2e-reports\<repo>\<runId>\playwright-report\index.html`, path in manifest `report:` |
| Machine-readable results | same folder, `e2e-results\results.json`; counts in manifest `results:` |
| Run info (lock, manual-job warning, properties restored, cleanup) | same folder, `e2e-results\run-info.json` |
| Lock phase result (acquire/release) | same folder, `e2e-results\site-lock.json` |
| Tested artifact | manifest `artifact:` and `artifact_sha256:` |

Open a report with `npx playwright show-report <folder>` or just open `index.html`.

### Retries and flakiness

- In release runs `CI=1`, so each failing test is retried up to 2 times. A test that fails and then
  passes is **flaky**. The step still passes, and the manifest notes list the flaky count.
- A test that is flaky in **two release runs in a row** may be quarantined. Quarantine is never
  silent:
  1. Add `@quarantine` to the test's tags (the projects exclude it with `grepInvert`).
  2. Add a line to `tests/e2e/QUARANTINE.md`: test title, date, who, reason, and the issue to fix it.
  3. Fix or delete it within one release cycle.
- A test that fails all 3 attempts is a failure. Fix the code or the test, or use the override
  with a reason.

### Later: GitHub Actions (phase 4)

The `cicd-pipeline.md` plan puts builds on a self-hosted runner, with `environment: production`
approval replacing smoke. The e2e step maps onto that plan directly:

```yaml
  e2e:
    needs: build
    runs-on: self-hosted          # same box: needs the deployment folder and the test-site token
    concurrency: test-sitefpo     # replaces the local lock file; the site lock stays
    steps:
      - uses: actions/checkout@v4
      - name: Deploy to test site and run e2e
        shell: pwsh
        env:
          CONTENSIVE_TEST_TOKEN: ${{ secrets.CONTENSIVE_TEST_TOKEN }}
          E2E_BASE_URL: https://test.sitefpo.com
          E2E_ADMIN_USER: ${{ secrets.E2E_ADMIN_USER }}
          E2E_ADMIN_PASSWORD: ${{ secrets.E2E_ADMIN_PASSWORD }}
          E2E_CUSTOMER_PASSWORD: ${{ secrets.E2E_CUSTOMER_PASSWORD }}
        run: ...   # same steps as Invoke-StepE2E
      - uses: actions/upload-artifact@v4
        if: always()
        with: { name: playwright-report, path: tests/e2e/playwright-report }
  publish:
    needs: e2e                    # the gate
    environment: production       # human approval (replaces smoke)
```

The env var names stay the same, so moving to GitHub Actions only changes where the values come
from (Actions secrets instead of `e2e.env`).

---

## 8. "Describe → generate": adding tests by describing them

You do not need to write Playwright code by hand. Describe the need; Claude writes the POMs and
specs; you review the result and the run.

### What to give Claude

```
Add an e2e regression test to aoEcommerce.

Workflow:        Promo code at checkout
Customer type:   Guest (not logged in)
Starting data:   seeded item TEST-E2E-Widget ($10.00), promo TESTE2E10 (10% off all items)
Steps:           add 2 widgets to cart, apply TESTE2E10, check out with AcceptAll test card
Acceptance:      - cart shows a discount line of $2.00
                 - order total is $18.00 before tax/shipping
                 - receipt page shows the order number
Out of scope:    tax, shipping
```

### What Claude does

1. Reads `tests/e2e/playwright.config.ts`, the existing POMs, `helpers/seed-data.ts`, and this
   document.
2. Reads the real markup: the layout files in `ui/layoutFiles/`, the C# view that renders the
   page, and the collection XML for field names. It never guesses a selector.
3. Adds to the seed if new data is needed (`TEST-E2E-` prefix, idempotent).
4. Adds or extends a POM, then writes the spec with the right tag (`@regression`) and a title
   that states the expected outcome.
5. Runs it locally against the test site (only when you say so). It runs it twice to check it
   is stable, and gives you the report.
6. Updates the coverage matrix cell in section 3 (`next` → done, listed in the pack's README).

### Good acceptance criteria

- Observable on the page or in admin ("receipt shows ...", "account balance is $0.00").
- Exact amounts, using seeded prices so the numbers never change.
- One workflow per test. Split "buy, then cancel, then renew" into three tests that share seed data.

---

## 9. Rollout phases

| Phase | Scope | Done when |
|---|---|---|
| **1. Pilot (this job)** | Runner `e2e` step, gate, override, hotfix flow, lock. aoEcommerce suite: untrack secrets, new config with setup → smoke → seed → regression → cleanup, the 8 existing specs repaired and tagged (12 spec files in all), guest and individual checkout with AcceptAll, guest promo, `@gateway` skeleton | A monthly release and one hotfix pass through `e2e` with the report path in the manifest |
| **2. aoEcommerce workflow packs** | The "next" cells in section 3: org/member/invoice checkout, tax/shipping, membership purchase, My Account actions, pay invoice, admin invoice/payment/credit, invoice PDF; then the subscription pack after section 6 risks are checked | Each pack merged with its seed additions; the matrix is updated |
| **3. Other Tier 1 addons and core** | aoDesignBlocks, aoCrm, aoMCP, and Contensive5 `tests/e2e` (core admin/public smoke). Core needs a different deploy (server upgrade, not `/installCollection`) | Each repo has `e2e_path` in `repo-catalog.yml` and a passing suite |
| **4. GitHub Actions** | Move the step into the workflows from `cicd-pipeline.md`; secrets move to Actions secrets; `concurrency` replaces the local lock | Release runner triggers workflows; e2e is a required job before publish |

---

## 10. Security

- **No secrets in git.** `.env`, `.auth/`, `playwright-report/`, `test-results/`, `e2e-results/`
  and `.test-data/` are git-ignored, and none of them may be tracked. Secrets live in environment
  variables or `%LOCALAPPDATA%\contensive\e2e.env` on the release machine, later in GitHub
  Actions secrets. If a secret is ever committed, rotate it. Removing the file is not enough,
  because git history keeps it.
- **Test site only.** The runner and the Playwright config both refuse any host that is not
  on the allow-list (default `test.sitefpo.com`, set with `E2E_ALLOWED_HOSTS`). There is no
  default URL, so a missing `E2E_BASE_URL` stops the run.
- **Never production.** Never point `E2E_BASE_URL` at a client site, and never use live gateway
  keys. Stripe keys must be test keys (`sk_test_` / `rk_test_`), and Authorize.Net must be in
  sandbox mode.
- **Least privilege.** Use a dedicated admin account for e2e (`E2E_ADMIN_USER`), not a person's
  account, so it can be disabled or rotated on its own. Customer accounts are seeded `TEST-E2E-`
  people with `@example.com` emails.
- **Emails.** All ecommerce notifications are redirected by
  `Account Billing Test Mode Email to-address` during runs. Seeded people use `@example.com`, so a
  missed redirect still goes nowhere.
- **Reports can contain data.** Traces and screenshots show what the browser saw. They stay on
  the release machine (or as private CI artifacts) and are not attached to public issues.
