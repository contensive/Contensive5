# Automated SSL Certificate Provisioning via Let's Encrypt

## Overview

Automate SSL certificate creation during `cc.exe -nf appName domainName` and renewal every 60 days using the Certes ACME client library, eliminating the manual win-acme step.

## Architecture Decision

- **ACME Client Library:** [Certes](https://github.com/fszlin/certes) (MIT license, NuGet, .NET Standard 2.0)
- **Challenge Type:** HTTP-01 via static file in wwwFiles (`/.well-known/acme-challenge/{token}`)
- **IIS Binding:** New iisUtil command `verify-https-binding` to add HTTPS binding with certificate thumbprint
- **Account Key Storage:** `ServerConfigBaseModel.secrets` list (new property, matching `AppConfigModel.secrets` pattern)
- **Certificate Renewal:** Contensive addon with a `processInterval` of 86400 minutes (60 days), running in the existing task scheduler service

## Implementation Steps

### Step 1: Add `secrets` property to `ServerConfigBaseModel`

Add a new abstract property to `ServerConfigBaseModel` following the same pattern as `AppConfigModel.secrets`:

**File:** `source/CPBase/BaseModels/ServerConfigBaseModel.cs`

```csharp
/// <summary>
/// Server-level secrets stored as name/value pairs.
/// Used for ACME account keys and other server-scoped credentials.
/// When useSecretManager is true, these are stored in AWS Secrets Manager.
/// </summary>
public abstract List<NameValueBaseModel> secrets { get; set; }
```

**File:** `source/Processor/Models/Domain/ServerConfigModel.cs`

Add the concrete implementation with the same lazy-init pattern used in `AppConfigModel`:

```csharp
public override List<NameValueBaseModel> secrets {
    get {
        if (_secrets is not null) { return _secrets; }
        _secrets = [];
        return _secrets;
    }
    set {
        _secrets = value;
    }
}
private List<NameValueBaseModel> _secrets;
```

**Secret key names:**
- `acmeAccountKeyPem` — the ACME account private key in PEM format
- `acmeContactEmail` — the email registered with Let's Encrypt

### Step 2: Add Certes NuGet package

**File:** `source/Processor/Processor.csproj`

Add NuGet reference:
```xml
<PackageReference Include="Certes" Version="3.*" />
```

### Step 3: Create `CertificateController`

**File:** `source/Processor/Controllers/CertificateController.cs`

A static controller with two public methods:

#### `requestCertificateAsync(CPClass cp, string domainName)`

Called during `-nf` provisioning. Flow:

1. Load or create the ACME account:
   - Read `acmeAccountKeyPem` from `cp.core.serverConfig.secrets`
   - If empty, create a new account with `AcmeContext.NewAccount()` using the server config's `defaultEmailContact`
   - Save the new account key PEM back to `secrets` and persist the server config
2. Create a certificate order for the domain via `acme.NewOrder(new[] { domainName })`
3. Get the HTTP-01 challenge token and key authorization
4. Write the challenge file to wwwFiles: `cp.WwwFiles.Save($".well-known/acme-challenge/{token}", keyAuthz)`
5. Tell Let's Encrypt to validate: `await httpChallenge.Validate()`
6. Poll until the challenge status is `valid` (with timeout and retry logic)
7. Generate a private key and finalize the order: `await order.Generate(new CsrInfo { CommonName = domainName }, privateKey)`
8. Export as PFX: `cert.ToPfx(privateKey).Build(domainName, password)`
9. Install PFX into Windows certificate store (`StoreName.My`, `StoreLocation.LocalMachine`) with `MachineKeySet | PersistKeySet` flags
10. Call `WebServerController` to add HTTPS binding (Step 4)
11. Delete the challenge file from wwwFiles
12. Store the certificate thumbprint and expiration in a site property for the renewal addon to check

#### `renewCertificateIfNeeded(CPClass cp)`

Called by the renewal addon. Flow:

1. Read site properties: `certificateThumbprint`, `certificateExpiration`, domain name from `appConfig.domainList`
2. If expiration is more than 30 days away, exit (no-op)
3. Call `requestCertificateAsync()` to get a new certificate
4. Remove the old certificate from the store by thumbprint
5. Update the site properties with the new thumbprint and expiration

#### Error handling

- All errors logged via `cp.Site.ErrorReport(ex)`
- Certificate request failures during `-nf` should log a warning and continue — the site works on HTTP, the user can retry manually or wait for the renewal addon
- Renewal failures should log but not throw — the addon will retry on the next interval

### Step 4: Add HTTPS binding support to `WebServerController` and iisUtil

**File:** `source/Processor/Controllers/WebServerController.cs`

Add a new method:

```csharp
/// <summary>
/// Add or update an HTTPS binding for the site with the specified certificate thumbprint.
/// </summary>
public void verifyHttpsBinding(string appName, string domainName, string certificateThumbprint) {
    try {
        runIisUtil($"verify-https-binding --name \"{appName}\" --domain \"{domainName}\" --thumbprint \"{certificateThumbprint}\"");
    } catch (Exception ex) {
        logger.Error($"{core.logCommonMessage}", ex, "verifyHttpsBinding");
        throw;
    }
}
```

**iisUtil changes:** Add a `verify-https-binding` command that:

1. Finds the site by name
2. Looks for an existing HTTPS binding for the domain
3. If found, updates the certificate thumbprint
4. If not found, adds a new `*:443:domainName` binding with the certificate hash from the `My` store, SNI enabled

### Step 5: Integrate into `NewAppFrameworkCmd.executeAsync`

**File:** `source/Cli/Views/NewAppFrameworkCmd.cs`

After the existing `verifySite` call (line 341) and after the database upgrade (line 377), add the certificate provisioning step:

```csharp
// -- provision SSL certificate via Let's Encrypt
if (!string.IsNullOrEmpty(domainName) && !domainName.EndsWith(".sitefpo.com", StringComparison.OrdinalIgnoreCase)) {
    Console.WriteLine($"NOTE: The domain [{domainName}] must have DNS configured to point to this server for the SSL certificate to be valid.");
    Console.WriteLine("Requesting SSL certificate from Let's Encrypt...");
    try {
        await CertificateController.requestCertificateAsync(cp, domainName);
        Console.WriteLine("SSL certificate installed and HTTPS binding configured.");
    } catch (Exception ex) {
        Console.WriteLine($"SSL certificate request failed: {ex.Message}");
        Console.WriteLine("The site is available on HTTP. You can retry certificate provisioning later or use an external ACME client.");
    }
}
```

Key behaviors:
- **Skips `.sitefpo.com` domains** — these are placeholder domains that won't pass ACME validation
- **Prints a DNS reminder** when a custom domain is provided
- **Non-fatal** — if certificate provisioning fails, the site still works on HTTP

### Step 6: Create the certificate renewal addon

This is a process addon registered in the Contensive5 base collection XML.

**Addon definition in collection XML:**

```xml
<Addon>
    <Name>Certificate Renewal Process</Name>
    <Guid>{GENERATE-NEW-GUID}</Guid>
    <ProcessInterval>86400</ProcessInterval>
    <DotNetClass>Contensive.Processor.Addons.CertificateRenewalProcess</DotNetClass>
    <ProcessRunOnce>0</ProcessRunOnce>
</Addon>
```

**File:** `source/Processor/Addons/CertificateRenewalProcess.cs`

```csharp
namespace Contensive.Processor.Addons {
    public class CertificateRenewalProcess : AddonBaseClass {
        public override object Execute(CPBaseClass cp) {
            CertificateController.renewCertificateIfNeeded((CPClass)cp);
            return "";
        }
    }
}
```

The `processInterval` of 86400 minutes = 60 days. The task scheduler picks up addons with a non-zero `processInterval` and runs them when `ProcessNextRun` has passed.

### Step 7: Site properties for certificate tracking

Stored per-app via `cp.core.siteProperties`:

| Property Name | Description |
|---|---|
| `certificateThumbprint` | Thumbprint of the currently installed certificate |
| `certificateExpiration` | ISO 8601 date when the certificate expires |
| `certificateLastRenewal` | ISO 8601 date of the last successful renewal |

These are standard site properties — no collection XML changes needed for them.

## Let's Encrypt Considerations

- **Rate limits:** 50 certificates per registered domain per week (production). Use the staging server (`WellKnownServers.LetsEncryptStagingV2`) during development and testing.
- **Port 80 requirement:** The HTTP-01 challenge requires port 80 to be reachable from the internet. The IIS site must be running and serving requests on the domain before certificate provisioning.
- **90-day expiration:** Let's Encrypt certificates are valid for 90 days. The 60-day renewal interval provides a 30-day buffer.
- **Account reuse:** The ACME account key is stored once in server config secrets and reused across all apps and renewals on the server.

## Files Modified

| File | Change |
|---|---|
| `source/CPBase/BaseModels/ServerConfigBaseModel.cs` | Add abstract `secrets` property |
| `source/Processor/Models/Domain/ServerConfigModel.cs` | Add concrete `secrets` implementation |
| `source/Processor/Processor.csproj` | Add Certes NuGet package |
| `source/Processor/Controllers/CertificateController.cs` | New file — ACME flow and certificate management |
| `source/Processor/Controllers/WebServerController.cs` | Add `verifyHttpsBinding` method |
| `source/Cli/Views/NewAppFrameworkCmd.cs` | Add certificate provisioning after site creation |
| `source/Processor/Addons/CertificateRenewalProcess.cs` | New file — renewal addon |
| Collection XML | Add addon definition for `CertificateRenewalProcess` |
| iisUtil project | Add `verify-https-binding` command |

## Compatibility

- **CPBase binary compatibility:** The new `secrets` property on `ServerConfigBaseModel` is additive (new abstract property). Existing compiled addons do not reference `ServerConfigBaseModel` directly — it is only used internally by Processor and CLI. This is safe.
- **External ACME tools:** The static file challenge approach (`/.well-known/acme-challenge/`) is the same mechanism external tools like win-acme use. Both can coexist — the file is written, validated, and deleted.
- **Non-Windows:** `CertificateController` should check `RuntimeInformation.IsOSPlatform(OSPlatform.Windows)` and skip IIS binding on non-Windows, matching the pattern in `WebServerController`.
