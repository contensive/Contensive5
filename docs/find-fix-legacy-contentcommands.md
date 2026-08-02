# Plan: Upgrade Content Commands to Embedded Addons with `{% %}` JSON Tags

## Background

There are two legacy formats for embedding addon execution in content:

### Legacy Format 1: AC Tags
```html
<AC type="AGGREGATEFUNCTION" name="Personalization-FirstName" ACInstanceID="{7D5A0080-2BAA-4C01-B1AB-3B9FD5FC31BC}" querystring="As Ajax=&css Container id=&css Container class=" guid="{41772430-FB1A-49F7-BD17-38B7EF280915}">
```
AC tag types found in the codebase:
- **AGGREGATEFUNCTION** — Executes an addon by name/guid with optional querystring arguments
- **ADDON** — Same as AGGREGATEFUNCTION (alias)
- **CONTENT** — Template content placeholder (inserts page content into template)
- **TEXT** — Template text placeholder

### Legacy Format 2: IMG Tags (Editor Representation)
```html
<img border="0" id="AC,AGGREGATEFUNCTION,0,My Addon,color=blue,{guid}" alt="Add-on" title="Rendered as the Add-on [My Addon]" src="/path/to/icon.png" ACInstanceID="instance-guid">
```
The IMG `id` attribute encodes the command as: `AC,{ACType},{NotUsedId},{AddonName},{OptionsList},{ACGuid}`

These IMG tags are the WYSIWYG editor representation of AC tags. The editor converts AC→IMG for display, and IMG→AC on save. Stale IMG tags may exist in stored content if the editor didn't properly convert them back.

### Target Format: `{% %}` JSON Tags
```
{%{"addon":{"addon":"Personalization-FirstName"}}%}
{%{"addon":{"addon":"My Addon","color":"blue"}}%}
```

### Where These Commands Appear
- Page content — `ccPageContent` table, content stored in files via `copyFilename`
- Email content — `ccEmail` table, body stored in files via `copyFilename`
- Copy content text blocks — `ccCopyContent` table, `copy` field (used by design blocks)
- Design block text blocks — `dbText` table, used in TextBlocks within design blocks
- Page templates — `ccTemplates` table, HTML fields

---

## Upgrade Strategy

The upgrade is a **CLI addon** — a standalone addon executed from the command line that scans content in the database, converts legacy AC/IMG tags to the new `{% %}` format, and creates per-record recovery entries so each change can be individually reverted.

### Key Design Decisions

