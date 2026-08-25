# Filename Sanitization Configuration Strategy Analysis

## Current Implementation: ServerConfig Level

The filename sanitization level was implemented as a **ServerConfig** property, meaning it applies to all applications on a server/server group.

```csharp
// Location: ServerConfigBaseModel.cs
public abstract FilenameSanitizationLevelEnum filenameSanitizationLevel { get; set; }

// Default: Moderate
// Scope: All applications on the server
```

---

## Alternative Strategies Considered

This document analyzes three possible configuration strategies for the filename sanitization feature:

1. **ServerConfig** (✅ Currently Implemented)
2. **AppConfig** (Alternative #1)
3. **Site Properties** (Alternative #2)

---

## Strategy 1: ServerConfig (Current Implementation)

### Architecture

```
ServerConfig (config.json)
  ├─ filenameSanitizationLevel: Moderate (default)
  │
  ├─ App 1 (inherits server-level setting)
  │   ├─ Domain A (uses server-level setting)
  │   └─ Domain B (uses server-level setting)
  │
  └─ App 2 (inherits server-level setting)
      ├─ Domain C (uses server-level setting)
      └─ Domain D (uses server-level setting)
```

### Advantages ✅

#### 1. **Consistent Security Policy Across All Apps**
- **Benefit:** IT administrators set a single policy that applies server-wide
- **Use Case:** Hosting provider enforcing security standards for all client sites
- **Example:** "All sites on this server must use Moderate or Strict mode for compliance"

#### 2. **Simplest Configuration**
- **Benefit:** One setting to manage, one place to look
- **Use Case:** Small organizations with few applications
- **Example:** Single organization running 3-5 apps on one server - consistent policy is ideal

#### 3. **Prevents Security Downgrade**
- **Benefit:** Individual app owners cannot lower security below server policy
- **Use Case:** Multi-tenant hosting where tenant admins shouldn't control security
- **Example:** SaaS provider ensures all tenants meet minimum security standards

#### 4. **Performance Optimization**
- **Benefit:** Setting loaded once at server initialization, not per-request or per-app
- **Use Case:** High-traffic environments
- **Technical:** No database lookup, no per-app config parsing

#### 5. **Matches Other Infrastructure Settings**
- **Benefit:** Aligns with existing patterns for infrastructure-level settings
- **Examples:**
  - `isLocalFileSystem` - S3 vs local storage (server-level decision)
  - `awsBucketName` - All apps use same S3 bucket
  - `enableRemoteCache` - All apps use same ElastiCache
  - `defaultDataSourceSecure` - Database encryption for all apps

#### 6. **Ideal for Homogeneous Environments**
- **Benefit:** Perfect when all apps have similar requirements
- **Use Case:** Single organization with multiple related apps
- **Example:** Intranet, public site, and partner portal all using same security policy

#### 7. **Easy Compliance Auditing**
- **Benefit:** Single setting to verify for security audits
- **Use Case:** ISO 27001, SOC 2, HIPAA compliance
- **Example:** Auditor asks "What's your file sanitization policy?" - one clear answer

### Disadvantages ❌

#### 1. **No Per-App Flexibility**
- **Problem:** All apps must use the same sanitization level
- **Impact:** Can't have one app support Unicode (Moderate) while another is ASCII-only (Strict)
- **Example:** Medical records app needs Strict, but international marketing site needs Moderate
- **Workaround:** Run separate servers or manually modify code

#### 2. **Not Ideal for Multi-Tenant Hosting**
- **Problem:** Different tenants may have different requirements
- **Impact:** Tenant A (international NGO) needs Unicode support, Tenant B (US government) needs ASCII-only
- **Example:** Hosting provider can't offer per-tenant security customization
- **Workaround:** Requires separate server groups per security level

#### 3. **Difficult for Mixed Compliance Requirements**
- **Problem:** One app may need PCI-DSS compliance (strict), another doesn't
- **Impact:** Either over-restrict non-compliant apps or under-restrict compliant ones
- **Example:** E-commerce app (strict) and blog (moderate) on same server
- **Workaround:** Separate infrastructure

#### 4. **No User Self-Service**
- **Problem:** Requires server admin access to change
- **Impact:** Tenant admins can't adjust for their needs
- **Example:** Customer onboarding requires IT ticket to adjust security level
- **Workaround:** Provide limited admin UI for server config

#### 5. **Migration Challenges**
- **Problem:** When migrating from Strict → Moderate, all apps change at once
- **Impact:** Can't test one app first
- **Example:** Upgrade causes unexpected behavior in all apps simultaneously
- **Workaround:** Requires thorough testing in staging environment

---

## Strategy 2: AppConfig (Alternative #1)

### Architecture

```
ServerConfig (config.json)
  ├─ filenameSanitizationLevel: Moderate (server default/fallback)
  │
  ├─ App 1 (appConfig)
  │   ├─ filenameSanitizationLevel: Strict (override)
  │   ├─ Domain A (uses app-level setting: Strict)
  │   └─ Domain B (uses app-level setting: Strict)
  │
  └─ App 2 (appConfig)
      ├─ filenameSanitizationLevel: Permissive (override)
      ├─ Domain C (uses app-level setting: Permissive)
      └─ Domain D (uses app-level setting: Permissive)
```

### How It Would Work

```csharp
// In AppConfigBaseModel.cs
public abstract FilenameSanitizationLevelEnum filenameSanitizationLevel { get; set; }

// At file upload time (ConfigurationClass)
var appConfig = AppConfigModel.getObject(core, serverConfig, appName);
string sanitized = FileController.sanitizeFilename(
    filename,
    appConfig.filenameSanitizationLevel ?? serverConfig.filenameSanitizationLevel // Fallback
);
```

### Advantages ✅

#### 1. **Per-Application Customization**
- **Benefit:** Each app can have its own security policy
- **Use Case:** Multi-tenant hosting with different tenant requirements
- **Example:**
  - App 1 (Medical Records): Strict (HIPAA compliance)
  - App 2 (International Blog): Moderate (Unicode support)
  - App 3 (Internal Tool): Permissive (trusted environment)

#### 2. **Tenant Self-Service**
- **Benefit:** App admins can adjust their own security level
- **Use Case:** SaaS platform where tenants manage their own settings
- **Example:** Admin UI: "Choose your filename security level: Strict/Moderate/Permissive"
- **Implementation:** App config UI in admin site

#### 3. **Gradual Migration**
- **Benefit:** Test new security level on one app before rolling out to others
- **Use Case:** Upgrading from Strict → Moderate server-wide
- **Example:**
  1. Test app: Set to Moderate
  2. Monitor for issues
  3. Roll out to remaining apps one by one

#### 4. **Mixed Compliance Requirements**
- **Benefit:** Apps with different compliance needs can coexist
- **Use Case:** Organization with some PCI-DSS apps and some non-regulated apps
- **Example:**
  - E-commerce app: Strict (PCI-DSS)
  - Company blog: Moderate (user-friendly)
  - Internal wiki: Permissive (fully trusted)

#### 5. **Better for Service Providers**
- **Benefit:** Hosting providers can offer security level as a product feature
- **Use Case:** "Premium Plan includes custom security settings"
- **Example:** Basic tier: Server default, Pro tier: Custom app-level setting

#### 6. **Matches Existing AppConfig Patterns**
- **Benefit:** Aligns with other app-specific security settings
- **Examples:**
  - `privateKey` - Per-app encryption key
  - `emailLimit` - Per-app email quota
  - `adminRoute` - Per-app admin path

### Disadvantages ❌

#### 1. **Increased Configuration Complexity**
- **Problem:** N apps = N configuration settings to manage
- **Impact:** More places to check, more documentation needed
- **Example:** 20 apps = 20 places where sanitization level might differ
- **Mitigation:** Provide config summary dashboard

#### 2. **Security Inconsistency Risk**
- **Problem:** Developer might accidentally set one app to Permissive
- **Impact:** Inadvertent security hole
- **Example:** New app created with Permissive default, exposed to attacks
- **Mitigation:** Default to server-level setting, require explicit override

#### 3. **More Complex Auditing**
- **Problem:** Auditor must check N app configs instead of 1 server config
- **Impact:** More time-consuming compliance verification
- **Example:** "Show me your filename sanitization policy" - 20 different answers
- **Mitigation:** Generate audit report showing all app settings

#### 4. **Performance Overhead (Minor)**
- **Problem:** Must load app config on every file upload
- **Impact:** Extra lookup per upload request
- **Technical:** AppConfig already loaded for request, negligible impact
- **Mitigation:** Cache app config in memory

#### 5. **Potential for Privilege Escalation**
- **Problem:** If app admins can change security level, they might weaken security
- **Impact:** Insider threat - malicious admin reduces security
- **Example:** Compromised admin account sets app to Permissive, uploads malicious files
- **Mitigation:** Require server admin approval for security level changes

#### 6. **Config File Bloat**
- **Problem:** Every app config includes sanitization setting
- **Impact:** Larger config.json file
- **Example:** 100 apps = 100 redundant settings if all use default
- **Mitigation:** Use nullable type, only store overrides

---

## Strategy 3: Site Properties (Alternative #2)

### Architecture

```
ServerConfig (config.json)
  ├─ filenameSanitizationLevel: Moderate (server default)
  │
  ├─ App 1 (appConfig)
  │   ├─ filenameSanitizationLevel: Strict (app default)
  │   ├─ Domain A → ccSetup table
  │   │   └─ filenameSanitizationLevel: Permissive (site override)
  │   └─ Domain B → ccSetup table
  │       └─ filenameSanitizationLevel: null (uses app default: Strict)
  │
  └─ App 2 (appConfig)
      ├─ filenameSanitizationLevel: null (uses server default: Moderate)
      ├─ Domain C → ccSetup table
      │   └─ filenameSanitizationLevel: Strict (site override)
      └─ Domain D → ccSetup table
          └─ filenameSanitizationLevel: null (uses app default → server default)
```

### How It Would Work

```csharp
// At file upload time
string levelStr = cp.Site.GetText("filenameSanitizationLevel", "");
FilenameSanitizationLevelEnum level;

if (Enum.TryParse(levelStr, out level)) {
    // Use site-specific setting
} else if (appConfig.filenameSanitizationLevel.HasValue) {
    // Use app-specific setting
    level = appConfig.filenameSanitizationLevel.Value;
} else {
    // Use server default
    level = serverConfig.filenameSanitizationLevel;
}

string sanitized = FileController.sanitizeFilename(filename, level);
```

### Advantages ✅

#### 1. **Maximum Granularity**
- **Benefit:** Per-site/per-domain customization
- **Use Case:** Multi-domain app with different audiences
- **Example:**
  - example.com (public, international): Moderate
  - admin.example.com (internal, strict compliance): Strict
  - api.example.com (trusted partners): Permissive

#### 2. **Runtime Configuration Changes**
- **Benefit:** Change security level without restarting server or editing config files
- **Use Case:** Quick response to security incidents
- **Example:** Security team detects malicious uploads, instantly switches to Strict mode
- **Implementation:** Update via admin UI, takes effect immediately

#### 3. **Domain-Specific Requirements**
- **Benefit:** Different domains within same app can have different policies
- **Use Case:** Geolocation-based compliance
- **Example:**
  - example.eu (GDPR): Strict
  - example.us (less restrictive): Moderate
  - example.cn (Unicode required): Moderate with special handling

#### 4. **User-Editable via Admin UI**
- **Benefit:** Site admins can adjust without IT support
- **Use Case:** Customer self-service portal
- **Example:** Admin UI → Settings → File Security → Sanitization Level [dropdown]
- **Implementation:** Standard site property editor in admin site

#### 5. **Matches UI/UX Settings**
- **Benefit:** Aligns with other site-specific configuration
- **Examples:**
  - `htmlPlatformVersion` - Bootstrap 4 vs 5 (per site)
  - `password min length` - Password policy (per site)
  - `blockNonProductionEmail` - Email behavior (per site)

#### 6. **A/B Testing Capability**
- **Benefit:** Test different security levels on different sites
- **Use Case:** Measuring impact of security on user experience
- **Example:**
  - Site A: Moderate (baseline)
  - Site B: Strict (test impact on international users)

#### 7. **Complete Override Chain**
- **Benefit:** Site → App → Server fallback hierarchy
- **Use Case:** Flexible inheritance with specific overrides
- **Example:** Most sites use server default, except specific high-security subdomain

### Disadvantages ❌

#### 1. **Maximum Configuration Complexity**
- **Problem:** Three levels of config to check (site → app → server)
- **Impact:** Debugging "which setting is active?" becomes difficult
- **Example:** Issue in production, must check ccSetup table + appConfig + serverConfig
- **Mitigation:** Provide diagnostic UI showing active security level

#### 2. **Database Dependency**
- **Problem:** Requires database query on every file upload
- **Impact:** Performance overhead, database load
- **Technical:**
  - ServerConfig: Loaded once at startup
  - AppConfig: Loaded once per request (already cached)
  - SiteProperty: Database query per request (unless cached)
- **Mitigation:** Cache site properties in memory with invalidation

#### 3. **Difficult to Audit**
- **Problem:** Must check database for every site's configuration
- **Impact:** Compliance audits become complex
- **Example:** "What's your filename security policy?" - must query database for all sites
- **Mitigation:** Generate audit report from ccSetup table

#### 4. **Risk of Inconsistent Configuration**
- **Problem:** Every site might have different setting
- **Impact:** Security policy fragmentation
- **Example:** 100 sites = potentially 100 different security levels
- **Mitigation:** Dashboard showing all site configurations, alerts for Permissive mode

#### 5. **No Type Safety**
- **Problem:** Site properties are stored as strings in database
- **Impact:** Typos or invalid values possible
- **Example:** Admin types "strick" instead of "Strict", setting ignored silently
- **Mitigation:** Validation on write, dropdown UI (not free text)

#### 6. **Migration Complexity**
- **Problem:** Existing sites need database update
- **Impact:** Requires migration script to populate ccSetup table
- **Example:** 1000 existing sites need default value inserted
- **Mitigation:** Migration script with fallback logic

#### 7. **Inappropriate Scope for Infrastructure Security**
- **Problem:** Filename sanitization is infrastructure-level security, not site preference
- **Impact:** Conceptual mismatch - this isn't a "site personality" setting
- **Philosophical:** Site properties are for UX/branding, not core security
- **Comparison:**
  - ✅ Appropriate: `htmlPlatformVersion` (site UX preference)
  - ❌ Inappropriate: `isLocalFileSystem` (infrastructure decision)
  - ⚠️ Borderline: `filenameSanitizationLevel` (security policy)

#### 8. **Privilege Management**
- **Problem:** Who can change site-level security?
- **Impact:** Must implement permission checks
- **Example:** Prevent non-admin users from lowering security
- **Mitigation:** Require "Developer" or "Admin" role for changes

---

## Comparison Matrix

| Criteria | ServerConfig ✅ | AppConfig | SiteProperties |
|----------|----------------|-----------|----------------|
| **Configuration Complexity** | ⭐⭐⭐⭐⭐ Simple | ⭐⭐⭐ Moderate | ⭐ Complex |
| **Per-App Flexibility** | ❌ None | ⭐⭐⭐⭐⭐ Full | ⭐⭐⭐⭐ Via app |
| **Per-Site Flexibility** | ❌ None | ❌ None | ⭐⭐⭐⭐⭐ Full |
| **Security Consistency** | ⭐⭐⭐⭐⭐ Excellent | ⭐⭐⭐ Good | ⭐⭐ Fair |
| **Audit Simplicity** | ⭐⭐⭐⭐⭐ Single setting | ⭐⭐⭐ N settings | ⭐ N × M settings |
| **Performance** | ⭐⭐⭐⭐⭐ Fastest | ⭐⭐⭐⭐ Fast | ⭐⭐⭐ Slower (DB) |
| **Runtime Changes** | ⭐⭐ Requires restart | ⭐⭐ Requires restart | ⭐⭐⭐⭐⭐ Immediate |
| **Multi-Tenant Suitable** | ⭐⭐ Limited | ⭐⭐⭐⭐⭐ Excellent | ⭐⭐⭐⭐ Good |
| **Privilege Escalation Risk** | ⭐⭐⭐⭐⭐ Minimal | ⭐⭐⭐ Moderate | ⭐⭐ Higher |
| **Migration Difficulty** | ⭐⭐⭐⭐⭐ Easy | ⭐⭐⭐ Moderate | ⭐ Difficult |
| **Type Safety** | ⭐⭐⭐⭐⭐ Full | ⭐⭐⭐⭐⭐ Full | ⭐⭐ String-based |
| **Matches Existing Patterns** | ⭐⭐⭐⭐⭐ Yes | ⭐⭐⭐⭐ Yes | ⭐⭐⭐ Partial |

---

## Recommendations by Use Case

### Recommendation 1: Keep ServerConfig (Most Organizations)

**Best For:**
- Single organization running multiple apps
- Hosting providers enforcing uniform security
- Small-to-medium deployments (1-10 apps)
- Organizations with consistent security requirements
- Teams prioritizing simplicity and consistency

**Why:**
- Simple to manage and audit
- Consistent security across all apps
- Matches existing infrastructure patterns
- No privilege escalation risk
- Performance optimized

**Example Organizations:**
- Corporate intranet hosting
- Government agencies
- Small SaaS providers
- Educational institutions

---

### Recommendation 2: Upgrade to AppConfig (Large Multi-Tenant)

**Best For:**
- Large multi-tenant hosting platforms
- Organizations with diverse app security requirements
- SaaS providers offering security level as a feature
- Environments with mixed compliance needs
- Teams managing 20+ apps with different requirements

**Migration Path:**
```csharp
// 1. Add property to AppConfigBaseModel
public abstract FilenameSanitizationLevelEnum? filenameSanitizationLevel { get; set; }

// 2. Update file upload logic
var level = appConfig.filenameSanitizationLevel ?? serverConfig.filenameSanitizationLevel;

// 3. Provide UI for app admins to configure
```

**Example Organizations:**
- Enterprise hosting platforms (e.g., GoDaddy, Bluehost)
- Large corporations with regulatory diversity
- International organizations

---

### Recommendation 3: Avoid SiteProperties (Inappropriate Scope)

**Not Recommended Because:**
- Filename sanitization is infrastructure/security, not site preference
- Database overhead on every upload
- Audit complexity
- Type safety issues
- Over-engineering for most use cases

**Only Consider If:**
- You absolutely need per-domain security levels
- You have a strong requirement for runtime configuration changes
- You have a custom multi-domain architecture requiring this granularity

**Better Alternatives:**
- If you need per-domain flexibility, consider AppConfig with multiple apps
- If you need runtime changes, consider admin UI for ServerConfig with restart

---

## Implementation Recommendation

### Current State Assessment

The current **ServerConfig** implementation is **correct for 90% of Contensive deployments**.

### Recommended Path Forward

#### Phase 1: Keep ServerConfig (Now)
- ✅ Already implemented
- ✅ Serves most use cases well
- ✅ Simple and maintainable

#### Phase 2: Add AppConfig Support (Future Enhancement)
If user demand materializes:

1. Add `filenameSanitizationLevel` to `AppConfigBaseModel` (nullable)
2. Update file upload logic to check app-level override
3. Provide admin UI for configuration
4. Document migration path

**Code Example:**
```csharp
// In AppConfigBaseModel.cs
public abstract FilenameSanitizationLevelEnum? filenameSanitizationLevel { get; set; }

// In ConfigurationClass (both WebApi and IIS)
var appConfig = /* load app config */;
var level = appConfig.filenameSanitizationLevel ?? ServerConfigBaseModel.FilenameSanitizationLevelEnum.Moderate;
string sanitized = FileController.sanitizeFilename(filename, level);
```

**Benefits:**
- Backward compatible (null = use server default)
- Simple migration path
- Addresses multi-tenant needs if they arise

#### Phase 3: Monitor Usage
- Collect feedback from users
- Track how many deployments use app-level overrides
- Decide if site-level is ever needed (likely no)

---

## Conclusion

The **ServerConfig** implementation is the right choice for the initial release:

✅ **Simple** - One setting to manage
✅ **Secure** - Consistent policy enforcement
✅ **Fast** - No runtime overhead
✅ **Auditable** - Single source of truth
✅ **Matches Patterns** - Aligns with infrastructure settings

For organizations needing per-app customization, **AppConfig** can be added in a future release without breaking changes.

**SiteProperties** should be avoided as it's inappropriate scope for infrastructure-level security and adds unnecessary complexity.

---

*Last Updated: 2026-08-25*
*Document Version: 1.0*
*Related: sanitization-future.md*
