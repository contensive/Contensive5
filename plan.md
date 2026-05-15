# Plan: Add "Login By Email" Custom Blocking Method

## Overview

Add a new blocking method (value `5`) called "Login By Email" to the Page Content blocking system. When selected, unauthenticated users are prompted for their email, sent a One Time Password (OTP), and logged in after entering the OTP — with different flows for existing vs. new users.

---

## Step 1: Update Collection XML — Add "Login By Email" to the LookupList

**File:** `source/Processor/aoBase51.xml` (line 1153)

Change the `BlockSourceID` field's `LookupList` from:
```
"Custom Blocking Message,Login Form,Registration Form,Age Restricted Content Block"
```
to:
```
"Custom Blocking Message,Login Form,Registration Form,Age Restricted Content Block,Login By Email"
```

This makes "Login By Email" appear as the 5th option (blockSourceId = 5) in the Blocking Tab dropdown.

---

## Step 2: Add New CDef for OTP Records

**File:** `source/Processor/aoBase51.xml`

Add a new `<CDef>` for table `LoginByEmailOtp` with fields:
- `Name` (Text) — auto-generated identifier
- `email` (Text) — the email address the OTP was sent to
- `otp` (Text) — the generated OTP code (6 digits)
- `expires` (Date) — expiration timestamp
- `used` (Boolean) — whether the OTP has been consumed

---

## Step 3: Add Constants

**File:** `source/Processor/Constants.cs`

```csharp
internal const int ContentBlockWithLoginByEmail = 5;

// Layout GUIDs for Login By Email blocking
public const string layoutLoginByEmailGuid = "{generated-guid}";
public const string layoutLoginByEmailName = "Login By Email Form Layout";
public const string layoutLoginByEmailCdnPathFilename = @"baseAssets\LoginByEmailForm.html";

public const string layoutLoginByEmailOtpGuid = "{generated-guid}";
public const string layoutLoginByEmailOtpName = "Login By Email OTP Form Layout";
public const string layoutLoginByEmailOtpCdnPathFilename = @"baseAssets\LoginByEmailOtpForm.html";

public const string layoutLoginByEmailNewUserOtpGuid = "{generated-guid}";
public const string layoutLoginByEmailNewUserOtpName = "Login By Email New User OTP Form Layout";
public const string layoutLoginByEmailNewUserOtpCdnPathFilename = @"baseAssets\LoginByEmailNewUserOtpForm.html";

// System email GUIDs for Login By Email
public const string systemEmailLoginByEmailExistingUserGuid = "{generated-guid}";
public const string systemEmailLoginByEmailNewUserGuid = "{generated-guid}";
```

---

## Step 4: Create Database Model for OTP

**File:** `source/Models/Models/Db/LoginByEmailOtpModel.cs` (new file)

```csharp
public class LoginByEmailOtpModel : DbBaseModel {
    public static DbBaseTableMetadataModel tableMetadata { get; }
        = new DbBaseTableMetadataModel("Login By Email Otp", "LoginByEmailOtp", false);
    public string email { get; set; }
    public string otp { get; set; }
    public DateTime expires { get; set; }
    public bool used { get; set; }
}
```

---

## Step 5: Create Layout HTML Files

### 5a. Email Request Form (`baseAssets/LoginByEmailForm.html`)
- Caption: "Enter Your Email to Access This Page"
- Email input with `type="email"` + client-side JavaScript regex validation
- Submit button
- JavaScript AJAX call to the `SubmitLoginByEmailRequest` remote method

### 5b. Existing User OTP Form (`baseAssets/LoginByEmailOtpForm.html`)
- OTP input field (6 digits)
- Hidden field for email
- Submit button
- JavaScript AJAX call to `SubmitLoginByEmailOtp` remote method

### 5c. New User OTP + Registration Form (`baseAssets/LoginByEmailNewUserOtpForm.html`)
- Instructions: "A code was sent to {{email}}. Check your email and enter the code below."
- First Name, Last Name inputs
- Email displayed (read-only)
- OTP input field
- Submit button
- JavaScript AJAX call to `SubmitLoginByEmailNewUserOtp` remote method

---

## Step 6: Create Addon — SubmitLoginByEmailRequest

**File:** `source/Processor/Addons/CustomBlocking/SubmitLoginByEmailRequest.cs` (new)