1. **CLI addon, not automatic upgrade** — The tool is run manually by an admin via the Contensive CLI (`cc -a appname -r "Upgrade Content Commands"`), giving full control over timing and environment
2. **Per-record recovery** — Before modifying any record, the original content is saved to a recovery table (`dbUpgradeContentCommandLog`). Each record can be individually restored via a companion CLI addon
3. **Conversion is content-level, not runtime** — We rewrite stored content in the database rather than adding another runtime translation layer
4. **ACInstanceID is preserved** — The instanceGuid is critical: at runtime, `AddonController.execute()` sets `core.docProperties["instanceId"]` to this value (line 330). Addons read it via `cp.Doc.GetText("instanceId")` and use it as the `ccguid` to look up a per-instance settings record (e.g., `AddonListItemModel` line 115 queries `ccguid={instanceGuid}` against the addon's `instanceSettingPrimaryContentId` table). The `{% %}` JSON format must include the instanceGuid. Since `ContentCmdController` currently does NOT pass instanceGuid, the conversion must embed it as a JSON argument (e.g., `"instanceId":"{GUID}"`) so it flows into `argumentKeyValuePairs` and then into `docProperties` — matching the same runtime path
5. **CONTENT and TEXT AC types are NOT converted** — These are template-level placeholders with a different rendering path; they should remain as-is or be handled separately
6. **IMG tags with AC-encoded IDs are converted** — These are stale editor artifacts that were never properly converted back to AC tags on save
7. **Addon identification uses name** — The `{% %}` addon command identifies addons by name. The GUID is not included in the output since the runtime executor looks up by name

---

## Steps

### Step 1: Create the Recovery Table (`dbUpgradeContentCommandLog`)

Add a CDef to the addon collection XML for a new table that stores the original content before conversion. This enables per-record rollback.

**Table: `dbUpgradeContentCommandLog`**

| Field | Type | Purpose |
|---|---|---|
| `sourceTable` | Text | Table name the record came from (e.g., `ccPageContent`) |
| `sourceField` | Text | Field name that was modified (e.g., `copy`, `copyFilename`) |
| `sourceRecordId` | Integer | Record ID in the source table |
| `originalContent` | LongText | The full original content before conversion |
| `convertedContent` | LongText | The new content after conversion |
| `restored` | Boolean | True if this record has been restored to its original |

### Step 2: Create the CLI Addon — `Upgrade Content Commands`

Create a new addon class (e.g., in the Processor or a standalone addon project) that runs as a CLI addon:

```
cc -a appname -r "Upgrade Content Commands"
```

```csharp
public class UpgradeContentCommandsAddon : AddonBaseClass {
    public override object Execute(CPBaseClass cp) {
        // 1. Scan all content locations
        // 2. For each record with legacy tags:
        //    a. Save original to dbUpgradeContentCommandLog
        //    b. Convert content
        //    c. Write converted content back
        //    d. Log summary to console output
        // 3. Return summary report
    }
}
```

### Step 3: Create the CLI Addon — `Restore Content Command Upgrade`

A companion addon that restores individual records from the recovery log:

```
cc -a appname -r "Restore Content Command Upgrade"
```

Options:
- No arguments → lists all conversion log entries with their IDs, source table, record ID, and status
- `id={logRecordId}` → restores a single record from the log by writing `originalContent` back to the source table/field and marking the log entry `restored=true`
- `all=true` → restores all non-restored records

```csharp
public class RestoreContentCommandUpgradeAddon : AddonBaseClass {
    public override object Execute(CPBaseClass cp) {
        string logId = cp.Doc.GetText("id");
        bool restoreAll = cp.Doc.GetBoolean("all");
        if (!string.IsNullOrEmpty(logId)) {
            // Restore single record
        } else if (restoreAll) {
            // Restore all non-restored records
        } else {
            // List all log entries
        }
    }
}
```

### Step 4: Implement Argument Encoding/Decoding

AC tags and IMG tags store arguments using **NVA encoding** — a Contensive-specific encoding where special characters are replaced with `#XXXX#` sequences (HTML character codes):

| Character | NVA Encoded |
|---|---|
| `&` | `#0038#` |
| `=` | `#0061#` |
| `,` | `#0044#` |
| `"` | `#0034#` |
| `'` | `#0039#` |
| `\|` | `#0124#` |
| `[` | `#0091#` |
| `]` | `#0093#` |
| `:` | `#0058#` |
| newline | `#0013#` |

The `{% %}` JSON format uses **plain JSON** with no NVA encoding. The conversion must:
1. HTML-decode the querystring attribute value
2. Split on `&` to get key=value pairs
3. Split each pair on `=` to get key and value
4. **NVA-decode both key and value** (using `GenericController.decodeNvaArgument()`)
5. Build the JSON object with decoded keys and values

The existing `GenericController.convertQSNVAArgumentstoDocPropertiesList()` handles steps 1-4 and returns a `Dictionary<string, string>`. The conversion logic should use this or replicate its behavior.

### Step 5: Implement AC Tag Conversion Logic

Parse each `<AC>` tag using `HtmlParserController` (same parser already used in `ContentRenderController`) and convert based on type:

**AGGREGATEFUNCTION / ADDON with querystring and ACInstanceID:**
```
Input:  <AC type="AGGREGATEFUNCTION" name="My Addon" ACInstanceID="{INSTANCE}" querystring="color=blue&size=large" guid="{GUID}">
Output: {%{"addon":{"addon":"My Addon","instanceId":"{INSTANCE}","color":"blue","size":"large"}}%}
```

**AGGREGATEFUNCTION / ADDON with NVA-encoded querystring:**
```
Input:  <AC type="AGGREGATEFUNCTION" name="My Addon" ACInstanceID="{INSTANCE}" querystring="json={#0034#key#0034##0058##0034#value#0034#}" guid="{GUID}">
        (NVA decodes to: json={"key":"value"})
Output: {%{"addon":{"addon":"My Addon","instanceId":"{INSTANCE}","json":"{\"key\":\"value\"}"}}%}
```

**AGGREGATEFUNCTION / ADDON without querystring:**
```
Input:  <AC type="AGGREGATEFUNCTION" name="My Addon" ACInstanceID="{INSTANCE}" guid="{GUID}">
Output: {%{"addon":{"addon":"My Addon","instanceId":"{INSTANCE}"}}%}
```

**AGGREGATEFUNCTION / ADDON without querystring or ACInstanceID:**
```
Input:  <AC type="AGGREGATEFUNCTION" name="My Addon" guid="{GUID}">
Output: {% addon "My Addon" %}
```

**Note:** The `instanceId` key is included in the JSON arguments whenever `ACInstanceID` is present in the source tag. At runtime, `argumentKeyValuePairs` are added to `docProperties` before addon execution (AddonController.cs line 360+), so `instanceId` will be available via `cp.Doc.GetText("instanceId")` — the same path used when `executeContext.instanceGuid` is set directly.

**CONTENT / TEXT — Skip these** (template placeholders, not addon commands).

Querystring parsing (via NVA decode):
- HTML-decode the querystring attribute
- Split on `&` to get key=value pairs
- Split each pair on first `=` to get key and value
- NVA-decode both key and value (`#XXXX#` → original characters)
- Skip empty keys
- Build JSON object with addon name + all decoded parameters

### Step 6: Implement IMG Tag Conversion Logic

Detect IMG tags where the `id` attribute starts with `"AC,"`. Parse the id by splitting on commas, using the same logic from `processWysiwygResponseForSave()`:

- `[0]` = "AC" (identifier — skip if not "AC")
- `[1]` = ACType (only convert AGGREGATEFUNCTION/ADDON)
- `[2]` = NotUsedId (ignored)
- `[3]` = Addon name (NVA-encoded — decode with `decodeNvaArgument()`)
- `[4]` = Options string (NVA-encoded, parse as `&`-delimited name=value pairs, NVA-decode each key and value)
- `[5+]` = GUID (if wrapped in `{}` — used for identification but not included in output)

**Important**: When splitting the `id` attribute on commas, the GUID portion (which contains no commas) must be detected by checking for `{}` wrapping. Any commas that appear before the GUID but after position [4] are part of the options string (since NVA encodes commas as `#0044#`, stray commas indicate the options were not fully NVA-encoded — append them back to element [4]).

```
Input:  <img id="AC,AGGREGATEFUNCTION,0,My Addon,color=blue,{guid}" ACInstanceID="{INSTANCE}" ...other attributes...>
Output: {%{"addon":{"addon":"My Addon","instanceId":"{INSTANCE}","color":"blue"}}%}
```

The `ACInstanceID` attribute on the IMG tag is a separate HTML attribute (not part of the comma-delimited `id`). Extract it from the IMG tag's attributes.

### Step 7: `convertContent()` — Full Content Processing

This method processes an entire HTML string:

1. Find and replace all `<AC ...>` tags (regex or HTML parser)
   - For each match, call `convertACTag()`
   - Replace the match with the result
2. Find and replace all `<img ...>` tags where `id` starts with `"AC,"`
   - For each match, call `convertACImgTag()`
   - Replace the match with the result
3. Return the modified string (or original if no changes)

### Step 8: Database Content Scanning (in the Upgrade addon)

The upgrade addon scans these database locations:

| Table | Field | Notes |
|---|---|---|
| `ccPageContent` | content via `copyFilename` | Page body content stored in files |
| `ccEmail` | content via `copyFilename` | Email body content stored in files |
| `ccCopyContent` | `copy` | Copy content text blocks |
| `dbText` | text content field | Design block TextBlock content |
| `ccTemplates` | template HTML fields | Page template markup |

For each record:
1. Read the content (from file or field)
2. Call `convertContent()`
3. If changed:
   a. Save a recovery record to `dbUpgradeContentCommandLog` with the original content, converted content, source table, source field, and source record ID
   b. Write the converted content back to the source record
4. Output a summary line to the CLI (table, record id, addon names converted)

### Step 9: Unit Tests

Create tests in `source/ProcessorTests/` that verify:

- AC tag with querystring and ACInstanceID → `{%{"addon":{...,"instanceId":"{...}",...}}%}` JSON format
- AC tag with ACInstanceID but no querystring → `{%{"addon":{"addon":"name","instanceId":"{...}"}}%}`
- AC tag without querystring or ACInstanceID → `{% addon "name" %}` simple format
- AC tag type ADDON (alias) converts same as AGGREGATEFUNCTION
- IMG tag with AC-encoded id → correct `{% %}` output
- CONTENT/TEXT AC tags are left unchanged
- Mixed content with multiple AC tags and IMG tags converts all correctly
- Content with no legacy tags passes through unchanged
- HTML-encoded characters in addon names and options are properly decoded
- NVA-encoded characters (`#0038#`, `#0034#`, `#0061#`, etc.) are decoded to original characters in JSON output
- Querystring values containing NVA-encoded JSON strings produce properly escaped JSON in output
- Querystring with empty values handled correctly
- Malformed AC/IMG tags don't crash (graceful skip)
- The `&` separator in querystrings is parsed correctly
- IMG tag id with extra commas before the GUID correctly reconstructs the options string
- IMG tag with ACInstanceID attribute includes instanceId in JSON output
- AC tag with ACInstanceID but no querystring still produces JSON format (not simple format) to carry the instanceId
- Converted instanceId is accessible at runtime via `cp.Doc.GetText("instanceId")` (same as the legacy path)

**Phase 2 tests:**
- dbText with one embedded addon splits into 3 addonList entries (text, addon, text)
- dbText with two embedded addons splits into 5 entries (text, addon, text, addon, text)
- dbText with addon at the start produces no empty leading text block
- dbText with addon at the end produces no empty trailing text block
- Adjacent addons with no text between them produce no empty text block between them
- dbText in a nested column addonList is split at the correct nesting level
- Addon name that can't be resolved to a ccguid logs a warning and remains inline
- New dbText records are created with correct ccguid and text content
- ccPageContent addonList JSON is correctly updated with expanded entries
- Recovery restores original addonList and deletes newly created dbText records
- dbText not referenced by any ccPageContent addonList is skipped in Phase 2
- Split inside nested HTML (e.g., `<div><p>text {%...%} text</p></div>`) produces valid HTML in both segments with correct closing/opening tags
- Split inside table structure (`<table><tr><td>`) repairs both segments correctly
- Split at a point with no open HTML elements produces segments with no extra tags added

---

## Conversion Reference Table

| Legacy Format | Converted Format |
|---|---|
| `<AC ... name="Foo" guid="{G}">` (no instanceID, no qs) | `{% addon "Foo" %}` |
| `<AC ... name="Foo" ACInstanceID="{I}" guid="{G}">` (no qs) | `{%{"addon":{"addon":"Foo","instanceId":"{I}"}}%}` |
| `<AC ... name="Foo" ACInstanceID="{I}" querystring="a=1&b=2" guid="{G}">` | `{%{"addon":{"addon":"Foo","instanceId":"{I}","a":"1","b":"2"}}%}` |
| `<AC ... name="Foo" querystring="x=#0034#hi#0034#" guid="{G}">` | `{%{"addon":{"addon":"Foo","x":"\"hi\""}}%}` *(NVA-decoded)* |
| `<AC type="ADDON" name="Foo" guid="{G}">` | `{% addon "Foo" %}` |
| `<img id="AC,AGGREGATEFUNCTION,0,Foo,,{G}" ...>` (no instanceID) | `{% addon "Foo" %}` |
| `<img id="AC,AGGREGATEFUNCTION,0,Foo,a=1&b=2,{G}" ACInstanceID="{I}" ...>` | `{%{"addon":{"addon":"Foo","instanceId":"{I}","a":"1","b":"2"}}%}` |
| `<AC type="CONTENT" name="...">` | *(no change)* |
| `<AC type="TEXT" name="...">` | *(no change)* |

---

## Phase 2: Split dbText Embedded Addons into AddonList Entries

After Phase 1 converts all AC/IMG tags to `{% %}` format, some `dbText` records will now contain embedded `{% %}` addon commands within their text content. These embedded addons should not remain inline in the text — they need to be promoted to first-class entries in the page's `addonList`.

### Background: AddonList Structure

Each page (`ccPageContent`) has an `addonList` field — a JSON array of `AddonListItemModel` entries. Each entry represents a design block on the page:

```json
[
  {
    "designBlockTypeGuid": "{4F7FADCB-7B0B-4E4B-BBE4-CFAF4E49D548}",
    "designBlockTypeName": "Text Block",
    "instanceGuid": "{some-guid}",
    "columns": null
  }
]
```

A TextBlock entry's `instanceGuid` matches the `ccguid` of a `dbText` record. The TextBlock addon reads `cp.Doc.GetText("instanceId")` and queries `dbText WHERE ccguid = instanceId` to get its content from the `text` field.

### The Problem

After Phase 1, a `dbText.text` field might contain:

```html
<p>Welcome to our site.</p>
{%{"addon":{"addon":"Contact Form","instanceId":"{FORM-GUID}"}}%}
<p>Thanks for visiting.</p>
```

This embedded addon should instead be represented as three separate entries in the page's addonList: a text block before it, the addon itself, and a text block after it.

### Step 10: Phase 2 — Scan dbText for Embedded Addons

For every `dbText` record that was modified in Phase 1 (i.e., has a recovery log entry) and now contains `{% %}` addon commands:

1. **Find the parent addonList** — Query all `ccPageContent` records, deserialize each `addonList` JSON, and search for an entry where `instanceGuid` matches the `dbText.ccguid`. Also search recursively through `columns[].addonList` for nested entries.

2. **If found**, proceed to split. If not found (the dbText is orphaned or used in email/copy content only), skip Phase 2 for this record.

### Step 11: Phase 2 — Split the AddonList Entry

Given a TextBlock addonList entry whose `dbText.text` contains one or more `{% %}` addon commands, split it into multiple entries:

**Before (single addonList entry):**
```json
[
  { "designBlockTypeGuid": "{4F7FADCB-...}", "designBlockTypeName": "Text Block", "instanceGuid": "{ORIGINAL-GUID}", "columns": null }
]
```

**dbText record `{ORIGINAL-GUID}` contains:**
```html
<p>Welcome to our site.</p>
{%{"addon":{"addon":"Contact Form","instanceId":"{FORM-GUID}"}}%}
<p>Thanks for visiting.</p>
```

**After (three addonList entries replacing the one):**
```json
[
  { "designBlockTypeGuid": "{4F7FADCB-...}", "designBlockTypeName": "Text Block", "instanceGuid": "{NEW-GUID-1}", "columns": null },
  { "designBlockTypeGuid": "{CONTACT-FORM-ADDON-GUID}", "designBlockTypeName": "Contact Form", "instanceGuid": "{FORM-GUID}", "columns": null },
  { "designBlockTypeGuid": "{4F7FADCB-...}", "designBlockTypeName": "Text Block", "instanceGuid": "{NEW-GUID-2}", "columns": null }
]
```

**Database changes:**
- **Original dbText** `{ORIGINAL-GUID}` — update `text` to just `<p>Welcome to our site.</p>`, update `ccguid` to `{NEW-GUID-1}`
- **New dbText** `{NEW-GUID-2}` — create with `text` = `<p>Thanks for visiting.</p>`
- **ccPageContent** — update the `addonList` JSON field with the expanded array

### Step 12: Phase 2 — Detailed Split Logic

For each dbText record with embedded `{% %}` commands:

1. **Parse the text** — Find all `{%...%}` commands and their positions in the text
2. **Split into segments** — Alternate between text segments and addon commands:
   - Text before first command → text segment
   - First `{% %}` command → addon segment
   - Text between commands → text segment
   - Next `{% %}` command → addon segment
   - ... and so on
   - Text after last command → text segment
3. **Repair HTML at split boundaries** — When the `{% %}` command sits inside nested HTML elements, splitting the text produces invalid HTML (unclosed tags in the segment before the split, orphaned closing tags in the segment after). Each text segment must be repaired:
   - **Before-segment**: Parse the HTML and identify any elements that are open (not closed) at the split point. Append closing tags in reverse nesting order (e.g., if the command was inside `<div><p>`, append `</p></div>`)
   - **After-segment**: Identify any closing tags at the start of the segment that have no matching opener. Prepend the corresponding opening tags with their original attributes in the correct nesting order (e.g., if the segment starts with `</p></div>`, prepend `<div><p>`)
   - Use an HTML parser or tag-stack approach: walk the tags in each segment, tracking opens and closes. Any unclosed opens at the end need closing tags appended. Any unmatched closes at the start need opening tags prepended.
   - Self-closing tags (`<br>`, `<img>`, `<hr>`, etc.) don't affect the stack
   - Example:
     ```html
     <!-- Original dbText content -->
     <div class="wrapper"><p>Hello</p><p>See our form:
     {%{"addon":{"addon":"Contact Form","instanceId":"{FORM-GUID}"}}%}
     Thanks for submitting.</p></div>

     <!-- After split — before-segment (repaired) -->
     <div class="wrapper"><p>Hello</p><p>See our form:</p></div>

     <!-- After split — after-segment (repaired) -->
     <div class="wrapper"><p>Thanks for submitting.</p></div>
     ```
4. **Skip empty text segments** — If a text segment is empty or whitespace-only after repair, don't create a dbText/addonList entry for it
5. **For each text segment**:
   - Generate a new GUID
   - Create a new `dbText` record with `ccguid = {new-guid}`, `text = {repaired segment content}`
   - Create an addonList entry: `{ designBlockTypeGuid: "{TextBlock-GUID}", designBlockTypeName: "Text Block", instanceGuid: "{new-guid}" }`
6. **For each addon segment**:
   - Parse the `{% %}` command to extract the addon name and instanceId
   - Look up the addon's `ccguid` (designBlockTypeGuid) from the addons table by name
   - Create an addonList entry: `{ designBlockTypeGuid: "{addon-ccguid}", designBlockTypeName: "{addon-name}", instanceGuid: "{instanceId-from-command}" }`
7. **Replace the original single entry** in the addonList array with the new sequence of entries (maintaining position relative to other entries)
8. **Delete or update the original dbText record** — Either delete it (if no longer referenced) or update it to contain only the first text segment

### Step 13: Phase 2 — Recovery

Phase 2 recovery records must capture:

- **For the ccPageContent addonList change**: Save the original `addonList` JSON to `dbUpgradeContentCommandLog` (sourceTable=`ccPageContent`, sourceField=`addonList`, sourceRecordId=page record id)
- **For each new dbText record created**: Log the record so it can be deleted on restore (sourceTable=`dbText`, sourceField=`text`, sourceRecordId=new record id, originalContent=empty to indicate this was a new record)
- **For the original dbText record modified**: Already logged in Phase 1

On restore:
1. Restore the original `addonList` JSON on the ccPageContent record
2. Delete any dbText records that were created in Phase 2 (those with empty `originalContent` in the log)
3. The Phase 1 restore will handle restoring the original dbText text content

### Step 14: Phase 2 — Edge Cases

- **Multiple embedded addons** — A dbText may contain 2+ embedded addons, resulting in 2n+1 segments (n addons, n+1 text segments). Handle all of them.
- **Nested addonList (columns)** — The TextBlock entry may be inside a column's addonList, not the top-level array. The split must occur at the correct nesting level.
- **Adjacent addons** — Two `{% %}` commands back-to-back with no text between them should produce no empty text block entry between them.
- **Addon at start/end** — If the text starts or ends with an addon command, don't create an empty leading/trailing text block.
- **Addon not found** — If the addon name from the `{% %}` command can't be resolved to a `ccguid` in the addons table, log a warning and leave the command inline in the text (don't split).
- **HTML repair at split boundaries** — When the `{% %}` command is nested inside HTML elements (e.g., inside a `<div><p>`), the split produces unclosed tags in the before-segment and orphaned closing tags in the after-segment. Both segments must be repaired to produce valid HTML. Deeply nested splits or splits inside table structures (`<table><tr><td>`) must also be handled correctly.

