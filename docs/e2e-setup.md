# E2E setup: the secrets file

Read this when you have forgotten how the e2e release step gets its passwords. It is the single source of truth.
The runner and the suite print a short version of it, and point here, whenever something is missing.

## 1. What the file is and why it is outside git

The e2e suite logs in to the shared test site (https://test.sitefpo.com), so it needs a few secrets: an admin login,
a password for the test customers, and the token that lets the release runner install a build.
They live in **one file outside every repo**, so a password can never be committed by accident.

## 2. Where it is

```
%LOCALAPPDATA%\contensive\e2e.env
```

`%LOCALAPPDATA%` is normally `C:\Users\<you>\AppData\Local`, so the file is usually
`C:\Users\<you>\AppData\Local\contensive\e2e.env`. To open the folder: press Win+R, type `%LOCALAPPDATA%\contensive`, press Enter.

Real environment variables with the same names win over the file. A suite-only run may also use a git-ignored
`tests\e2e\.env` in aoEcommerce (order: environment variables, then `tests\e2e\.env`, then `e2e.env`).

## 3. Create it (one command)

```powershell
cd C:\Git\Contensive5\scripts\release
.\release-runner.ps1 -SetupE2E
```

This creates the folder and the file from the template `scripts\release\e2e.env.template` if they are missing,
opens the file in Notepad and prints a checklist. It never overwrites an existing file.
Replace every `<...>` placeholder, save, close Notepad. A value that is empty, looks like `<something>` or is `changeme` counts as **not set**.

No Contensive5 checkout on this machine (suite only)? `cd C:\Git\aoEcommerce\tests\e2e` then `npm run e2e:setup` does the same.

## 4. The keys and where each value comes from

| Key | Required | What it is and where to get it |
|-----|----------|-------------------------------|
| `E2E_BASE_URL` | yes | `https://test.sitefpo.com`. Must be https and an allowed host. |
| `E2E_ADMIN_USER` | yes | The dedicated `e2e-admin` account on the test site. If it is lost, in test.sitefpo.com admin > People create a new user with developer/admin rights. |
| `E2E_ADMIN_PASSWORD` | yes | The password of that account. |
| `E2E_CUSTOMER_PASSWORD` | yes | Any new password you make up. The suite creates the `TEST-E2E` customers with it. |
| `CONTENSIVE_TEST_TOKEN` | release machine only | The bearer token for `https://test.sitefpo.com/installCollection`: the same token the interactive deploy (`build-deploy-remote.cmd`) asks for ("Enter bearer token for ..."). A long-lived token is preferred, so you do not have to look it up each release. The suite alone and the cloud never need it. |
| `E2E_TEST_EMAIL_TO` | no | A mailbox Jay reads. Ecommerce emails triggered by the suite go there. Default `e2e-redirect@example.com`. |
| `E2E_ALLOWED_HOSTS` | no | Comma list of hosts the suite may run against. Default `test.sitefpo.com`. |
| `E2E_AUTHNET_LOGIN_ID`, `E2E_AUTHNET_TRANSACTION_KEY` | no | Authorize.Net **sandbox** keys (developer.authorize.net, Sandbox account, API Credentials & Keys). Without them the `@gateway` test skips. |
| `E2E_STRIPE_SECRET_KEY`, `E2E_STRIPE_WEBHOOK_SECRET` | no | Stripe **test mode** keys (`sk_test_...`, `whsec_...`; Stripe dashboard, test mode on, Developers). Without them the Stripe test skips. |

## 5. Check it

```powershell
cd C:\Git\Contensive5\scripts\release
.\release-runner.ps1 -CheckE2E
```

It shows where each key was found, and per key `OK`, `MISSING`, `PLACEHOLDER` or `INVALID`, plus whether `node`, `npm.cmd`
and `npx.cmd` are found. It never prints a value (only the length) and changes nothing. Exit code 0 means ready, 1 means not.
From the suite folder: `npm run e2e:check`.

## 6. Other machines and the cloud

- Another Windows machine: repeat step 3 there, or set environment variables.
- Linux, macOS, the cloud, CI: set environment variables with **the same names**. `npm run e2e:setup` prints the list.
- The cloud and CI never need `CONTENSIVE_TEST_TOKEN`: only `release-runner.ps1` deploys.

## 7. When a password is rotated

1. Change it on the test site (admin > People).
2. Open the file: `.\release-runner.ps1 -SetupE2E` (opens the existing file, changes nothing), edit the value, save.
3. Run `-CheckE2E`.
4. For `E2E_CUSTOMER_PASSWORD`, change the value only. The next seed run applies it to the `TEST-E2E` customers.

## 8. If something prints "e2e setup incomplete"

The block lists the names that are wrong, the path of the file and whether it exists. Run `-SetupE2E`, fix the values, run `-CheckE2E`, run the step again.

See also: [release-workflow.md](release-workflow.md) (Step 4b), [e2e-testing-strategy.md](e2e-testing-strategy.md).
Keep in sync: the template text lives in `scripts/release/e2e.env.template` and, copied, in aoEcommerce `tests/e2e/scripts/e2e-setup.mjs`.
