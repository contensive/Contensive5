# Site, User & Cache API Reference

> Source files: `source/CPBase/BaseClasses/CPSiteBaseClass.cs`, `CPUserBaseClass.cs`, `CPCacheBaseClass.cs`, `CPContentBaseClass.cs`, `CPVisitBaseClass.cs`, `CPVisitorBaseClass.cs`

---

## CPSiteBaseClass (cp.Site)

Site-level properties, error reporting, and activity logging.

### Site Info

```csharp
string Name { get; }            // application name
string DomainPrimary { get; }   // primary domain
string Domain { get; }          // current request domain
string DomainList { get; }      // all supported domains
string PageDefault { get; }     // default script page for websites
int htmlPlatformVersion { get; } // html platform version
```

### Site Properties (persistent key-value store)

```csharp
// Set
void SetProperty(string key, string value)
void SetProperty(string key, bool value)
void SetProperty(string key, DateTime value)
void SetProperty(string key, int value)
void SetProperty(string key, double value)
void ClearProperty(string key)

// Get
string GetText(string key)
string GetText(string key, string defaultValue)
bool GetBoolean(string key)
bool GetBoolean(string key, bool defaultValue)
int GetInteger(string key)
int GetInteger(string key, int defaultValue)
double GetNumber(string key)
double GetNumber(string key, double defaultValue)
DateTime GetDate(string key)
DateTime GetDate(string key, DateTime defaultValue)
```

### Error Reporting

```csharp
void ErrorReport(string message)
void ErrorReport(Exception ex)
void ErrorReport(Exception ex, string message)
void LogAlarm(string cause)
void SetSiteWarning(string name, string description)
void SetSiteWarning(string name, string description, bool addRemove)
void TestPoint(string message)   // only logs when debugging is enabled
```

### Activity Logging

```csharp
int AddActivity(string subject)
int AddActivity(string subject, string detailsText)
int AddActivity(string subject, string detailsText, int activityUserId)
int AddActivity(string subject, string detailsText, int activityUserId, int typeId)
int AddActivity(string subject, string detailsText, int activityUserId, int typeId, DateTime dateScheduled, int duration, int scheduledStaffId)
```

### Link Aliases and Events

```csharp
void AddLinkAlias(string linkAlias, int pageId)
void AddLinkAlias(string linkAlias, int pageId, string queryStringSuffix)
string ThrowEventByName(string eventName)
string ThrowEventByGuid(string eventGuid)
```

---

## CPUserBaseClass (cp.User)

Current user identity, authentication, role checks, and per-user properties.

### Identity

```csharp
int Id { get; }                // user id (creates guest record if needed)
int IdInSession { get; }       // user id without creating a guest
string Name { get; }           // user's name (if authenticated)
string Email { get; }          // user's email (if authenticated)
string Username { get; }       // user's username (if authenticated)
string Language { get; }       // browser language
int LanguageID { get; }
int OrganizationID { get; }
```

### Authentication State

```csharp
bool IsAuthenticated { get; }   // user has logged in
bool IsRecognized { get; }      // user is recognized (cookie)
bool IsGuest { get; }           // not authenticated and not recognized
bool IsNew { get; }             // not recognized and not authenticated
```

### Role Checks

```csharp
bool IsAdmin { get; }
bool IsDeveloper { get; }
bool IsContentManager()
bool IsContentManager(string contentName)
bool IsInGroup(string groupName)
bool IsInGroup(string groupName, int userId)
bool IsInGroupList(string groupIdCommaList)
bool IsInGroupList(string groupIdCommaList, int checkUserID)
```

### Editing State

```csharp
bool IsEditing()
bool IsEditing(string contentName)
bool IsAdvancedEditing()
bool IsAdvancedEditing(string contentName)
bool IsQuickEditing(string contentName)
bool IsTemplateEditing { get; }
bool IsPageBuilderEditing { get; }
bool IsDebugging { get; }
```

### Authentication Actions

```csharp
bool Login(string username, string password)
bool Login(string username, string password, bool setAutoLogin)
bool LoginByID(int recordId)
bool LoginByID(int recordId, bool setAutoLogin)
bool LoginIsOK(string username, string password)
bool IsNewLoginOK(string username, string password)
bool IsNewLoginOK(int userId, string username, string password, ref string errorMessage)
void Logout()
bool Recognize(int userID)
int GetIdByLogin(string username, string password)
void Track()
```

### User Properties (persistent key-value store)

```csharp
// Set
void SetProperty(string key, string value)
void SetProperty(string key, string value, int userId)
void SetProperty(string key, int value)
void SetProperty(string key, int value, int userId)
void SetProperty(string key, double value)
void SetProperty(string key, double value, int userId)
void SetProperty(string key, bool value)
void SetProperty(string key, bool value, int userId)
void SetProperty(string key, DateTime value)
void SetProperty(string key, DateTime value, int userId)
void ClearProperty(string key)

// Get
string GetText(string key)
string GetText(string key, string defaultValue)
bool GetBoolean(string key)
bool GetBoolean(string key, bool defaultValue)
int GetInteger(string key)
int GetInteger(string key, int defaultValue)
double GetNumber(string key)
double GetNumber(string key, double defaultValue)
DateTime GetDate(string key)
DateTime GetDate(string key, DateTime defaultValue)
T GetObject<T>(string key)
```

