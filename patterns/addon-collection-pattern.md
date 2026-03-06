
# Addon Collection XML Pattern

## Overview

An Addon Collection XML file defines everything needed to install and configure a Contensive addon collection. It declares the collection metadata, content definitions (CDefs), addons, navigator entries, SQL indexes, data records, resource files, and dependency imports. The installer reads this file to create or update all database metadata, deploy files, and register addons.

The canonical example is `source/Processor/aoBase51.xml` (the Base5 collection). The XSD schema is published at `https://contensive.s3.amazonaws.com/xsd/Collection.xsd`.

## File Structure

The root element is `<Collection>` and contains child elements in this order:

```xml
<?xml version="1.0"?>
<Collection
    Name="MyCollection"
    Guid="{XXXXXXXX-XXXX-XXXX-XXXX-XXXXXXXXXXXX}"
    System="false"
    OnInstallAddonGuid="{XXXXXXXX-XXXX-XXXX-XXXX-XXXXXXXXXXXX}"
    updateable="true"
    xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"
    xsi:noNamespaceSchemaLocation="https://contensive.s3.amazonaws.com/xsd/Collection.xsd"
>
    <!-- Content Definitions (CDefs) -->
    <!-- Addons -->
    <!-- Navigator Entries -->
    <!-- SQL Indexes -->
    <!-- Data Records -->
    <!-- Import Collections (dependencies) -->
    <!-- Resources -->
</Collection>
```

### Collection Attributes

| Attribute | Required | Description |
|-----------|----------|-------------|
| `Name` | Yes | Display name of the collection |
| `Guid` | Yes | Unique identifier. Generate with `System.Guid.NewGuid()` and wrap in braces `{...}` |
| `System` | No | `"true"` for core system collections, `"false"` (default) for user collections |
| `OnInstallAddonGuid` | No | GUID of an addon in this collection to execute after installation (e.g., to run `cp.Layout.updateLayout()`) |
| `updateable` | No | `"true"` allows the collection to be updated on reinstall |

## Content Definitions (CDef)

A `<CDef>` element defines a content table and its fields. It maps to a database table and configures how the admin site displays and manages that content.

```xml
<CDef
    Name="My Content"
    NavTypeId="Content"
    AddonCategoryId=""
    ActiveOnly="1"
    AdminOnly="0"
    AliasID="id"
    AliasName="name"
    AllowAdd="1"
    AllowDelete="1"
    AllowContentTracking="0"
    AllowTopicRules="0"
    AllowWorkflowAuthoring="0"
    AuthoringDataSourceName="DEFAULT"
    AuthoringTableName="myTable"
    ContentDataSourceName="DEFAULT"
    ContentTableName="myTable"
    DefaultSortMethod="By Name"
    DeveloperOnly="0"
    DropDownFieldList="NAME"
    EditorGroupName=""
    Parent=""
    AllowContentChildTool="0"
    NavIconType="Content"
    IconLink=""
    IconWidth=""
    IconHeight=""
    IconSprites=""
    Guid="{XXXXXXXX-XXXX-XXXX-XXXX-XXXXXXXXXXXX}"
>
    <Field ... />
    <Field ... />
</CDef>
```

### Key CDef Attributes

| Attribute | Description |
|-----------|-------------|
| `Name` | Content definition name, used in API calls like `cp.Content.GetTable()` |
| `Guid` | Unique identifier for this CDef |
| `ContentTableName` | Database table name (e.g., `ccMembers`, `myCustomTable`) |
| `AuthoringTableName` | Usually same as ContentTableName |
| `Parent` | Name of parent CDef for content inheritance |
| `NavTypeId` | Admin nav section: `Content`, `System`, `Tool`, `Report`, `Setting`, `Comm`, `Design`, `Other` |
| `AddonCategoryId` | Category for admin navigation grouping |
| `DefaultSortMethod` | Default listing sort: `By Name`, `By Date`, `By Alpha Sort Order Field`, etc. |
| `AllowAdd` / `AllowDelete` | `"1"` or `"0"` to allow adding/deleting records |
| `AdminOnly` / `DeveloperOnly` | `"1"` restricts visibility to admin/developer users |
| `DropDownFieldList` | Field name(s) shown in dropdown selectors |

