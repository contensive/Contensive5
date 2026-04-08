Review this Contensive addon collection for conformance with all Contensive patterns, security practices, and best practices. Produce a structured report with prioritized findings.

## Step 1 — Load the pattern documentation

Fetch each of the following documents. Read all of them before beginning the review.

- https://raw.githubusercontent.com/contensive/Contensive5/refs/heads/master/patterns/security-best-practices.md
- https://raw.githubusercontent.com/contensive/Contensive5/refs/heads/master/patterns/authentication-pattern.md
- https://raw.githubusercontent.com/contensive/Contensive5/refs/heads/master/patterns/best-practices-pattern.md
- https://raw.githubusercontent.com/contensive/Contensive5/refs/heads/master/patterns/addon-collection-pattern.md
- https://raw.githubusercontent.com/contensive/Contensive5/refs/heads/master/patterns/remote-method-pattern.md
- https://raw.githubusercontent.com/contensive/Contensive5/refs/heads/master/patterns/page-widget-pattern.md
- https://raw.githubusercontent.com/contensive/Contensive5/refs/heads/master/patterns/process-addon-pattern.md
- https://raw.githubusercontent.com/contensive/Contensive5/refs/heads/master/patterns/database-models-pattern.md
- https://raw.githubusercontent.com/contensive/Contensive5/refs/heads/master/patterns/build-script-pattern.md

If the user passed an argument (e.g. `/review-collection security`), load only the pattern docs relevant to that focus area. For `security`, load only the security and authentication docs.

## Step 2 — Locate and read the project

Find and read:
- All `.cs` source files under `server/` or `source/`
- All collection XML files under `Collections/`
- `scripts/build.ps1` and `scripts/build.cmd`
- Any `.md` files in `helpfiles/` or `ui/helpFiles/`

Use Glob to find the files, then read them. If the project is large, prioritize: collection XML first, then all addon Execute methods, then controllers/services.

## Step 3 — Produce the review report

Structure the report into the following sections. Only include a section if there are findings for it. Within each section, cite the specific file and line number for every finding.

---

### 🔴 Security (fix before shipping)

Check against `security-best-practices.md` and `authentication-pattern.md`:

- Any addon Execute method that performs a sensitive operation without first calling `core.session.isAuthenticated`
- Any authorization check that reads user properties directly (e.g. `user.admin`, `user.id > 0`) instead of using `isAuthenticatedAdmin()`, `isAuthenticatedDeveloper()`, or `isEditing()`
- Any place where a recognized-but-unauthenticated user could reach protected functionality
- Remote methods (`<RemoteMethod>Yes</RemoteMethod>`) that lack authentication checks
- Error responses that leak internal details (stack traces, SQL, internal paths)
- Sensitive data exposure in client-side code or public endpoints

---

### 🟠 Error Handling (required by best-practices pattern)

Check against `best-practices-pattern.md`:

- Addon Execute methods missing the outer `try/catch` with `cp.Site.ErrorReport(ex)` and a user-friendly return string
- Non-addon methods that swallow exceptions silently (no `cp.Site.ErrorReport` and no rethrow)
- Non-addon methods that rethrow without reporting (missing `cp.Site.ErrorReport(ex)` before `throw`)
- Critical workflows where a non-critical step (logging, telemetry) can interrupt the main flow

---

### 🟡 Collection XML (addon-collection-pattern)

Check against `addon-collection-pattern.md`:

- Missing or malformed collection GUID (must be wrapped in braces)
- CDef fields missing required attributes (`Guid`, `FieldType`, `EditSortPriority`)
- Addon elements missing `<DotNetClass>` or pointing to a class that doesn't exist in the source
- `<RemoteMethod>Yes</RemoteMethod>` addons that should also set `<BlockEditTools>Yes</BlockEditTools>`
- Resource entries referencing zip names that don't match the build script's output
- Missing `<ImportCollection>` entries for dependencies that are referenced in `<IncludeAddon>` elements
- `OnInstallAddonGuid` referencing a GUID not defined in the collection

---

### 🟡 Build Script (build-script-pattern)

Check against `build-script-pattern.md`:

- `scripts/build.cmd` is not the thin wrapper pattern (should only call `build.ps1` via PowerShell)
- `scripts/build.ps1` contains build step logic instead of only configuration and a single `Invoke-ContensiveBuild` call
- `CollectionDlls` list in `build.ps1` does not match the DLLs actually produced by the solution
- `DeploymentRoot` path does not follow the `C:\deployments\{name}` convention
- `CleanFolders` missing project `bin\` or `obj\` directories

---

### 🟡 Addon Pattern Conformance

Check the type of each addon in the collection XML, then verify against the relevant pattern doc:

- **Remote methods**: check against `remote-method-pattern.md`
- **Page/content widgets**: check against `page-widget-pattern.md`
- **Background tasks**: check against `process-addon-pattern.md`

Common issues:
- Addon class does not inherit from the correct base class for its type
- Addon does not match its declared type (e.g. marked `<RemoteMethod>Yes</RemoteMethod>` but returns HTML)
- Background task addon missing `<ProcessInterval>`

---

### 🟢 Code Style & Best Practices

- String concatenation used where string interpolation is more readable
- HTML CSS classes using `js-` prefix in CSS stylesheets, or non-`js-` selectors used as JavaScript binding targets
- Any other pattern deviations not covered above that are worth noting

---

### ✅ Summary

End the report with:
1. A one-paragraph overall assessment
2. A count of findings by severity (🔴 critical, 🟠 required, 🟡 pattern deviation, 🟢 style)
3. The top 3 highest-priority items to address first, by file and line number