### Password Management

```csharp
bool SetPassword(string password)
bool SetPassword(string password, ref string userErrorMessage)
bool SetPassword(string password, int userId)
bool SetPassword(string password, int userId, ref string userErrorMessage)
```

---

## CPCacheBaseClass (cp.Cache)

In-memory and distributed caching with dependency tracking.

### Store

```csharp
void Store(string key, object value)
void Store(string key, object value, DateTime invalidationDate)
void Store(string key, object value, string dependentKey)
void Store(string key, object value, DateTime invalidationDate, string dependentKey)
void Store(string key, object value, List<string> dependentKeyList)
void Store(string key, object value, DateTime invalidationDate, List<string> dependentKeyList)
void StorePtr(string keyPtr, string key)    // pointer to another cache entry
```

### Get

```csharp
object GetObject(string key)
T GetObject<T>(string key)
string GetText(string key)
int GetInteger(string key)
double GetNumber(string key)
DateTime GetDate(string key)
bool GetBoolean(string key)
```

### Invalidate

```csharp
void Invalidate(string key)
void InvalidateAll()
void ClearAll()
void Clear(List<string> keyList)
void InvalidateTagList(List<string> keyList)
void InvalidateContentRecord(string contentName, int recordId)
void InvalidateTableRecord(string tableName, int recordId)
void InvalidateTable(string tableName)
void invalidateTableDependencyKey(string tableName)
```

### Key Creation

```csharp
string CreateKey(string uniqueName)
string CreateKey(string uniqueName, string objectUniqueIdentifier)
string CreateRecordKey(int recordId, string tableName)
string CreateRecordKey(int recordId, string tableName, string dataSourceName)
string CreateTableDependencyKey(string tableName)
string CreateTableDependencyKey(string tableName, string dataSourceName)
string CreatePtrKeyforDbRecordGuid(string guid, string tableName)
string CreatePtrKeyforDbRecordGuid(string guid, string tableName, string dataSourceName)
string CreatePtrKeyforDbRecordUniqueName(string name, string tableName)
string CreatePtrKeyforDbRecordUniqueName(string name, string tableName, string dataSourceName)
```

### Cache Pattern Example

```csharp
// Cache with table dependency — auto-invalidates when any record in the table changes
string cacheKey = cp.Cache.CreateKey("myAddon-summary");
string tableDepKey = cp.Cache.CreateTableDependencyKey("myTable");
var cached = cp.Cache.GetObject<MyData>(cacheKey);
if (cached == null) {
    cached = buildExpensiveData(cp);
    cp.Cache.Store(cacheKey, cached, tableDepKey);
}
```

---

## CPContentBaseClass (cp.Content)

Content metadata, copy content, edit links, and record helpers.

### Copy Content

```csharp
string GetCopy(string copyName)
string GetCopy(string copyName, string defaultContent)
string GetCopy(string copyName, string defaultContent, int personalizationPeopleId)
void SetCopy(string copyName, string content)
```

### Edit Links (for inline editing UI)

```csharp
// By content name
string GetEditLink(string contentName, int recordId)
string GetEditLink(string contentName, int recordId, string customCaption)
string GetEditLink(string contentName, string recordGuid)
string GetEditLink(string contentName, string recordGuid, string customCaption)

// By content id
string GetEditLink(int contentId, int recordId)
string GetEditLink(int contentId, int recordId, string customCaption)
string GetEditLink(int contentId, string recordGuid)
string GetEditLink(int contentId, string recordGuid, string customCaption)

// Legacy overloads
string GetEditLink(string contentName, string recordId, bool allowCut, string recordLabel, bool isEditing)
string GetEditLink(string contentName, string recordId, bool allowCut, string recordLabel, bool isEditing, string customCaption)

// Edit URL (without icon)
string GetEditUrl(string contentName, int recordId)
string GetEditUrl(string contentName, string recordGuid)
string GetEditUrl(int contentId, int recordId)
string GetEditUrl(int contentId, string recordGuid)

// Edit wrapper (surrounds content with edit controls)
string GetEditWrapper(string innerHtml)
string GetEditWrapper(string innerHtml, string contentName, int recordId)
string GetEditWrapper(string innerHtml, string contentName, int recordId, string editCaption)
string GetEditWrapper(string innerHtml, string contentName, string recordGuid)
string GetEditWrapper(string innerHtml, int contentId, int recordId)
// ... additional overloads
```

### Add Links