### Field Element

Each `<Field>` within a CDef defines a database column and its admin editor behavior.

```xml
<Field
    Name="FieldName"
    Active="true"
    AdminOnly="0"
    Authorable="1"
    Caption="Display Caption"
    DeveloperOnly="0"
    EditSortPriority="1000"
    FieldType="Text"
    HtmlContent="0"
    IndexColumn="0"
    IndexSortDirection="0"
    IndexSortOrder="0"
    IndexWidth="30"
    LookupContent=""
    NotEditable="0"
    Password="0"
    ReadOnly="0"
    RedirectContent=""
    RedirectId=""
    RedirectPath=""
    Required="0"
    TextBuffered="0"
    UniqueName="0"
    DefaultValue=""
    MemberSelectGroup=""
    EditGroup=""
    EditTab=""
    Scramble="0"
    LookupList=""
    ManyToManyContent=""
    ManyToManyRuleContent=""
    ManyToManyRulePrimaryField=""
    ManyToManyRuleSecondaryField=""
    LookupContentSqlFilter=""
    Guid="{XXXXXXXX-XXXX-XXXX-XXXX-XXXXXXXXXXXX}"
>
    <HelpDefault><![CDATA[Help text for this field]]></HelpDefault>
</Field>
```

### Field Types

| FieldType | Description |
|-----------|-------------|
| `Text` | Single-line text input |
| `LongText` | Multi-line text area |
| `Boolean` | Checkbox (true/false) |
| `Integer` | Whole number |
| `Float` | Decimal number |
| `Date` | Date picker |
| `File` | File upload |
| `FileImage` | Image file upload |
| `Lookup` | Dropdown select referencing another content. Set `LookupContent` to the target CDef name |
| `ManyToMany` | Many-to-many relationship. Set `ManyToManyContent`, `ManyToManyRuleContent`, `ManyToManyRulePrimaryField`, `ManyToManyRuleSecondaryField` |
| `Redirect` | Shows related records from another content. Set `RedirectContent` and `RedirectId` |
| `Link` | URL input |
| `ResourceLink` | Link to a resource file |
| `Html` | Rich HTML editor (WYSIWYG). Set `HtmlContent="1"` for raw HTML editing |
| `FileText` | Text stored in a file |
| `FileHTML` | HTML stored in a file |
| `FileCSS` | CSS stored in a file |
| `FileJavascript` | JavaScript stored in a file |
| `FileXML` | XML stored in a file |
| `Currency` | Currency value |
| `AutoIdIncrement` | Auto-incrementing ID |

### Key Field Attributes

| Attribute | Description |
|-----------|-------------|
| `EditSortPriority` | Order of appearance on the edit form (lower = higher) |
| `EditTab` | Tab name on the edit form (e.g., `"Features"`, `"Presentation"`, `"Control Info"`) |
| `IndexColumn` | Column position in admin listing (0-based, `99` = hidden) |
| `IndexWidth` | Relative width of column in admin listing (0 = hidden) |
| `LookupList` | Pipe-separated static lookup values (e.g., `"Option1|Option2|Option3"` or comma-separated) |
| `LookupContent` | CDef name for dynamic lookups |
| `LookupContentSqlFilter` | SQL WHERE filter for lookup content. Use `{fieldName}` for current record substitution |
| `DefaultValue` | Initial value for new records |
| `Required` | `"1"` = field cannot be blank |
| `UniqueName` | `"1"` = value must be unique across all records |
| `Password` | `"1"` = renders as password input |
| `Scramble` | `"1"` = value is scrambled in the database |
| `NotEditable` | `"1"` = write-once (editable only on add) |
| `ReadOnly` | `"1"` = never editable |
| `Authorable` | `"0"` = hidden from the edit form |

## Addon Element

An `<Addon>` registers an executable addon with the system.