### Phase 2 Example

**Before:**

`ccPageContent.addonList`:
```json
[
  { "designBlockTypeGuid": "{4F7FADCB-...}", "designBlockTypeName": "Text Block", "instanceGuid": "{AAA}", "columns": null },
  { "designBlockTypeGuid": "{OTHER-GUID}", "designBlockTypeName": "Image Slider", "instanceGuid": "{BBB}", "columns": null }
]
```

`dbText` where `ccguid = {AAA}`, `text`:
```html
<h2>Our Services</h2>
<p>We offer the following:</p>
{%{"addon":{"addon":"Service List","instanceId":"{SVC-GUID}"}}%}
<p>Contact us for more info.</p>
{%{"addon":{"addon":"Contact Form","instanceId":"{FORM-GUID}"}}%}
```

**After:**

`ccPageContent.addonList`:
```json
[
  { "designBlockTypeGuid": "{4F7FADCB-...}", "designBlockTypeName": "Text Block", "instanceGuid": "{NEW-1}", "columns": null },
  { "designBlockTypeGuid": "{SERVICE-LIST-CCGUID}", "designBlockTypeName": "Service List", "instanceGuid": "{SVC-GUID}", "columns": null },
  { "designBlockTypeGuid": "{4F7FADCB-...}", "designBlockTypeName": "Text Block", "instanceGuid": "{NEW-2}", "columns": null },
  { "designBlockTypeGuid": "{CONTACT-FORM-CCGUID}", "designBlockTypeName": "Contact Form", "instanceGuid": "{FORM-GUID}", "columns": null },
  { "designBlockTypeGuid": "{OTHER-GUID}", "designBlockTypeName": "Image Slider", "instanceGuid": "{BBB}", "columns": null }
]
```

New `dbText` records:
- `ccguid={NEW-1}`, `text`: `<h2>Our Services</h2>\n<p>We offer the following:</p>`
- `ccguid={NEW-2}`, `text`: `<p>Contact us for more info.</p>`

Original `dbText` `{AAA}` — deleted or deactivated (logged for recovery).

---

## Questions for Clarification

1. **Should CONTENT and TEXT AC types be converted to a `{% %}` equivalent?** Currently they are template placeholders rendered differently. If there's a `{% %}` equivalent planned (e.g., `{% content %}` or `{% text %}`), we should convert them too.

2. **Are there other database tables/fields that store content with AC/IMG tags** beyond ccPageContent, ccEmail, ccCopyContent, dbText, and ccTemplates?

3. **After conversion, should the AC tag runtime processing in `ContentRenderController.renderContent_ACTags_AnchorTags()` be deprecated/removed**, or kept for backward compatibility with any content that wasn't converted?

4. **Should the recovery log records be automatically purged after a certain period**, or kept indefinitely until manually cleared?
