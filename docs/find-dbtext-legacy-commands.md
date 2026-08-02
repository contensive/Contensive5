# Plan: Find Legacy Content Commands in dbText

## Goal

Create a CLI addon that scans all `dbText` records and reports which ones contain embedded content commands in any of the three formats. This is a read-only diagnostic tool — it makes no changes to the database.

## The Three Command Formats

### Format 1: AC Tags
```html
<AC type="AGGREGATEFUNCTION" name="Personalization-FirstName" ACInstanceID="{7D5A0080-2BAA-4C01-B1AB-3B9FD5FC31BC}" querystring="As Ajax=&css Container id=&css Container class=" guid="{41772430-FB1A-49F7-BD17-38B7EF280915}">
```

### Format 2: IMG Tags with AC-encoded IDs
```html
<img id="AC,AGGREGATEFUNCTION,0,My Addon,color=blue,{guid}" alt="Add-on" src="/path/to/icon.png" ACInstanceID="instance-guid">
```

### Format 3: `{% %}` JSON Tags
```
{%{"addon":{"addon":"My Addon","color":"blue"}}%}
{% addon "My Addon" %}
```

---

## Implementation

### Step 1: Create the CLI Addon — `Find Legacy Content Commands`

A single addon class that runs from the command line:

```
cc -a appname -r "Find Legacy Content Commands"
```

```csharp
public class FindLegacyContentCommandsAddon : AddonBaseClass {
    public override object Execute(CPBaseClass cp) {
        // 1. Query all dbText records
        // 2. For each record, check the text field for any of the three formats
        // 3. Build a report of matching records
        // 4. Return the report
    }
}
```

### Step 2: Detection Logic

For each `dbText` record, read the `text` field and check for:

1. **AC tags** — search for `<AC ` (case-insensitive). Matches `<AC type="AGGREGATEFUNCTION"...>`, `<AC type="ADDON"...>`, etc.
2. **IMG tags with AC-encoded IDs** — search for `<img ` tags where the `id` attribute value starts with `"AC,"` (case-insensitive on the tag, case-sensitive on the `AC,` prefix in the id value)
3. **`{% %}` commands** — search for the `{%` delimiter

For each match found, extract:
- The command format type (AC tag, IMG tag, or `{% %}`)
- The addon name (from the `name` attribute, the comma-delimited id, or the JSON `addon` key)
- The full matched tag/command string (for reference)

### Step 3: Output Format

Return a text report listing each matching dbText record:

```
Find Legacy Content Commands Report
====================================

dbText id: 142, name: "Homepage Text", ccguid: {A1B2C3D4-...}
  [AC tag] addon: "Personalization-FirstName"
  [AC tag] addon: "Contact Form"

dbText id: 287, name: "About Page Text", ccguid: {E5F6A7B8-...}
  [IMG tag] addon: "Image Gallery"

dbText id: 445, name: "Footer Text", ccguid: {C9D0E1F2-...}
  [{% %}] addon: "Social Links"
  [{% %}] addon: "Newsletter Signup"

====================================
Summary: 3 dbText records found with embedded commands (5 total commands)
  AC tags: 2
  IMG tags: 1
  {% %} commands: 2
```

### Step 4: Unit Tests

- dbText with a single AC tag is detected and reported
- dbText with a single IMG tag (id starts with "AC,") is detected and reported
- dbText with a single `{% %}` command is detected and reported
- dbText with multiple commands of mixed formats reports all of them
- dbText with no commands is not included in the report
- dbText with `<AC type="CONTENT"...>` or `<AC type="TEXT"...>` is still reported (these are AC tags even though they are template placeholders)
- dbText with a normal `<img>` tag (no AC-encoded id) is not reported
- Addon name is correctly extracted from each format
- Summary counts are accurate
