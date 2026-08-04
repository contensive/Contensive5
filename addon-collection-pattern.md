# Addon Collection XML Pattern

This document defines the required patterns for Contensive addon collection XML files.

## CDef Field Nodes

When creating `<Field>` nodes inside a `<CDef>` block, every field **must** include the following attributes:

| Attribute | Description | Example |
|-----------|-------------|---------|
| `Name` | The field name in the database | `"serverSiteId"` |
| `Caption` | The caption displayed on the edit form | `"Server Site"` |
| `FieldType` | The type of field | `"Text"`, `"Integer"`, `"Boolean"`, `"Date"`, `"LongText"`, `"Lookup"`, `"Float"` |
| `EditTab` | The tab name on the edit form (empty string for the default tab) | `"Status"`, `"Monitor"`, `""` |
| `EditSortPriority` | A numeric string that determines the order of fields within the edit tab | `"1010"`, `"2020"` |

### EditSortPriority Numbering Convention

Use a numbering scheme that leaves room for future insertions:
- Increment by 10 within a tab (e.g., `1010`, `1020`, `1030`)
- Or increment by 1111 for wider spacing (e.g., `1111`, `2222`, `3333`)

### Additional Common Attributes

These attributes are not strictly required but are commonly used:

| Attribute | Default | Description |
|-----------|---------|-------------|
| `Active` | `"true"` | Whether the field is active |
| `Authorable` | `"true"` | Whether the field appears on the edit form |
| `LookupContent` | `""` | For `Lookup` fields, the content name to look up against |
| `DefaultValue` | `""` | Default value for new records |

### Example

```xml
<CDef Name="SEO Issues" ContentTableName="smSeoIssues" AdminOnly="True" Active="True" AllowAdd="True" AllowDelete="True">
    <Field Name="serverSiteId" Caption="Server Site" FieldType="Lookup" EditTab="" EditSortPriority="1010" LookupContent="Server Sites" Active="true" />
    <Field Name="severity" Caption="Severity" FieldType="Text" EditTab="" EditSortPriority="1020" Active="true" />
    <Field Name="category" Caption="Category" FieldType="Text" EditTab="" EditSortPriority="1030" Active="true" />
    <Field Name="description" Caption="Description" FieldType="LongText" EditTab="" EditSortPriority="1040" Active="true" />
    <Field Name="status" Caption="Status" FieldType="Text" EditTab="" EditSortPriority="1050" Active="true" />
    <Field Name="note" Caption="Note" FieldType="LongText" EditTab="Resolution" EditSortPriority="1010" Active="true" />
    <Field Name="completedDate" Caption="Completed Date" FieldType="Date" EditTab="Resolution" EditSortPriority="1020" Active="true" />
    <Field Name="completedBy" Caption="Completed By" FieldType="Text" EditTab="Resolution" EditSortPriority="1030" Active="true" />
</CDef>
```