Logic:
1. Get `email` from doc properties, validate format server-side
2. Generate a 6-digit OTP code
3. Create a `LoginByEmailOtpModel` record (email, otp, expires = now + 10 min, used = false)
4. Query `ccmembers` for existing user with this email
5. If user **exists**: send OTP via "existing user" system email → return `{ success: true, isNewUser: false }`
6. If user **does not exist**: send OTP via "new user" system email → return `{ success: true, isNewUser: true }`

The OTP is appended to the system email body in the send method.

---

## Step 7: Create Addon — SubmitLoginByEmailOtp (Existing User)

**File:** `source/Processor/Addons/CustomBlocking/SubmitLoginByEmailOtp.cs` (new)

Logic:
1. Get `email` and `otp` from doc properties
2. Query `LoginByEmailOtp` for matching record where `expires > now` and `used = false`
3. If not found: return error
4. Mark OTP record as `used = true`
5. Find user in `ccmembers` by email
6. Call `cp.User.LoginByID(userId)`
7. Return `{ success: true }`

---

## Step 8: Create Addon — SubmitLoginByEmailNewUserOtp (New User)

**File:** `source/Processor/Addons/CustomBlocking/SubmitLoginByEmailNewUserOtp.cs` (new)

Logic:
1. Get `email`, `otp`, `firstName`, `lastName` from doc properties
2. Validate OTP (same as Step 7)
3. Mark OTP record as `used = true`
4. Check current session's people record:
   - If current record **has no email**: update it with email, firstName, lastName → login with this record
   - If current record **already has an email**: create a **new** PersonModel with firstName, lastName, email → login with the new record
5. Return `{ success: true }`

---

## Step 9: Register Addons in Collection XML

**File:** `source/Processor/aoBase51.xml`

Add three `<Addon>` entries with `<RemoteMethod>true</RemoteMethod>`:
1. `SubmitLoginByEmailRequest` → `Contensive.Processor.Addons.CustomBlocking.SubmitLoginByEmailRequest`
2. `SubmitLoginByEmailOtp` → `Contensive.Processor.Addons.CustomBlocking.SubmitLoginByEmailOtp`
3. `SubmitLoginByEmailNewUserOtp` → `Contensive.Processor.Addons.CustomBlocking.SubmitLoginByEmailNewUserOtp`

---

## Step 10: Add Case to PageManagerController Switch

**File:** `source/Processor/Controllers/PageManagerController.cs` (~line 790, before `default:`)

```csharp
case ContentBlockWithLoginByEmail: {
    if (core.session.isAuthenticated) {
        ContentBlocked = false;
    } else {
        result = core.cpParent.Layout.GetLayout(
            Constants.layoutLoginByEmailGuid,
            Constants.layoutLoginByEmailName,
            Constants.layoutLoginByEmailCdnPathFilename);
    }
    break;
}
```

The client-side JavaScript handles form transitions (email → OTP or new-user form) via AJAX responses.

---

## Step 11: Build and Test

1. Build the solution to verify compilation
2. Test flows:
   - Authenticated user → passes through, no block
   - Unauthenticated, existing email → email form → OTP email → OTP form → logged in
   - Unauthenticated, new email → email form → OTP email → name + OTP form → registered + logged in

---

## File Change Summary

| File | Action |
|------|--------|
| `source/Processor/aoBase51.xml` | Modify LookupList, add OTP CDef, add 3 Addon registrations |
| `source/Processor/Constants.cs` | Add `ContentBlockWithLoginByEmail = 5`, layout GUIDs, email GUIDs |
| `source/Models/Models/Db/LoginByEmailOtpModel.cs` | **New** — OTP database model |
| `source/Processor/Addons/CustomBlocking/SubmitLoginByEmailRequest.cs` | **New** — email submission + OTP generation |
| `source/Processor/Addons/CustomBlocking/SubmitLoginByEmailOtp.cs` | **New** — existing user OTP verification + login |
| `source/Processor/Addons/CustomBlocking/SubmitLoginByEmailNewUserOtp.cs` | **New** — new user OTP verification + registration + login |
| `source/Processor/Controllers/PageManagerController.cs` | Add `case ContentBlockWithLoginByEmail` to switch |
| `source/Processor/baseAssets/LoginByEmailForm.html` | **New** — email entry form with client-side validation |
| `source/Processor/baseAssets/LoginByEmailOtpForm.html` | **New** — OTP entry form for existing users |
| `source/Processor/baseAssets/LoginByEmailNewUserOtpForm.html` | **New** — OTP + registration form for new users |
