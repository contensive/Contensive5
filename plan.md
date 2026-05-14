# Plan: Add `textLength` to Content Fields

## Problem
Text fields in ccFields are created as `nvarchar(255)` but some fields (like `Fingerprint` at 272 chars) need longer lengths. There is no metadata to configure this per-field.

## Overview
Add a `textLength` integer property to ccFields metadata. When 0 or blank, default to 255 (current behavior). When set, use that value for the SQL column width and for the admin editor's `maxlength` attribute.

---

## Step 1: Add `TextLength` to the base collection XML (`aoBase51.xml`)

**File:** [aoBase51.xml](source/Processor/aoBase51.xml)

- Add a new `<Field>` element inside the "Content Fields" CDef (around line 135):
  ```xml
  <Field Name="TextLength" Active="true" AdminOnly="0" Authorable="1" Caption="Text Length" DeveloperOnly="1" EditSortPriority="1015" FieldType="Integer" HtmlContent="0" ... DefaultValue="0" ...>
    <HelpDefault>For text fields, the nvarchar length in the database. If 0 or blank, defaults to 255.</HelpDefault>
  </Field>
  ```
- Add `TextLength="512"` attribute to the Visitors.Fingerprint field definition (if it's in the base XML), or to the appropriate collection XML that defines the Fingerprint field

---

## Step 2: Add `textLength` to `ContentFieldModel` (database model)

**File:** [ContentFieldModel.cs](source/Models/Models/Db/ContentFieldModel.cs) ~line 144

- Add property:
  ```csharp
  /// <summary>
  /// For text fields, the nvarchar length in the database. If 0, defaults to 255.
  /// </summary>
  public int textLength { get; set; }
  ```

---

## Step 3: Add `textLength` to `ContentFieldMetadataModel` (runtime metadata model)

**File:** [ContentFieldMetadataModel.cs](source/Processor/Models/Domain/ContentFieldMetadataModel.cs)

- Add property (~line 275):
  ```csharp
  /// <summary>
  /// For text fields, the nvarchar length in the database. If 0, defaults to 255.
  /// </summary>
  public int textLength { get; set; }
  ```
- Update `createDefault()` (~line 31) to include `textLength = 0` in the initializer

---

## Step 4: Add `TextLength` to `verifyBasicTables` in `BuildController`

**File:** [BuildController.cs](source/Processor/Controllers/Build/BuildController.cs) ~line 833

- Add after existing ccFields field definitions:
  ```csharp
  core.db.createSQLTableField("ccFields", "TextLength", CPContentBaseClass.FieldTypeIdEnum.Integer);
  ```

---

## Step 5: Read `TextLength` from XML during collection install

**File:** [CollectionInstallMetadataController.cs](source/Processor/Controllers/Collection/Install/CollectionInstallMetadataController.cs) ~line 282

- In `loadXML()`, add after `editorAddonGuid` line:
  ```csharp
  metaDataField.textLength = XmlController.getXMLAttributeInteger(core, MetaDataChildNode, "TextLength", DefaultMetaDataField.textLength);
  ```

---

## Step 6: Save `textLength` to DB during metadata install

**File:** [ContentMetadataModel.cs](source/Processor/Models/Domain/ContentMetadataModel.cs) ~line 976

- In `verifyContentField()`, add `textLength` to the `sqlList` NameValueCollection:
  ```csharp
  { "textlength", DbController.encodeSQLNumber(fieldMetadata.textLength) }
  ```
- Gate behind a version check similar to `editgroup` (line 980) so it only writes if the column exists:
  ```csharp
  if (!GenericController.versionIsOlder(core.siteProperties.dataBuildVersion, "<new-version>")) {
      sqlList.Add("textlength", DbController.encodeSQLNumber(fieldMetadata.textLength));
  }
  ```

---

## Step 7: Use `textLength` when creating SQL table fields

**File:** [DbController.cs](source/Processor/Controllers/DbController.cs)

### 7a: Add overload for `getSQLAlterColumnType` (~line 759)
- Add a new overload that accepts `textLength`:
  ```csharp
  public string getSQLAlterColumnType(CPContentBaseClass.FieldTypeIdEnum fieldType, int textLength)
  ```
- For the Text/File/etc. case (line 780-788), return `$"nvarchar({effectiveLength}) NULL"` where `effectiveLength = textLength > 0 ? textLength : 255`
- The existing parameterless version continues to default to 255

### 7b: Add overload for `createSQLTableField` (~line 606)
- Add a new overload:
  ```csharp
  public void createSQLTableField(string tableName, string fieldName, CPContentBaseClass.FieldTypeIdEnum fieldType, int textLength, bool clearMetadataCache = false)
  ```
- This calls `getSQLAlterColumnType(fieldType, textLength)` instead of `getSQLAlterColumnType(fieldType)`

### 7c: Update `installMetaDataMiniCollection_BuildDb` stage 1 (~line 581)
- In [CollectionInstallMetadataController.cs](source/Processor/Controllers/Collection/Install/CollectionInstallMetadataController.cs):581, pass `textLength`:
  ```csharp
  core.db.createSQLTableField(metaKvp.Value.tableName, fieldKvp.Value.nameLc, fieldKvp.Value.fieldTypeId, fieldKvp.Value.textLength);
  ```

### 7d: Update `verifyContentField` table field creation (~line 939)
- In [ContentMetadataModel.cs](source/Processor/Models/Domain/ContentMetadataModel.cs):939, pass `textLength`:
  ```csharp
  db.createSQLTableField(tableName, fieldMetadata.nameLc, fieldMetadata.fieldTypeId, fieldMetadata.textLength);
  ```

---

## Step 8: Verify field length during housekeeping

**File:** [ContentFieldsClass.cs](source/Processor/Addons/Housekeeping/ContentFieldsClass.cs) (daily tasks, ~line 39)

- Add logic to `executeDailyTasks`:
  1. Query all text-type fields from ccFields that have `textLength > 0`, joined to cccontent and cctables to get the SQL table name
  2. For each, look up the actual SQL column schema using `TableSchemaModel.getTableSchema()`
  3. Compare `ColumnSchemaModel.CHARACTER_MAXIMUM_LENGTH` to the field's `textLength`
  4. If the db column is shorter than `textLength`:
     - Check `tableSchema.indexes` for any index whose `indexKeyList` contains the column name
     - If indexes exist on the column, drop them first (same pattern as [BuildController.cs:406-414](source/Processor/Controllers/Build/BuildController.cs#L406-L414)):
       ```csharp
       foreach (TableSchemaModel.IndexSchemaModel index in tableSchema.indexes) {
           if (index.indexKeyList.Contains(column.COLUMN_NAME)) {
               core.db.deleteIndex(tableName, index.index_name);
           }
       }
       ```
     - ALTER the column to match:
       ```sql
       ALTER TABLE {tableName} ALTER COLUMN {fieldName} nvarchar({textLength}) NULL
       ```
     - Recreate any dropped indexes:
       ```csharp
       foreach (TableSchemaModel.IndexSchemaModel index in droppedIndexes) {
           core.db.createSQLIndex(tableName, index.index_name, index.index_keys);
       }
       ```
  5. Log the change

---

## Step 9: Upgrade migration — populate `textLength` from current database schema

**File:** [BuildController.cs](source/Processor/Controllers/Build/BuildController.cs)

- Add a new method `verifyTextFieldLengths()` called from the build process (near `verifySqlfieldCompatibility`):
  1. Query all ccFields records where `type` is a text-type (2=Text, 6=File, 10=FileText, etc.) and `textLength` is 0 or null
  2. For each, look up the actual column's `CHARACTER_MAXIMUM_LENGTH` from `TableSchemaModel`
  3. If the actual length is not 255, update the ccFields record's `textLength` to match the actual db length
  4. This handles existing fields that were manually altered to be wider

---

## Step 10: Admin edit — use `textLength` for text input maxlength

### 10a: Edit Modal (new admin UI)
**File:** [EditModalViewModel_Field.cs](source/Processor/Models/Domain/EditModalViewModel_Field.cs) ~line 81

- Change from:
  ```csharp
  textMaxLength = isText ? 255 : (isTextLong ? 65353 : ((isHtml || isHtmlCode) ? 65535 : 255));
  ```
- To:
  ```csharp
  int effectiveTextLength = (field.textLength > 0) ? field.textLength : 255;
  textMaxLength = isText ? effectiveTextLength : (isTextLong ? 65353 : ((isHtml || isHtmlCode) ? 65535 : effectiveTextLength));
  ```

### 10b: Legacy Admin Text Editor
**File:** [AdminUIEditorController.cs](source/Processor/Controllers/EditControls/AdminUIEditorController.cs) ~line 724

- Add a `textLength` parameter to the `getTextEditor` method (or an overload):
  ```csharp
  public static string getTextEditor(CoreController core, string fieldName, string fieldValue, bool readOnly, string htmlId, bool required, int textLength)
  ```
- Change hardcoded `255` at line 733 to `textLength > 0 ? textLength : 255`
- Update callers to pass the field's `textLength`

---

## Step 11: Update the Visitors Fingerprint field

- In the collection XML that defines Visitors content, add `TextLength="512"` (or appropriate value) to the Fingerprint `<Field>` element
- This will cause the next install/upgrade to widen the database column

---

## Files Modified (summary)

| # | File | Change |
|---|------|--------|
| 1 | `source/Processor/aoBase51.xml` | Add TextLength field to "Content Fields" CDef; set TextLength on Fingerprint field |
| 2 | `source/Models/Models/Db/ContentFieldModel.cs` | Add `textLength` property |
| 3 | `source/Processor/Models/Domain/ContentFieldMetadataModel.cs` | Add `textLength` property + default |
| 4 | `source/Processor/Controllers/Build/BuildController.cs` | Add TextLength to verifyBasicTables; add `verifyTextFieldLengths()` migration |
| 5 | `source/Processor/Controllers/Collection/Install/CollectionInstallMetadataController.cs` | Read TextLength from XML; pass to createSQLTableField |
| 6 | `source/Processor/Models/Domain/ContentMetadataModel.cs` | Save textLength in verifyContentField; pass to createSQLTableField |
| 7 | `source/Processor/Controllers/DbController.cs` | Add textLength overloads for getSQLAlterColumnType and createSQLTableField |
| 8 | `source/Processor/Addons/Housekeeping/ContentFieldsClass.cs` | Verify db column length matches textLength |
| 9 | `source/Processor/Models/Domain/EditModalViewModel_Field.cs` | Use textLength for textMaxLength |
| 10 | `source/Processor/Controllers/EditControls/AdminUIEditorController.cs` | Use textLength for maxlength in text inputs |