```csharp
string GetAddLink(string contentName)
string GetAddLink(string contentName, string presetNameValueList)
string GetAddLink(int contentId)
string GetAddLink(int contentId, string presetNameValueList)
// ... additional overloads
```

### Record Helpers

```csharp
int AddRecord(string contentName)
int AddRecord(string contentName, string recordName)
int GetRecordID(string contentName, string recordName)
string GetRecordName(string contentName, int recordId)
string GetRecordName(string contentName, string recordGuid)
string GetRecordGuid(string contentName, int recordId)
void Delete(string contentName, string sqlCriteria)
```

### Content Metadata

```csharp
int GetID(string contentName)
string GetName(int contentId)
string GetTable(string contentName)
string GetDataSource(string contentName)
string GetContentControlCriteria(string contentName)
bool IsField(string contentName, string fieldName)
bool IsLocked(string contentName, string recordId)
bool IsChildContent(string childContentId, string parentContentId)
string GetListLink(string contentName)
int GetTableID(string tableName)
```

### Content Definition Management

```csharp
int AddContent(string contentName)
int AddContent(string contentName, string sqlTableName)
int AddContent(string contentName, string sqlTableName, string dataSource)
int AddContentField(string contentName, string fieldName, int fieldType)
int AddContentField(string contentName, string fieldName, FieldTypeIdEnum fieldTypeId)
void DeleteContent(string contentName)
```

### Page Links

```csharp
string GetPageLink(int pageId)
string GetPageLink(int pageId, string queryStringSuffix)
string GetLinkAliasByPageID(int pageId, string queryStringSuffix, string defaultLink)
```

### FieldTypeIdEnum

```csharp
public enum FieldTypeIdEnum {
    Integer = 1,
    Text = 2,
    LongText = 3,
    Boolean = 4,
    Date = 5,
    File = 6,
    Lookup = 7,
    Redirect = 8,
    Currency = 9,
    FileText = 10,
    FileImage = 11,
    Float = 12,
    AutoIdIncrement = 13,
    ManyToMany = 14,
    MemberSelect = 15,
    FileCSS = 16,
    FileXML = 17,
    FileJavaScript = 18,
    Link = 19,
    ResourceLink = 20,
    HTML = 21,
    FileHTML = 22,
    HTMLCode = 23,
    FileHTMLCode = 24
}
```

---

## CPVisitBaseClass (cp.Visit)

Session-level properties for the current visit (one browser session).

### Visit Info

```csharp
int Id { get; }
string Name { get; }
DateTime StartTime { get; }
int StartDateValue { get; }
DateTime LastTime { get; }
int Pages { get; }               // number of page hits
int LoginAttempts { get; }
string Referer { get; }
bool CookieSupport { get; }
```

### Visit Properties

```csharp
// Set
void SetProperty(string key, string value)
void SetProperty(string key, string value, int targetVisitId)
void SetProperty(string key, int value)
void SetProperty(string key, double value)
void SetProperty(string key, DateTime value)
void SetProperty(string key, bool value)
void ClearProperty(string key)

// Get
string GetText(string key)
string GetText(string key, string defaultValue)
bool GetBoolean(string key)
bool GetBoolean(string key, bool defaultValue)
int GetInteger(string key)
int GetInteger(string key, int defaultValue)
double GetNumber(string key)
double GetNumber(string key, double defaultValue)
DateTime GetDate(string key)
DateTime GetDate(string key, DateTime defaultValue)
T GetObject<T>(string key)
```

---

## CPVisitorBaseClass (cp.Visitor)

Persistent visitor properties (across visits, tied to browser cookie).

### Visitor Info

```csharp
int Id { get; }
bool IsNew { get; }
int UserId { get; }     // last authenticated user for this visitor
```

### Visitor Properties

```csharp
// Set
void SetProperty(string key, string value)
void SetProperty(string key, string value, int targetVisitorId)
void SetProperty(string key, bool value)
void SetProperty(string key, int value)
void SetProperty(string key, double value)
void SetProperty(string key, DateTime value)
void ClearProperty(string key)

// Get
string GetText(string key)
string GetText(string key, string defaultValue)
bool GetBoolean(string key)
bool GetBoolean(string key, bool defaultValue)
int GetInteger(string key)
int GetInteger(string key, int defaultValue)
double GetNumber(string key)
double GetNumber(string key, double defaultValue)
DateTime GetDate(string key)
DateTime GetDate(string key, DateTime defaultValue)
T GetObject<T>(string key)
```

---

## Scope Comparison

| Scope | Object | Lifetime | Use For |
|-------|--------|----------|---------|
| **Site** | `cp.Site` | Permanent | App-wide settings, feature flags |
| **Visitor** | `cp.Visitor` | Persistent (cookie) | Cross-session preferences |
| **Visit** | `cp.Visit` | Browser session | Session-specific state, filters |
| **User** | `cp.User` | Per-user record | User preferences, profile data |
| **Doc** | `cp.Doc` | Single page request | Pass data between addons on same page |
