# Database API Reference

> Source files: `source/Models/Models/Db/DbBaseModel.cs`, `source/Models/Models/Db/DbBaseFieldsModel.cs`, `source/CPBase/BaseClasses/CPDbBaseClass.cs`, `source/CPBase/BaseClasses/CPCSBaseClass.cs`

## When to Use Which API

| API | Use When |
|-----|----------|
| **DbBaseModel** | CRUD on typed model classes (strongly typed, cached, recommended) |
| **CPDbBaseClass** (`cp.Db`) | Raw SQL queries, table/field checks, SQL encoding |
| **CPCSBaseClass** (`cp.CSNew()`) | Legacy content-set operations, admin form inputs, record navigation |

---

## DbBaseModel (Contensive.Models.Db)

All database model classes inherit from `DbBaseModel`. Every model requires a static `tableMetadata` property of type `DbBaseTableMetadataModel`.

### Base Fields (every table)

| Property | Type | Description |
|----------|------|-------------|
| `id` | `int` | Primary key |
| `name` | `string` | Record name, used in lookup lists |
| `ccguid` | `string` | GUID, auto-created |
| `active` | `bool` | Record active flag |
| `contentControlId` | `int` | Metadata content id |
| `createdBy` | `int?` | User id who created record |
| `createKey` | `int` | Used when creating new records |
| `dateAdded` | `DateTime?` | Date record was created |
| `modifiedBy` | `int?` | User id who last modified |
| `modifiedDate` | `DateTime?` | Date last modified |
| `sortOrder` | `string` | Optional sort order |

### Field Type Classes

These types are used as properties on model classes for file-backed fields:

| Type | Description |
|------|-------------|
| `FieldTypeFile` | References an external file (image, document). Properties: `filename`, `tempFileCopySource`, `privateFileCopySource`, `wwwFileCopySource`, `cdnFileCopySource`, `uploadRequestName` |
| `FieldTypeTextFile` | Text content stored in a CDN file. Properties: `filename`, `content` (lazy-loaded from CDN on read) |
| `FieldTypeHTMLFile` | HTML content stored in a file. Same interface as `FieldTypeTextFile` |
| `FieldTypeJavascriptFile` | JavaScript content stored in a file. Same interface as `FieldTypeTextFile` |
| `FieldTypeCSSFile` | CSS content stored in a file. Same interface as `FieldTypeTextFile` |

For text file types, set `.content` and call `save()` — the filename is managed automatically.

### createList&lt;T&gt;

Returns a `List<T>` of model objects from active records.

```csharp
// All overloads — each adds defaults for trailing parameters
static List<T> createList<T>(CPBaseClass cp, string sqlCriteria, string sqlOrderBy, int pageSize, int pageNumber, List<string> callersCacheNameList)
static List<T> createList<T>(CPBaseClass cp, string sqlCriteria, string sqlOrderBy, int pageSize, int pageNumber)
static List<T> createList<T>(CPBaseClass cp, string sqlCriteria, string sqlOrderBy, int pageSize)
static List<T> createList<T>(CPBaseClass cp, string sqlCriteria, string sqlOrderBy)
static List<T> createList<T>(CPBaseClass cp, string sqlCriteria)
static List<T> createList<T>(CPBaseClass cp)
```

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `cp` | `CPBaseClass` | required | The cp instance |
| `sqlCriteria` | `string` | `""` | SQL WHERE clause (without WHERE keyword). Active filter is always applied. |
| `sqlOrderBy` | `string` | `"id"` | SQL ORDER BY clause (without ORDER BY keyword) |
| `pageSize` | `int` | `99999` | Records per page. Must be > 0. |
| `pageNumber` | `int` | `1` | 1-based page number. Must be > 0. |
| `callersCacheNameList` | `List<string>` | `new List<string>()` | Cache keys affected by these records (for cache dependency tracking) |

**Important**: `sqlCriteria` is appended with `AND` to the built-in `(active>0)` filter. Only active records are returned.

```csharp
// Examples
var allPeople = DbBaseModel.createList<PersonModel>(cp);
var filtered = DbBaseModel.createList<PersonModel>(cp, "(name like '%smith%')", "name", 20, 1);
var recent = DbBaseModel.createList<PersonModel>(cp, $"(dateAdded>{cp.Db.EncodeSQLDate(cutoff)})", "dateAdded desc");
```

