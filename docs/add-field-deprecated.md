# Implementation Plan: Deprecated Field Attribute for Collection XML

## Context

This change addresses the need to mark database fields as deprecated in Contensive addon collection XML files. When a field is no longer needed but removing it would break existing installations with data in that column, the `deprecated` attribute allows collections to:

1. **Prevent new installations** from creating the field (saving database resources)
2. **Hard-delete existing installations** by removing the ccFields metadata record entirely while preserving the SQL column and its data

This maintains backward compatibility while phasing out unused fields, allowing the data to remain in the database for historical access.

## Implementation Overview

The implementation adds a new optional `deprecated` boolean attribute to the `<Field>` element in collection XML files. The installation logic will:

- **deprecated=false or omitted**: Install field normally (default behavior, no change)
- **deprecated=true + field doesn't exist**: Skip installation entirely (no ccFields record, no SQL column)
- **deprecated=true + field exists**: Delete the ccFields metadata record but preserve the SQL column and its data

## Critical Files

1. [source/Processor/Collection.xsd](source/Processor/Collection.xsd) - Lines 13-216: Add `deprecated` attribute to Field element schema
2. [source/Processor/Models/Domain/ContentFieldMetadataModel.cs](source/Processor/Models/Domain/ContentFieldMetadataModel.cs) - Add `deprecated` property to model
3. [source/Processor/Controllers/Collection/Install/CollectionInstallMetadataController.cs](source/Processor/Controllers/Collection/Install/CollectionInstallMetadataController.cs) - Core installation logic (XML parsing, SQL creation, metadata persistence)
4. [source/Processor/Controllers/DbController.cs](source/Processor/Controllers/DbController.cs) - Lines 2187-2206: `getContentFieldId()` method used to check field existence

## Detailed Implementation Steps

### Step 1: Update Collection.xsd Schema

**File**: [source/Processor/Collection.xsd](source/Processor/Collection.xsd)

**Location**: After line 212 (after the `uniqueName` attribute definition, before the closing `</xs:complexType>` tag at line 216)

**Changes**: Add new optional boolean attribute following the existing pattern:

```xml
<xs:attribute name="deprecated" type="xs:boolean">
    <xs:annotation>
        <xs:documentation>When true, the field is marked as deprecated. If the field does not exist, it will not be installed. If the field exists, its ccFields metadata record will be deleted while preserving the SQL database column and data.</xs:documentation>
    </xs:annotation>
</xs:attribute>
```

**Pattern Reference**: Follow lines 19-212 boolean attribute pattern (optional, no `use="required"`)

### Step 2: Add Deprecated Property to ContentFieldMetadataModel

**File**: [source/Processor/Models/Domain/ContentFieldMetadataModel.cs](source/Processor/Models/Domain/ContentFieldMetadataModel.cs)

**Location 1**: After line 67 in the `createDefault` method

**Changes**: Initialize the deprecated property to false:

```csharp
deprecated = false,
```

**Location 2**: After line 282 (after the `scramble` property)

**Changes**: Add the property definition:

```csharp
//
//====================================================================================================
/// <summary>
/// if true this field is deprecated and should not be installed if new, or should be deleted if it exists
/// </summary>
public bool deprecated { get; set; }
```

**Pattern Reference**: Follow existing boolean property pattern (lines 95, 113, 125, etc.)

### Step 3: Parse Deprecated Attribute from XML

**File**: [source/Processor/Controllers/Collection/Install/CollectionInstallMetadataController.cs](source/Processor/Controllers/Collection/Install/CollectionInstallMetadataController.cs)

**Location**: Line 281 in the `loadXML` method (after the `textLength` attribute parsing)

**Changes**: Add XML attribute parsing:

```csharp
metaDataField.deprecated = XmlController.getXMLAttributeBoolean(core, MetaDataChildNode, "Deprecated", DefaultMetaDataField.deprecated);
```

**Pattern Reference**: Follow lines 239, 245, 247-251, 267, 275 (`XmlController.getXMLAttributeBoolean` pattern)

### Step 4: Modify Stage 1 to Skip SQL Column Creation for Deprecated Non-Existent Fields

**File**: [source/Processor/Controllers/Collection/Install/CollectionInstallMetadataController.cs](source/Processor/Controllers/Collection/Install/CollectionInstallMetadataController.cs)

**Location**: Lines 573-578 in `installMetaDataMiniCollection_BuildDb` method

**Current Code**:
```csharp
foreach (KeyValuePair<string, ContentFieldMetadataModel> fieldKvp in metaKvp.Value.fields) {
    if (string.IsNullOrWhiteSpace(fieldKvp.Value.nameLc)) {
        logger.Warn($"{core.logCommonMessage}, Field [# " + fieldKvp.Value.id + "] in content [" + metaKvp.Value.name + "] in collection [" + Collection.name + "] cannot be added because the content tablename is empty.");
        continue;
    }
    core.db.createSQLTableField(metaKvp.Value.tableName, fieldKvp.Value.nameLc, fieldKvp.Value.fieldTypeId, fieldKvp.Value.textLength);
}
```

