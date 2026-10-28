# Code-First Collection Metadata Plan

Replace the XML collection file with C# attributes on the classes that already own the information. The installer scans assemblies for these attributes and builds the same metadata it currently reads from XML.

---

## 1. CDef → `[ContentDefinition]` on Model Classes

**Current (XML):**
```xml
<CDef Name="Blog Posts" ContentTableName="myBlogPosts" NavTypeId="Content"
    AllowAdd="1" AllowDelete="1" DefaultSortMethod="By Date"
    Guid="{A1B2C3D4-...}">
    <Field Name="title" FieldType="Text" Caption="Title"
        EditSortPriority="1000" IndexColumn="0" IndexWidth="50" />
    <Field Name="authorId" FieldType="Lookup" Caption="Author"
        LookupContent="people" EditSortPriority="1010" IndexColumn="1" />
    ...
</CDef>
```

**Proposed (attributes):**
```csharp
[ContentDefinition(
    name: "Blog Posts",
    tableName: "myBlogPosts",
    guid: "{A1B2C3D4-...}",
    navType: NavType.Content,
    defaultSort: DefaultSort.ByDate
)]
public class BlogPostModel : DbBaseModel {
    public static DbBaseTableMetadataModel tableMetadata { get; }
        = new DbBaseTableMetadataModel("Blog Posts", "myBlogPosts");

    [ContentField(caption: "Title", required: true, indexColumn: 0, indexWidth: 50)]
    public string title { get; set; }

    [ContentField(caption: "Author", lookupContent: "people", indexColumn: 1)]
    public int authorId { get; set; }

    [ContentField(caption: "Published Date", indexColumn: 2)]
    public DateTime? publishedDate { get; set; }

    [ContentField(caption: "Body", fieldType: FieldType.Html)]
    public FieldTypeHTMLFile bodyFilename { get; set; }

    [ContentField(caption: "Featured")]
    public bool isFeatured { get; set; }

    // No attribute = in DB but hidden from the edit form (Authorable=false)
    public string internalNotes { get; set; }
}
```

**Key design decisions:**

- **Edit order = property declaration order** — no `EditSortPriority` numbers. Reflection preserves declaration order in practice; the installer uses the order returned by `Type.GetProperties()`. Add `[EditOrder(n)]` only when you need to override.
- **Field type is inferred from the C# type** — `string`→Text, `bool`→Boolean, `int`→Integer, `DateTime?`→Date, `double`→Float, `FieldTypeHTMLFile`→FileHTML, etc. Use `fieldType:` param only to override (e.g., `string` → LongText or Html).
- **Lookup fields** — any `int` property with `lookupContent:` set becomes a Lookup field automatically.
- **No `[ContentField]`** = field exists in DB (no change there) but is `Authorable=false` on the edit form.
- **`tableMetadata` stays** — it's used by CRUD methods at runtime. The installer reads `[ContentDefinition]` and verifies it matches, or derives `tableMetadata` from it in a future step.

**`ContentDefinitionAttribute` parameters:**

| Parameter | Default | Notes |
|-----------|---------|-------|
| `name` | required | CDef name |
| `guid` | required | unique identifier |
| `tableName` | derived from `name` | DB table name |
| `navType` | `NavType.Content` | admin nav section |
| `allowAdd` | `true` | |
| `allowDelete` | `true` | |
| `adminOnly` | `false` | |
| `developerOnly` | `false` | |
| `defaultSort` | `ByName` | |
| `dropDownFieldList` | `"name"` | |
| `parent` | `""` | parent CDef name for inheritance |

**`ContentFieldAttribute` parameters:**