### create&lt;T&gt;

Returns a single model object, or `null`/`default` if not found.

```csharp
static T create<T>(CPBaseClass cp, int recordId)
static T create<T>(CPBaseClass cp, int recordId, ref List<string> callersCacheNameList)
static T create<T>(CPBaseClass cp, string recordGuid)
static T create<T>(CPBaseClass cp, string recordGuid, ref List<string> callersCacheNameList)
```

| Parameter | Type | Description |
|-----------|------|-------------|
| `cp` | `CPBaseClass` | The cp instance |
| `recordId` | `int` | Record id to load. Returns default if <= 0. |
| `recordGuid` | `string` | Record ccguid to load. Returns default if empty. |

```csharp
var person = DbBaseModel.create<PersonModel>(cp, 42);
var addon = DbBaseModel.create<AddonModel>(cp, "{guid-here}");
```

### createByUniqueName&lt;T&gt;

Returns the first active record matching the name field (model must have `nameFieldIsUnique = true` in tableMetadata).

```csharp
static T createByUniqueName<T>(CPBaseClass cp, string recordName)
static T createByUniqueName<T>(CPBaseClass cp, string recordName, ref List<string> callersCacheNameList)
```

### createFirstOfList&lt;T&gt;

Returns the first record from a filtered, ordered list, or `null` if none found.

```csharp
static T createFirstOfList<T>(CPBaseClass cp, string sqlCriteria, string sqlOrderBy)
```

### addDefault&lt;T&gt;

Creates a new record with default values from the content definition metadata (active=true, contentControlId, guid, timestamps, plus field defaults from the admin).

```csharp
static T addDefault<T>(CPBaseClass cp)
static T addDefault<T>(CPBaseClass cp, int userId)
static T addDefault<T>(CPBaseClass cp, Dictionary<string, string> defaultValues)
static T addDefault<T>(CPBaseClass cp, Dictionary<string, string> defaultValues, int userId)
static T addDefault<T>(CPBaseClass cp, Dictionary<string, string> defaultValues, int userId, ref List<string> callersCacheNameList)
```

### addEmpty&lt;T&gt;

Creates a new record with no default values — only an id and ccguid are assigned.

```csharp
static T addEmpty<T>(CPBaseClass cp)
static T addEmpty<T>(CPBaseClass cp, int userId)
```

### save

Saves the instance properties to the database. Returns the record id.

```csharp
int save(CPBaseClass cp)
int save(CPBaseClass cp, int userId)
int save(CPBaseClass cp, int userId, bool asyncSave)
```

If `id == 0`, a new record is inserted. Otherwise, the existing record is updated.

```csharp
var person = DbBaseModel.addDefault<PersonModel>(cp);
person.name = "Jane Smith";
person.save(cp);
// person.id is now set
```

### delete&lt;T&gt;

Deletes a record and invalidates its cache.

```csharp
static void delete<T>(CPBaseClass cp, int recordId)
static void delete<T>(CPBaseClass cp, string guid)
```

### deleteRows&lt;T&gt;

Deletes multiple records matching criteria. `sqlCriteria` is required and cannot be blank.

```csharp
static void deleteRows<T>(CPBaseClass cp, string sqlCriteria)
```

### getSelectSql&lt;T&gt;

Generates a SQL SELECT statement for the model's table. Always includes `(active>0)`.

```csharp
static string getSelectSql<T>(CPBaseClass cp, List<string> fieldList, string sqlCriteria, string sqlOrderBy)
static string getSelectSql<T>(CPBaseClass cp, List<string> fieldList, string sqlCriteria)
static string getSelectSql<T>(CPBaseClass cp, List<string> fieldList)
static string getSelectSql<T>(CPBaseClass cp)
```

| Parameter | Type | Description |
|-----------|------|-------------|
| `fieldList` | `List<string>` | Specific fields to select. If null/empty, selects all model properties. |
| `sqlCriteria` | `string` | WHERE clause (appended with AND) |
| `sqlOrderBy` | `string` | ORDER BY clause |

### Other Utility Methods