**Replace With**:
```csharp
foreach (KeyValuePair<string, ContentFieldMetadataModel> fieldKvp in metaKvp.Value.fields) {
    if (string.IsNullOrWhiteSpace(fieldKvp.Value.nameLc)) {
        logger.Warn($"{core.logCommonMessage}, Field [# " + fieldKvp.Value.id + "] in content [" + metaKvp.Value.name + "] in collection [" + Collection.name + "] cannot be added because the field name is empty.");
        continue;
    }
    //
    // -- if field is deprecated and does not exist, skip SQL column creation entirely
    if (fieldKvp.Value.deprecated) {
        int existingFieldId = DbController.getContentFieldId(core, metaKvp.Value.id, fieldKvp.Value.nameLc);
        if (existingFieldId == 0) {
            logger.Info($"{core.logCommonMessage}, Field [{fieldKvp.Value.nameLc}] in content [{metaKvp.Value.name}] is deprecated and does not exist. Skipping installation.");
            continue;
        }
    }
    core.db.createSQLTableField(metaKvp.Value.tableName, fieldKvp.Value.nameLc, fieldKvp.Value.fieldTypeId, fieldKvp.Value.textLength);
}
```

**Rationale**: Uses `DbController.getContentFieldId()` to check if the field exists in ccFields table before deciding whether to skip SQL column creation.

### Step 5: Modify Stage 6 to Handle Deprecated Field Metadata

**File**: [source/Processor/Controllers/Collection/Install/CollectionInstallMetadataController.cs](source/Processor/Controllers/Collection/Install/CollectionInstallMetadataController.cs)

**Location**: Lines 870-874 in `installMetaDataMiniCollection_buildDb_saveMetaDataToDb` method

**Current Code**:
```csharp
foreach (var nameValuePair in contentMetadata.fields) {
    ContentFieldMetadataModel fieldMetadata = nameValuePair.Value;
    if (fieldMetadata.dataChanged) {
        contentMetadata.verifyContentField(core, fieldMetadata, false, logMsgContext, existingFieldsByName, guidToIdCache, batchSqlList);
    }
}
```

**Replace With**:
```csharp
foreach (var nameValuePair in contentMetadata.fields) {
    ContentFieldMetadataModel fieldMetadata = nameValuePair.Value;
    if (fieldMetadata.dataChanged) {
        //
        // -- prevent deprecation of base fields
        if (fieldMetadata.deprecated && fieldMetadata.isBaseField) {
            logger.Warn($"{core.logCommonMessage}, Field [{fieldMetadata.nameLc}] in content [{contentMetadata.name}] is marked as deprecated but is a base field. Ignoring deprecated flag.");
            fieldMetadata.deprecated = false;
        }
        //
        // -- handle deprecated fields: delete existing, skip non-existent
        if (fieldMetadata.deprecated) {
            bool fieldExists = existingFieldsByName.ContainsKey(fieldMetadata.nameLc.ToLowerInvariant());
            if (!fieldExists && fieldMetadata.id > 0) {
                //
                // -- edge case: field has an ID but wasn't in preloaded dictionary
                logger.Warn($"{core.logCommonMessage}, Field [{fieldMetadata.nameLc}] has id={fieldMetadata.id} but not in existingFieldsByName dictionary. Treating as exists.");
                fieldExists = true;
            }
            if (fieldExists) {
                //
                // -- field exists: delete the ccFields record entirely
                var existingField = existingFieldsByName[fieldMetadata.nameLc.ToLowerInvariant()];
                logger.Info($"{core.logCommonMessage}, Field [{fieldMetadata.nameLc}] in content [{contentMetadata.name}] is deprecated and exists. Deleting ccFields record (id={existingField.id}).");
                batchSqlList.Add($"DELETE FROM ccFields WHERE id={existingField.id};");
            } else {
                //
                // -- field does not exist: skip installation entirely
                logger.Info($"{core.logCommonMessage}, Field [{fieldMetadata.nameLc}] in content [{contentMetadata.name}] is deprecated and does not exist. Skipping ccFields record creation.");
            }
            continue;
        }
        contentMetadata.verifyContentField(core, fieldMetadata, false, logMsgContext, existingFieldsByName, guidToIdCache, batchSqlList);
    }
}
```

**Rationale**:
- Checks `deprecated` flag before calling `verifyContentField()`
- Uses preloaded `existingFieldsByName` dictionary for efficient lookup (populated at lines 840-849)
- Hard-deletes ccFields record: `DELETE FROM ccFields WHERE id=...` (matches pattern from housekeeping cleanup in `ContentFieldsClass.cs:48`)
- Adds SQL to `batchSqlList` for batch execution (matches pattern at lines 914-918)
- Prevents base fields from being deprecated
- Handles edge case where field has ID but isn't in dictionary
- SQL column and data remain untouched in the content table

