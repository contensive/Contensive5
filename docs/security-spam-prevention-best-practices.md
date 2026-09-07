# Spam Prevention Best Practices

This document describes the spam prevention techniques implemented in the Contact Us design block. These patterns should be adopted across all Contensive form-handling addons.

## 1. Honeypot Field

### Overview

A honeypot is a hidden form field that is invisible to real users but gets auto-filled by spam bots. When the server detects a value in this field, it silently discards the submission without alerting the bot.

### Client-Side Implementation

Add a hidden input field to the form template, positioned off-screen so human users never see or interact with it:

```html
<div style="position:absolute;left:-9999px;" aria-hidden="true">
    <input type="text" name="website_url" tabindex="-1" autocomplete="off" value="">
</div>
```

Key attributes:
- `style="position:absolute;left:-9999px;"` - Positions the field off-screen (preferred over `display:none` or `type="hidden"` since some bots skip those)
- `aria-hidden="true"` - Hides the field from screen readers so it doesn't confuse assistive technology users
- `tabindex="-1"` - Prevents keyboard users from accidentally tabbing into the field
- `autocomplete="off"` - Prevents browsers from auto-filling the field with saved data
- Field name `website_url` - Uses a name that looks like a legitimate field to attract bot auto-fillers

### Server-Side Implementation

Check the honeypot field value early in the form processing pipeline. If it contains any value, log the attempt and return a fake success response:

```csharp
string honeypotValue = CP.Doc.GetText("website_url");
if (!string.IsNullOrEmpty(honeypotValue)) {
    CP.Utils.AppendLog("FormSubmit, honeypot triggered - rejecting as spam");
    var spamResponse = new SubmitResponseClass() {
        errorList = new List<string>(),
        redirectUrl = settings.redirectUrl,
        thankYouEmbedCode = CP.Utils.EncodeContentForWeb(settings.thankYouEmbedCode)
    };
    return CP.JSON.Serialize(spamResponse);
}
```

Important: Return a success-like response (empty error list, normal redirect/thank-you) so the bot believes the submission succeeded and doesn't retry with different strategies.

---

## 2. Rate Limiting by Session

### Overview

Rate limiting prevents rapid-fire form submissions from the same visitor. This blocks both automated spam and accidental double-submits. The implementation uses a 15-second cooldown window between submissions.

### How It Works

1. Before processing a form submission, check the visitor's session for a stored timestamp of their last successful submit
2. If the last submit was less than 15 seconds ago, reject with a user-friendly message
3. After a successful submission, store the current timestamp in the session

### Implementation

```csharp
// -- rate limiting: block resubmission within 15 seconds
string rateLimitKey = $"contactUsLastSubmit-{settings.id}";
string lastSubmitStr = CP.Visit.GetText(rateLimitKey, "");
if (!string.IsNullOrEmpty(lastSubmitStr) && DateTime.TryParse(lastSubmitStr, out DateTime lastSubmit)) {
    if ((DateTime.Now - lastSubmit).TotalSeconds < 15) {
        CP.Utils.AppendLog($"FormSubmit, rate limited - resubmit within 15s blocked");
        var rateLimitResponse = new SubmitResponseClass() {
            errorList = new List<string> { "Please wait a moment before submitting again." },
            redirectUrl = "",
            thankYouEmbedCode = ""
        };
        return CP.JSON.Serialize(rateLimitResponse);
    }
}
```

After successful processing:

```csharp
// -- record submission time for rate limiting
CP.Visit.SetProperty(rateLimitKey, DateTime.Now.ToString("o"));
```

### Design Decisions

- **Visit-level storage (`CP.Visit`)**: The rate limit is tied to the visitor's session, which correlates with their IP and browser. This avoids needing direct IP access or external storage.
- **Per-form keying**: The key includes `settings.id` so submitting one form instance doesn't block submissions to a different form on the same site.
- **ISO 8601 timestamp format (`"o"`)**: Ensures reliable round-trip parsing with `DateTime.TryParse`.
- **15-second window**: Long enough to prevent spam bursts and accidental double-clicks, short enough to not frustrate legitimate users who need to correct and resubmit.

---

## Processing Order

These checks should execute in this order within the form submission handler:

1. **Rate limiting** - Cheapest check, prevents processing load from rapid submissions
2. **Honeypot validation** - Simple string check, catches most basic bots
3. **reCAPTCHA validation** (if enabled) - More expensive, external API call
4. **Form processing** - Business logic, database writes, email notifications

---

## Adoption Checklist

When adding these protections to a new form widget:

- [ ] Add the honeypot `<div>` to the HTML template inside the form, before visible fields
- [ ] Add server-side honeypot check early in the `Execute` method
- [ ] Add rate limit check using `CP.Visit.GetText` / `CP.Visit.SetProperty`
- [ ] Record the submission timestamp after successful processing
- [ ] Log blocked attempts via `CP.Utils.AppendLog` for monitoring
- [ ] Return appropriate responses (fake success for honeypot, error message for rate limit)