```csharp
// Get record name by id or guid
static string getRecordName<T>(CPBaseClass cp, int recordId)
static string getRecordName<T>(CPBaseClass cp, string guid)

// Get record id by guid or unique name
static int getRecordId<T>(CPBaseClass cp, string guid)
static int getRecordIdByUniqueName<T>(CPBaseClass cp, string recordName)

// Get default field values from content definition
static Dictionary<string, string> getDefaultValues<T>(CPBaseClass cp)
static Dictionary<string, string> getDefaultValues<T>(CPBaseClass cp, int createdModifiedById)

// Check if model type contains a field
static bool containsField<T>(string fieldName)

// Hierarchy checks (requires parentId field)
bool isParentOf<T>(CPBaseClass cp, int childRecordId)
bool isChildOf<T>(CPBaseClass cp, int parentRecordId)

// Reload from database
void reload(CPBaseClass cp)

// Populate model properties from request form data
void loadFromRequest(CPBaseClass cp)

// Cache invalidation
static void invalidateCacheOfRecord<T>(CPBaseClass cp, int recordId)
static void invalidateCacheOfTable<T>(CPBaseClass cp)
static string createDependencyKeyInvalidateOnChange<T>(CPBaseClass cp)
```

---

## CPDbBaseClass (cp.Db)

Direct SQL and table operations. Access via `cp.Db`.

### SQL Execution

```csharp
DataTable ExecuteQuery(string sql)
DataTable ExecuteQuery(string sql, int startRecord)
DataTable ExecuteQuery(string sql, int startRecord, int maxRecords)
int ExecuteScalar(string sql)
void ExecuteNonQuery(string sql)
void ExecuteNonQuery(string sql, ref int recordsAffected)
Task<int> ExecuteNonQueryAsync(string sql)
```

| Parameter | Type | Description |
|-----------|------|-------------|
| `sql` | `string` | SQL statement to execute |
| `startRecord` | `int` | 0-based record offset for paging |
| `maxRecords` | `int` | Maximum records to return |

### SQL Encoding

Always use these to prevent SQL injection:

```csharp
string EncodeSQLText(string sourceText)         // wraps in quotes, escapes
string EncodeSQLTextLike(string sourceText)      // for LIKE clauses
string EncodeSQLNumber(double sourceNumber)
string EncodeSQLNumber(int sourceNumber)
string EncodeSQLBoolean(bool sourceBoolean)
string EncodeSQLDate(DateTime sourceDate)
```

```csharp
// Example
string sql = $"select * from people where name={cp.Db.EncodeSQLText(userName)}";
using var dt = cp.Db.ExecuteQuery(sql);
```

### Table Operations

```csharp
int Add(string tableName, int createdByUserId)                           // returns new record id
DataTable Insert(string tableName, int createdByUserId)                  // returns DataTable with new record
DataTable Insert(string tableName, NameValueCollection sqlList, int createdByUserId)  // insert with field values
void Update(string tableName, string criteria, NameValueCollection sqlList)
void Update(string tableName, string criteria, NameValueCollection sqlList, bool async)
void Delete(string tableName, int recordId)
void Delete(string tableName, string guid)
void DeleteRows(string tableName, string sqlCriteria)
```

### Schema Checks

```csharp
bool IsTable(string tableName)
bool IsTableField(string tableName, string fieldName)
string GetConnectionString()
```

### File Path Helpers

```csharp
string CreateFieldPathFilename(string tableName, string fieldName, int recordId, CPContentBaseClass.FieldTypeIdEnum fieldTypeId)
string CreateUploadFieldPathFilename(string tableName, string fieldName, int recordId, string filename)
string CreateUploadFieldPath(string tableName, string fieldName, int recordId)
```

### Properties

```csharp
int SQLTimeout { get; set; }   // timeout in seconds for all Db methods
```

---

## CPCSBaseClass (cp.CSNew())

Content Set (CS) API for record-at-a-time operations. Create with `cp.CSNew()`.

### Opening Records