## Edge Cases Handled

### 1. Base Fields Protection
Base fields (isBaseField=true) are framework-provided and cannot be deprecated. The code logs a warning and overrides the flag to false.

### 2. Redirect and ManyToMany Fields
These field types don't create SQL columns. The existing logic in `verifyContentField()` (lines 979-984) and `createSQLTableField()` already handles this, so no special case needed.

### 3. Batch SQL Execution
Stage 6 uses batch SQL execution via `batchSqlList`. The DELETE statement is added to this list for efficient execution.

### 4. Field Has ID But Not in Dictionary
If a field has `fieldMetadata.id > 0` but isn't in `existingFieldsByName`, treat it as existing and delete it. This handles race conditions or cache misses.

### 5. Multiple Collections
If multiple collections modify the same field, the last collection to install wins. This is existing behavior and doesn't change.

## Testing Strategy

### Unit Tests
Create: `source/ProcessorTests/Controllers/Collection/CollectionInstallMetadataControllerTests_Deprecated.cs`

**Test Cases**:
1. Deprecated field not existing → Skip installation (no SQL column, no ccFields record)
2. Deprecated field existing → Delete ccFields record entirely, preserve SQL column and data
3. Non-deprecated field → Normal installation
4. Deprecated attribute omitted → Normal installation (defaults to false)
5. Base field with deprecated=true → Warning logged, flag ignored, normal installation
6. Redirect field deprecated → No SQL column (already behavior), no ccFields record
7. ManyToMany field deprecated → No SQL column (already behavior), no ccFields record
8. Batch SQL execution → Multiple deprecated fields deleted in one batch

### Integration Tests
Create: `source/ModelsTests/Integration/CollectionInstallDeprecatedFieldTests.cs`

Verify end-to-end flow from XML parsing through database updates.

### E2E Tests
Create: `tests/e2e/collection-deprecated-field.spec.ts`

1. Install collection with deprecated field (not existing) → Verify field not in admin UI
2. Install collection with deprecated field (existing) → Verify field disappears from admin UI, data preserved
3. Reinstall collection after field deprecated → Verify field remains soft-deleted

## Verification Steps

After implementation:

1. **Schema Validation**: Create a test collection XML with `deprecated="true"` attribute and verify it validates against the XSD
2. **Install New Field**: Install collection with deprecated field that doesn't exist → Verify no SQL column created, no ccFields record
3. **Install Existing Field**: Install collection with deprecated field that exists → Verify ccFields record deleted (SELECT returns 0 rows), SQL column and data unchanged
4. **Normal Field**: Install collection with deprecated=false or omitted → Verify normal installation
5. **Base Field Protection**: Install collection with base field marked deprecated → Verify warning logged, field installs normally
6. **Run Unit Tests**: Execute all test cases in `CollectionInstallMetadataControllerTests_Deprecated.cs`
7. **Run Integration Tests**: Execute `CollectionInstallDeprecatedFieldTests.cs`
8. **Run E2E Tests**: Execute `collection-deprecated-field.spec.ts`

## Documentation Updates

Update [patterns/addon-collection-pattern.md](patterns/addon-collection-pattern.md):

```markdown
## Deprecated Field Attribute

Mark fields as deprecated to phase them out while maintaining backward compatibility:

- **deprecated="false"** or omitted: Normal installation (default)
- **deprecated="true"** + field doesn't exist: Skip installation entirely
- **deprecated="true"** + field exists: Delete ccFields metadata record, preserve SQL column and data

### Example

```xml
<Field name="OldLegacyField"
       caption="Legacy Field (Deprecated)"
       fieldType="text"
       deprecated="true"
       active="false" />
```

### Important Notes

- Base fields cannot be deprecated
- SQL columns and their data are never deleted, only ccFields metadata records are removed
- Use this to phase out fields while maintaining data compatibility for historical access
```

## Rollback Strategy

If issues arise:
1. Remove `deprecated` attribute from Collection.xsd
2. Remove `deprecated` property from ContentFieldMetadataModel.cs
3. Remove XML parsing line from CollectionInstallMetadataController.cs
4. Remove Stage 1 and Stage 6 handling logic
5. Deleted ccFields records cannot be automatically restored (use database backup)
6. Recreate fields if needed by reinstalling collection without deprecated flag

## Performance Impact

- **Minimal overhead**: One `getContentFieldId()` SELECT query per deprecated field in Stage 1
- **Optimized Stage 6**: Uses preloaded `existingFieldsByName` dictionary (no additional queries)
- **Batch execution**: Maintains performance for multiple deprecated fields