```xml
<Addon Name="My Addon" Guid="{XXXXXXXX-XXXX-XXXX-XXXX-XXXXXXXXXXXX}" Type="Add-on">
    <Description>What this addon does</Description>
    <Category><![CDATA[CategoryName]]></Category>
    <DotNetClass><![CDATA[MyNamespace.MyClass]]></DotNetClass>
    <Admin>No</Admin>
    <Content>No</Content>
    <Template>No</Template>
    <Email>No</Email>
    <RemoteMethod>No</RemoteMethod>
    <BlockEditTools>Yes</BlockEditTools>
    <HtmlDocument>No</HtmlDocument>
    <OnBodyEnd>No</OnBodyEnd>
    <ProcessInterval></ProcessInterval>
    <JavascriptForceHead>No</JavascriptForceHead>
    <ArgumentList><![CDATA[argName=[defaultValue]]]></ArgumentList>
    <jsheadscriptsrc>/path/to/script.js</jsheadscriptsrc>
    <styleslinkhref>/path/to/style.css</styleslinkhref>
    <Styles><![CDATA[/* inline CSS */]]></Styles>
    <JavascriptInHead><![CDATA[/* inline JS */]]></JavascriptInHead>
    <IncludeAddon Name="Dependency Addon" Guid="{...}"/>
    <FormXML><![CDATA[<Form>...</Form>]]></FormXML>
    <Scripting Language="" EntryPoint="" Timeout="5000">
        <Code><![CDATA[ ]]></Code>
    </Scripting>
</Addon>
```

### Addon Type Attribute

| Type | Description |
|------|-------------|
| `Add-on` | General-purpose addon |
| `Tool` | Admin tool, appears in the Tools section. Set `<Admin>Yes</Admin>` |
| `Widget` | Page widget / design block. Usually sets `<Content>Yes</Content>` and/or `<Template>Yes</Template>` |
| `Setting` | Settings form addon, uses `<FormXML>` |
| `Task` | Background process task. Set `<ProcessInterval>` in minutes |

### Key Addon Child Elements

| Element | Description |
|---------|-------------|
| `<DotNetClass>` | Fully qualified .NET class name (namespace.classname) implementing `CPAddonBaseClass` |
| `<Category>` | Admin category for organizing. Use `Category.Subcategory` format for nesting |
| `<Admin>` | `"Yes"` = available in admin tools |
| `<Content>` | `"Yes"` = can be placed on content pages (drag-and-drop) |
| `<Template>` | `"Yes"` = can be placed on templates |
| `<Email>` | `"Yes"` = can be used in email |
| `<RemoteMethod>` | `"Yes"` = callable via HTTP as an API endpoint |
| `<BlockEditTools>` | `"Yes"` = suppresses edit toolbar when this addon runs |
| `<HtmlDocument>` | `"Yes"` = addon returns a complete HTML document |
| `<OnBodyEnd>` | `"Yes"` = addon output is injected before `</body>` on every page |
| `<ProcessInterval>` | Minutes between background task executions (for Type="Task" addons) |
| `<JavascriptForceHead>` | `"Yes"` = force JavaScript into `<head>` |
| `<ArgumentList>` | Addon arguments. Format: `argName=[defaultValue]` or `argName=[listid(contentName)]` for dropdowns |
| `<jsheadscriptsrc>` | Path to external JavaScript file |
| `<styleslinkhref>` | Path to external CSS file |
| `<Styles>` | Inline CSS wrapped in `<![CDATA[...]]>` |
| `<JavascriptInHead>` | Inline JavaScript wrapped in `<![CDATA[...]]>` |
| `<IncludeAddon>` | Dependency addon loaded before this addon. Requires `Name` and `Guid` attributes |
| `<FormXML>` | Settings form definition (for Type="Setting"). Contains `<Form>` with `<Tab>` and `<SiteProperty>` elements |
| `<Description>` | Human-readable description of the addon |

### FormXML Structure (for Settings Addons)

```xml
<FormXML><![CDATA[
<Form>
    <Description>Form description</Description>
    <Tab Name="TabName" heading="Tab Heading" description="Tab description">
        <SiteProperty
            Caption="Display Label"
            Name="PropertyName"
            ReadOnly="0"
            Type="Text"
            Selector=""
            Description="Help text"
        >DefaultValue</SiteProperty>
    </Tab>
</Form>
]]></FormXML>
```