```csharp
// Open by content name with filtering and pagination
bool Open(string contentName, string sqlCriteria, string sortFieldList, bool activeOnly, string selectFieldList, int pageSize, int pageNumber)
bool Open(string contentName, string sqlCriteria, string sortFieldList, bool activeOnly, string selectFieldList, int pageSize)
bool Open(string contentName, string sqlCriteria, string sortFieldList, bool activeOnly, string selectFieldList)
bool Open(string contentName, string sqlCriteria, string sortFieldList, bool activeOnly)
bool Open(string contentName, string sqlCriteria, string sortFieldList)
bool Open(string contentName, string sqlCriteria)
bool Open(string contentName)

// Open a specific record
bool OpenRecord(string contentName, int recordId, string selectFieldList, bool activeOnly)
bool OpenRecord(string contentName, int recordId, string selectFieldList)
bool OpenRecord(string contentName, int recordId)
bool OpenRecord(string contentName, string recordGuid, string selectFieldList, bool activeOnly)
bool OpenRecord(string contentName, string recordGuid, string selectFieldList)
bool OpenRecord(string contentName, string recordGuid)

// Open with raw SQL
bool OpenSQL(string sql, string dataSourceName, int pageSize, int pageNumber)
bool OpenSQL(string sql, string dataSourceName, int pageSize)
bool OpenSQL(string sql, string dataSourceName)
bool OpenSQL(string sql)

// Open users in a group
bool OpenGroupUsers(string groupName, string sqlCriteria, string sortFieldList, bool activeOnly, int pageSize, int pageNumber)
// ... additional overloads with fewer parameters
bool OpenGroupUsers(List<string> groupList, string sqlCriteria, string sortFieldList, bool activeOnly, int pageSize, int pageNumber)
// ... additional overloads with fewer parameters

// Insert a new record
bool Insert(string contentName)
```

### Navigation

```csharp
bool OK()          // true if current row is valid
void GoNext()      // move to next record
bool NextOK()      // move to next and return true if valid
void GoFirst()     // move to first record
int GetRowCount()  // total rows in result
```

### Reading Fields

```csharp
string GetText(string fieldName)
string GetHtml(string fieldName)
int GetInteger(string fieldName)
double GetNumber(string fieldName)
bool GetBoolean(string fieldName)
DateTime GetDate(string fieldName)
string GetValue(string fieldName)       // raw value
string GetFilename(string fieldName)    // file field path
bool FieldOK(string fieldName)          // true if field exists
```

### Writing Fields

```csharp
void SetField(string fieldName, string fieldValue)
void SetField(string fieldName, int fieldValue)
void SetField(string fieldName, bool fieldValue)
void SetField(string fieldName, DateTime fieldValue)
void SetField(string fieldName, object fieldValue)
void Save()
```

### Other

```csharp
void Delete()      // delete current row
void Close()       // close the record set
void Dispose()     // IDisposable support
string GetSQL()    // returns the query used
```

### Typical CS Pattern

```csharp
using (var cs = cp.CSNew()) {
    if (cs.Open("People", $"(name={cp.Db.EncodeSQLText(searchName)})", "name")) {
        while (cs.OK()) {
            string name = cs.GetText("name");
            int id = cs.GetInteger("id");
            // process record
            cs.GoNext();
        }
    }
}
```

---

## Common Examples

### Create and save a new record

```csharp
var person = DbBaseModel.addDefault<PersonModel>(cp);
person.name = "John Doe";
person.email = "john@example.com";
person.save(cp);
```

### Query with filtering and paging

```csharp
var page2 = DbBaseModel.createList<PersonModel>(cp, "(organizationId=5)", "name", 25, 2);
```

### Raw SQL query

```csharp
using var dt = cp.Db.ExecuteQuery($"select id, name from people where email={cp.Db.EncodeSQLText(email)}");
foreach (DataRow row in dt.Rows) {
    int id = cp.Utils.EncodeInteger(row["id"]);
    string name = row["name"].ToString();
}
```

### Delete by criteria

```csharp
DbBaseModel.deleteRows<TempDataModel>(cp, $"(dateAdded<{cp.Db.EncodeSQLDate(cutoffDate)})");
```

### Working with file fields

```csharp
var addon = DbBaseModel.create<AddonModel>(cp, addonId);
// Read text file content (lazy-loaded from CDN)
string jsContent = addon.jsFilename.content;

// Update text file content
addon.jsFilename.content = "// new javascript content";
addon.save(cp);  // file is automatically saved to CDN
```
