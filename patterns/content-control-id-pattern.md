
# Content Control ID Pattern

> All patterns and API reference: [Patterns Index](https://raw.githubusercontent.com/contensive/Contensive5/refs/heads/master/patterns/index.md)

## Overview

Every record in a Contensive database table has a `contentControlId` field (column `contentcontrolid`). This integer links the record to a content definition (CDef) in the `cccontent` table. It controls which content definition "owns" the record, determining visibility in the admin UI and query filtering.

## Content Hierarchy

Content definitions can form parent-child hierarchies. Multiple content definitions can share the same underlying SQL table, each representing a subset of records.

```
Layouts (root, parentId=0)       → table: cclayouts
    └── Email Layouts (child)    → table: cclayouts (same table)
    └── Report Layouts (child)   → table: cclayouts (same table)
```

The `parentId` field in `cccontent` defines this hierarchy:
- **Root content:** `parentId <= 0` — no parent, owns the entire table
- **Child content:** `parentId > 0` — a subset of records within the parent's table

## The Meaning of contentControlId=0

When a record is inserted directly by **table name** (via `DbBaseModel.verify`, `DbBaseModel.addEmpty`, or `DbController.insert`), the `contentControlId` defaults to **0**. This is not an error — it is a valid sentinel value meaning "this record belongs to the root content for this table."

### How contentControlId Gets Set

| Insert Method | contentControlId Value | Why |
|---------------|----------------------|-----|
| `CsModel.insert(contentName)` with child content (`parentId > 0`) | The child content's ID | Set explicitly by CsModel insert logic |
| `CsModel.insert(contentName)` with root content (`parentId <= 0`) | 0 | Root content — left as 0 |
| `DbBaseModel.verify<T>()` / `DbBaseModel.addEmpty<T>()` | 0 | Inserts by table name, no content context |
| `DbController.insert(tableName)` | 0 | Raw table insert, no content context |
| `LayoutController.updateLayout()` with explicit `layoutContentId` | The provided ID | Set via direct SQL update |

### Key Code Locations

**Default on insert** — `source/Processor/Controllers/DbController.cs:791`:
```csharp
if (sqlList["contentcontrolid"] == null) {
    sqlList.Add("contentcontrolid", encodeSQLNumber(0));
}
```

**CsModel child content resolution** — `source/Processor/Models/Domain/CsModel.cs:290-294`:
```csharp
case "contentcontrolid": {
    if (metaData.parentId > 0) {
        sqlList.Add(field.nameLc, metaData.id.ToString());
    }
    break;
}
```

Only child contents set `contentControlId` to their own ID. Root contents leave it as 0.

## Query-Time Resolution

The platform resolves `contentControlId=0` at query time using `legacyContentControlCriteria`, a pre-built SQL WHERE clause stored on each `ContentMetadataModel`.

### Root Content (parentId <= 0)

For root contents, the criteria is simply `(1=1)` — all records in the table match, regardless of `contentControlId` value.

**Source** — `source/Processor/Models/Domain/ContentMetadataModel.cs:636`:
```csharp
result.legacyContentControlCriteria = (result.parentId <= 0)
    ? "(1=1)"
    : getLegacyContentControlCriteria(core, result.id, ...);
```

### Child Content (parentId > 0)

For child contents, the criteria includes both the child's ID and `contentControlId=0` for the root:

```sql
((cclayouts.contentcontrolId=0) or (cclayouts.contentcontrolId=90) or (cclayouts.contentcontrolId=91))
```

The recursive method `getLegacyContentControlCriteria` builds this by walking the content hierarchy. The first (root) content always includes `contentControlId=0`.

**Source** — `source/Processor/Models/Domain/ContentMetadataModel.cs:737-762`:
```csharp
// first contentid in list, include contentid 0
if (parentIdList.Count == 0) {
    returnCriteria += "(" + contentTableName + ".contentcontrolId=0)or";
}
parentIdList.Add(contentId);
returnCriteria += "(" + contentTableName + ".contentcontrolId=" + contentId + ")";
// recurse into children
foreach (var childContent in ContentModel.createList<ContentModel>(...)) {
    returnCriteria += "or" + getLegacyContentControlCriteria(...);
}
```

### How CsModel.open Uses It

When opening records by content name, `CsModel.open` applies the criteria:

**Source** — `source/Processor/Models/Domain/CsModel.cs:2125`:
```csharp
string sqlContentCriteria = contentMetaData.legacyContentControlCriteria;
```

This ensures that opening "Layouts" returns all layout records (including those with `contentControlId=0`), while opening "Email Layouts" returns only records with `contentControlId` matching that child content or its descendants.

## Explicit Assignment

To explicitly set `contentControlId` on a record (e.g., to move it to a child content), use `ContentMetadataModel.setContentControlId`:

**Source** — `source/Processor/Models/Domain/ContentMetadataModel.cs:1739-1754`:
```csharp
public void setContentControlId(CoreController core, int recordId, int newContentControlID) {
    core.db.executeNonQuery(
        $"update {tableName} set contentcontrolid={newContentControlID} where id={recordId}"
    );
}
```

This method also recursively updates child records if the table has a `parentid` field (for tree-structured data within a content).

## Summary

| Scenario | contentControlId | Query Behavior |
|----------|-----------------|----------------|
| Table has one content (root only) | 0 | `(1=1)` — all records match |
| Table has parent + children, record is root | 0 | Matched by root content queries via `contentControlId=0` clause |
| Table has parent + children, record is child | Child content's ID | Matched only by that child content's queries |
| Record inserted by table name (DbBaseModel) | 0 | Treated as root content |
| Record inserted by content name (CsModel) | 0 if root, content ID if child | Set during insert |

**Key takeaway:** `contentControlId=0` is the normal, expected value for records belonging to a root content or inserted by table name. The platform's query logic handles it correctly at runtime.