**SiteProperty Types**: `Text`, `Boolean`, `Integer`, `text`

**SiteProperty Selector**: Use `[None:0|listid(ContentName)]` to create a dropdown from a content table.

## Navigator Entry

Navigator entries define links in the admin site navigation tree.

```xml
<NavigatorEntry
    Name="Entry Name"
    NameSpace="Tools.Advanced"
    NavIconTitle="Tooltip text"
    NavIconType="Tools"
    LinkPage="?af=35"
    ContentName=""
    AdminOnly="0"
    DeveloperOnly="0"
    NewWindow=""
    Active="true"
    AddonGuid=""
    Guid="{XXXXXXXX-XXXX-XXXX-XXXX-XXXXXXXXXXXX}"
/>
```

| Attribute | Description |
|-----------|-------------|
| `Name` | Display name in the nav tree |
| `NameSpace` | Dot-separated path in the nav tree (e.g., `"Tools"`, `"Tools.Advanced"`) |
| `NavIconType` | Section: `Tools`, `Content`, `Report`, `Setting`, `Comm`, `Design` |
| `LinkPage` | URL or query string for the link target |
| `ContentName` | Content definition name to link to its listing page |
| `AddonGuid` | GUID of an addon to run when this entry is clicked |
| `AdminOnly` / `DeveloperOnly` | `"1"` restricts visibility |

## SQL Index

Declares database indexes to be created during installation.

```xml
<SQLIndex
    IndexName="indexName"
    DataSourceName="Default"
    TableName="tableName"
    FieldNameList="Field1,Field2"
/>
```

| Attribute | Description |
|-----------|-------------|
| `IndexName` | Unique name for the index |
| `DataSourceName` | Usually `"Default"` |
| `TableName` | Database table to index |
| `FieldNameList` | Comma-separated list of fields to include in the index |

## Data Records

The `<Data>` section seeds initial records into content tables.

```xml
<Data>
    <Record Content="Content Name" Guid="{...}" Name="Record Name">
        <field Name="fieldName">value</field>
        <field Name="lookupField">{GUID-of-related-record}</field>
    </Record>
</Data>
```

- `Content` attribute must match an existing CDef name
- `Guid` uniquely identifies the record (used for updates on reinstall)
- `Name` sets the record's Name field
- Child `<field>` elements set field values by field name
- Lookup fields can reference other records by GUID

## Import Collections (Dependencies)

Declare other collections that must be installed before this collection.

```xml
<ImportCollection Name="Bootstrap">{2d3f9a21-9602-4549-b5df-5e09a9dae57e}</ImportCollection>
```

The element text content is the GUID of the required collection. The `Name` attribute is for readability.

## Resources

Declare files to be deployed during installation.

```xml
<Resource Name="filename.zip" Type="www" Path="targetFolder" />
<Resource Name="filename.zip" Type="files" Path="targetFolder" />
<Resource Name="helpfiles.zip" Type="privatefiles" Path="helpfiles/CollectionName" />
```

| Attribute | Description |
|-----------|-------------|
| `Name` | Filename of the resource (must be included in the collection package) |
| `Type` | Target location: `www` (web root wwwroot), `files` (public files), `privatefiles` (private files) |
| `Path` | Subfolder path within the target location |

## Steps to Create a New Addon Collection XML

1. **Create the XML file** with the `<Collection>` root element. Generate a unique GUID for the collection.

2. **Define CDefs** for any custom database tables your addons need. Each CDef requires:
   - A unique GUID
   - A `ContentTableName` (the actual database table name)
   - `<Field>` elements for each custom column

3. **Define Addons** for each executable component. Each addon requires:
   - A unique GUID
   - A `Type` (`Add-on`, `Tool`, `Widget`, `Task`, or `Setting`)
   - A `<DotNetClass>` pointing to the implementing class
   - Appropriate placement flags (`Admin`, `Content`, `Template`, `RemoteMethod`, etc.)

4. **Add Navigator Entries** if your addons need admin navigation links.

5. **Add SQL Indexes** for any custom tables that need query optimization.

6. **Seed Data Records** for any initial data (categories, default records, etc.).

7. **Declare Dependencies** with `<ImportCollection>` for any collections yours depends on.