| Parameter | Default | Notes |
|-----------|---------|-------|
| `caption` | derived from property name | |
| `guid` | required | stable identity |
| `fieldType` | inferred | override only when needed |
| `required` | `false` | |
| `readOnly` | `false` | |
| `notEditable` | `false` | write-once |
| `adminOnly` | `false` | |
| `developerOnly` | `false` | |
| `defaultValue` | `""` | |
| `tab` | `""` | edit tab name |
| `group` | `""` | right-side edit group |
| `lookupContent` | `""` | CDef name for FK lookup |
| `lookupList` | `""` | pipe-separated static list |
| `indexColumn` | `99` (hidden) | admin list column |
| `indexWidth` | `0` | |
| `helpText` | `""` | |
| `password` | `false` | |
| `uniqueName` | `false` | |
| `manyToManyContent` | `""` | |
| `manyToManyRuleContent` | `""` | |

---

## 2. Addon → `[Addon]` on Addon Classes

**Current (XML):**
```xml
<Addon Name="Housekeep" Guid="{...}" Type="Task">
    <DotNetClass>Contensive.Processor.Addons.Housekeeping.HousekeepTask</DotNetClass>
    <ProcessInterval>30</ProcessInterval>
    <Category>System</Category>
</Addon>
```

**Proposed:**
```csharp
[Addon(
    name: "Housekeep",
    guid: "{...}",
    type: AddonType.Task,
    category: "System",
    processInterval: 30
)]
public class HousekeepTask : AddonBaseClass {
    public override object Execute(CPBaseClass cp) { ... }
}
```

**Widget with dependencies:**
```csharp
[Addon(
    name: "Blog Widget",
    guid: "{C3D4E5F6-...}",
    type: AddonType.Widget,
    category: "Blog.Widgets",
    content: true,
    template: true,
    blockEditTools: true,
    argumentList: "maxItems=[5]"
)]
[IncludeAddon("{EF1FD66C-D62F-4BD2-BF07-38F47996EBB3}", name: "Bootstrap")]
[IncludeAddon("{F83EE7F9-79DA-4B3F-A1CD-45AEAD93D70F}", name: "Contensive Base Assets")]
[Navigator(nameSpace: "Content.Blog", type: NavType.Content)]
[ProcessTrigger(contentName: "Blog Posts")]
public class BlogWidgetClass : AddonBaseClass { ... }
```

`[IncludeAddon]`, `[Navigator]`, and `[ProcessTrigger]` are `AllowMultiple = true` so you can stack them.

**`AddonAttribute` parameters:**

| Parameter | Default | Notes |
|-----------|---------|-------|
| `name` | required | |
| `guid` | required | |
| `type` | `AddonType.Addon` | Addon, Widget, Tool, Task, Setting |
| `category` | `""` | dot-separated for nesting |
| `description` | `""` | |
| `content` | `false` | placeable on pages |
| `template` | `false` | |
| `email` | `false` | |
| `admin` | `false` | |
| `remoteMethod` | `false` | |
| `blockEditTools` | `false` | |
| `htmlDocument` | `false` | |
| `onBodyEnd` | `false` | |
| `diagnostic` | `false` | |
| `dashboardWidget` | `false` | |
| `isInline` | `false` | |
| `inFrame` | `false` | |
| `processInterval` | `0` | minutes (Task type) |
| `javascriptForceHead` | `false` | |
| `argumentList` | `""` | |
| `jsHeadScriptSrc` | `""` | |
| `stylesLinkHref` | `""` | |
| `instanceSettingPrimaryContent` | `""` | |

---

## 3. Collection-Level Metadata → Assembly Attribute

```csharp
// In AssemblyInfo.cs or any .cs file in the collection project
[assembly: CollectionInfo(
    name: "My Blog Collection",
    guid: "{B2C3D4E5-...}"
)]

// Optional: run an addon after install
[assembly: CollectionInfo(
    name: "My Blog Collection",
    guid: "{B2C3D4E5-...}",
    onInstallAddonGuid: "{...}"
)]

// Optional: dependency on another collection
[assembly: ImportCollection("{2d3f9a21-9602-4549-b5df-5e09a9dae57e}", name: "Bootstrap")]
```

---

## 4. Resource Files → Convention + Optional Attributes

**Convention-based (zero config for standard cases):**

The installer looks for these fixed filenames in the assembly's embedded resources or alongside the DLL:

| Filename | Deploys to |
|----------|-----------|
| `HelpFiles.zip` | helpFiles |
| `wwwFiles.zip` | wwwroot |
| `cdnFiles.zip` | cdnFiles |
| `privateFiles.zip` | privateFiles |
| `layoutFiles.zip` | layoutFiles |

**Override with assembly attributes when needed:**
```csharp
[assembly: CollectionResource("assets-v2.zip", ResourceType.Www, path: "myCollection")]
[assembly: CollectionResource("layouts.zip", ResourceType.LayoutFiles)]
```

---

## 5. Navigator Entries → Inline or Attribute

Simple navigator entries on addons use the `[Navigator]` attribute (shown above). Standalone navigator entries (not tied to an addon) use an assembly-level attribute:

```csharp
[assembly: NavigatorEntry(
    name: "Blog Settings",
    nameSpace: "Settings.Blog",
    contentName: "Blog Posts",
    adminOnly: false
)]
```

---

## 6. SQL Indexes → Attribute on Model

```csharp
[ContentDefinition(...)]
[SqlIndex("title,authorId")]
[SqlIndex("publishedDate")]
public class BlogPostModel : DbBaseModel { ... }
```

---

## 7. Data Records → Static Method Convention

Seed data doesn't map cleanly to attributes (values can be complex). Implement a static `SeedData` method on the model that the installer calls:

```csharp
[ContentDefinition(...)]
public class BlogCategoryModel : DbBaseModel {
    public static void SeedData(CPBaseClass cp) {
        BlogCategoryModel.createByUniqueName<BlogCategoryModel>(cp, "General")
            ?? BlogCategoryModel.addDefault<BlogCategoryModel>(cp).Save(cp);
    }
}
```

Alternatively, keep seed data records in a companion JSON file alongside the assembly.

---

## Implementation Phases

**Phase 1 — Define attribute classes** (no behavior changes yet)
Create `ContentDefinitionAttribute`, `ContentFieldAttribute`, `AddonAttribute`, `CollectionInfoAttribute`, `IncludeAddonAttribute`, etc. in a new `Contensive.Attributes` namespace in the Models project or BaseClasses.

**Phase 2 — Build the attribute scanner**
An `AttributeCollectionInstaller` that reflects an assembly, reads the attributes, and produces the same in-memory metadata model the XML installer produces. This runs *instead of* or *alongside* XML parsing.

**Phase 3 — Validate consistency**
Run the scanner against existing code where both XML and attributes coexist, and assert they agree. This is the integration test for correctness.

**Phase 4 — Migrate Base5 collection**
Convert `aoBase51.xml` entries to attributes, one section at a time. The scanner + XML installer can run in parallel during the transition.

**Phase 5 — Deprecate XML for new collections**
New collections use only attributes. XML still works for legacy collections.

---

## What This Doesn't Cover (yet)

- **FormXML (Settings addons)** — complex enough to warrant its own attribute-based DSL or companion JSON file. Could be a `[SettingsForm]` attribute pointing to an embedded resource.
- **InlineStyles / InlineJS** — small enough they can stay as embedded resource files or properties on `[Addon]`.
- **`tableMetadata` migration** — could eventually be auto-generated from `[ContentDefinition]` to eliminate the duplicate declaration, but that's a deeper refactor to `DbBaseModel`.

---

## Developer Experience Summary

**Before:**
1. Generate GUID, write XML CDef with all attributes
2. Add Field elements with EditSortPriority numbers
3. Write the matching C# model property
4. Add Addon XML, set DotNetClass, set all flags
5. Keep them in sync manually forever

**After:**
1. Write the model class with `[ContentDefinition]`
2. Add properties with `[ContentField]` — edit order is the declaration order
3. Write the addon class with `[Addon]`
4. Done — the installer reads the assembly