8. **Include Resources** for any static files (CSS, JS, images, help files) that need deployment.

9. **Create an OnInstall addon** (optional) referenced by the collection's `OnInstallAddonGuid` attribute. This addon typically calls `cp.Layout.updateLayout()` to install layout files.

## Example: Minimal Collection with One Addon

```xml
<?xml version="1.0"?>
<Collection
    Name="My Custom Collection"
    Guid="{A1B2C3D4-E5F6-7890-ABCD-EF1234567890}"
    System="false"
    updateable="true"
    xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"
    xsi:noNamespaceSchemaLocation="https://contensive.s3.amazonaws.com/xsd/Collection.xsd"
>
    <CDef Name="My Settings" NavTypeId="Content" ActiveOnly="1" AdminOnly="0"
        AllowAdd="1" AllowDelete="1" ContentTableName="mySettings"
        AuthoringTableName="mySettings" DefaultSortMethod="By Name"
        DropDownFieldList="NAME" Guid="{B2C3D4E5-F678-9012-BCDE-F12345678901}">
        <Field Name="Name" Active="true" Authorable="1" Caption="Name"
            EditSortPriority="110" FieldType="Text" IndexColumn="0" IndexWidth="30">
            <HelpDefault><![CDATA[The name of this settings record]]></HelpDefault>
        </Field>
        <Field Name="Title" Active="true" Authorable="1" Caption="Title"
            EditSortPriority="1000" FieldType="Text" IndexColumn="1" IndexWidth="50">
            <HelpDefault><![CDATA[The display title]]></HelpDefault>
        </Field>
        <Field Name="Description" Active="true" Authorable="1" Caption="Description"
            EditSortPriority="1010" FieldType="LongText">
            <HelpDefault><![CDATA[Detailed description]]></HelpDefault>
        </Field>
    </CDef>

    <Addon Name="My Widget" Guid="{C3D4E5F6-7890-1234-CDEF-123456789012}" Type="Widget">
        <DotNetClass><![CDATA[MyNamespace.Addons.MyWidgetClass]]></DotNetClass>
        <Category><![CDATA[Applications]]></Category>
        <Content>Yes</Content>
        <Template>Yes</Template>
        <BlockEditTools>Yes</BlockEditTools>
        <IncludeAddon Name="Bootstrap" Guid="{EF1FD66C-D62F-4BD2-BF07-38F47996EBB3}"/>
        <IncludeAddon Name="Contensive Base Assets" Guid="{F83EE7F9-79DA-4B3F-A1CD-45AEAD93D70F}"/>
    </Addon>

    <ImportCollection Name="Bootstrap">{2d3f9a21-9602-4549-b5df-5e09a9dae57e}</ImportCollection>
</Collection>
```

## Common Include Addon GUIDs

These are frequently referenced dependency addons:

| Name | GUID |
|------|------|
| JQuery | `{9C882078-0DAC-48E3-AD4B-CF2AA230DF80}` |
| Bootstrap | `{EF1FD66C-D62F-4BD2-BF07-38F47996EBB3}` |
| Contensive Base Assets | `{F83EE7F9-79DA-4B3F-A1CD-45AEAD93D70F}` |
| Font Awesome | `{1e2f82ae-6722-4564-a461-5b2e7c5e32b3}` |
| jQuery BlockUI | `{F6087787-E01E-4E09-AC02-502D0387E048}` |

## Conventions

- All GUIDs should be wrapped in braces: `{XXXXXXXX-XXXX-XXXX-XXXX-XXXXXXXXXXXX}`
- Use `<![CDATA[...]]>` for any element content that may contain special characters
- Table names conventionally use the `cc` prefix for core tables (e.g., `ccMembers`, `ccContent`)
- Field `EditSortPriority` values: `110` for Name field, `1000`+ for custom fields, `2000` for sort order, `9999` for deprecated fields
- Set `Authorable="0"` to hide deprecated fields from the edit form
- The `Name` field with `EditSortPriority="110"` is a standard convention across all CDefs
- Boolean values accept `"true"/"false"`, `"Yes"/"No"`, `"1"/"0"` depending on context
