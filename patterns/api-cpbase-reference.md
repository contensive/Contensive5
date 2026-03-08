# CPBase API Reference

> Source files: `source/CPBase/BaseClasses/CPBaseClass.cs`, `CPAddonBaseClass.cs`, `CPDocBaseClass.cs`, `CPRequestBaseClass.cs`, `CPResponseBaseClass.cs`, `CPEmailBaseClass.cs`, `CPLogBaseClass.cs`

## CPBaseClass — The `cp` Object

The `cp` object is the central API passed to every addon. It provides access to all framework services through typed sub-objects.

### Sub-Object Properties

| Property | Type | Description |
|----------|------|-------------|
| `cp.Addon` | `CPAddonBaseClass` | Execute addons, install collections |
| `cp.Cache` | `CPCacheBaseClass` | Store, get, invalidate cache objects |
| `cp.CdnFiles` | `CPFileSystemBaseClass` | Read/write CDN files (content uploads, images) |
| `cp.Content` | `CPContentBaseClass` | Content metadata, copy content, edit links |
| `cp.Db` | `CPDbBaseClass` | SQL queries, table operations |
| `cp.Doc` | `CPDocBaseClass` | Page properties, head/body asset injection |
| `cp.Email` | `CPEmailBaseClass` | Send emails to users, groups |
| `cp.Group` | `CPGroupBaseClass` | Group management, add/remove users |
| `cp.Html` | `CPHtmlBaseClass` | HTML form elements, wrappers |
| `cp.Http` | `CPHttpBaseClass` | HTTP GET/POST, CDN path prefixes |
| `cp.Image` | `CPImageBaseClass` | Image controller |
| `cp.Log` | `CPLogBaseClass` | Structured logging (NLog) |
| `cp.Mustache` | `CPMustacheBaseClass` | Mustache template rendering |
| `cp.PrivateFiles` | `CPFileSystemBaseClass` | Read/write private files (not web-accessible) |
| `cp.Request` | `CPRequestBaseClass` | Incoming request data |
| `cp.Response` | `CPResponseBaseClass` | Response output control |
| `cp.Security` | `CPSecurityBaseClass` | Encrypt/decrypt, password generation |
| `cp.Secrets` | `CPSecretsBaseClass` | AWS credentials, secret manager |
| `cp.Site` | `CPSiteBaseClass` | Site properties, error reporting, activity logging |
| `cp.TempFiles` | `CPFileSystemBaseClass` | Temporary file storage (auto-deleted) |
| `cp.User` | `CPUserBaseClass` | Current user identity, auth, properties |
| `cp.Utils` | `CPUtilsBaseClass` | Type conversion, encoding utilities |
| `cp.Visit` | `CPVisitBaseClass` | Current visit session properties |
| `cp.Visitor` | `CPVisitorBaseClass` | Persistent visitor properties |
| `cp.WwwFiles` | `CPFileSystemBaseClass` | Read/write application root files |
| `cp.ServerConfig` | `ServerConfigBaseModel` | Server configuration |
| `cp.Version` | `string` | Contensive version string |

### Factory Methods

```csharp
CPCSBaseClass cp.CSNew()                          // create a new Content Set object
CPBlockBaseClass cp.BlockNew()                    // create a new Block object
CPDbBaseClass cp.DbNew(string dataSourceName)     // create Db for non-default datasource
AppConfigBaseModel cp.GetAppConfig()              // current app config
AppConfigBaseModel cp.GetAppConfig(string appName) // specific app config
List<string> cp.GetAppNameList()                  // all app names on this server
```

---

## CPAddonBaseClass (cp.Addon)

Execute addons and manage addon collections.

### Execute Addons

```csharp
// By GUID
string Execute(string addonGuid)
string Execute(string addonGuid, Dictionary<string, string> argumentKeyValuePairs)
string Execute(string addonGuid, CPUtilsBaseClass.addonExecuteContext executeContext)

// By ID
string Execute(int addonId)
string Execute(int addonId, Dictionary<string, string> argumentKeyValuePairs)
string Execute(int addonId, CPUtilsBaseClass.addonExecuteContext executeContext)

// By unique name
string ExecuteByUniqueName(string addonName)
string ExecuteByUniqueName(string addonName, Dictionary<string, string> argumentKeyValuePairs)
string ExecuteByUniqueName(string addonName, CPUtilsBaseClass.addonExecuteContext executeContext)

// As background process
void ExecuteAsProcess(string addonGuid)
void ExecuteAsProcess(string addonGuid, Dictionary<string, string> argumentKeyValuePairs)
void ExecuteAsProcess(int addonId)
void ExecuteAsProcess(int addonId, Dictionary<string, string> argumentKeyValuePairs)
void ExecuteAsProcessByUniqueName(string addonName)
void ExecuteAsProcessByUniqueName(string addonName, Dictionary<string, string> argumentKeyValuePairs)

// Dependency execution
string ExecuteDependency(string addonGuid)
```

### Addon Execute Context

```csharp
public enum addonContext {
    ContextPage = 1,
    ContextAdmin = 2,
    ContextTemplate = 3,
    ContextEmail = 4,
    ContextRemoteMethodHtml = 5,
    ContextRemoteMethodJson = 18,
    ContextSimple = 15,
    // ... and others
}
```

### Install Collections

```csharp
bool InstallCollectionFile(string tempPathFilename, ref string returnUserError)
int InstallCollectionFileAsync(string tempPathFilename)
bool InstallCollectionsFromFolder(string tempFolder, bool deleteFolderWhenDone, ref string returnUserError)
bool InstallCollectionFromLibrary(string collectionGuid, ref string returnUserError)
bool InstallCollectionFromLink(string collectionFileLink, ref string returnUserError)
bool ExportCollection(int collectionId, ref string collectionZipCdnPathFilename, ref string returnUserError)
bool ExportCollection(string collectionGuid, ref string collectionZipCdnPathFilename, ref string returnUserError)
```

### Properties

```csharp
int ID { get; }        // id of the currently executing addon
string ccGuid { get; } // guid of the currently executing addon
```

---

## CPDocBaseClass (cp.Doc)

Properties and assets for the current HTML document/page.

### Page Info

```csharp
int PageId { get; }
string PageName { get; }
int TemplateId { get; }
string Type { get; }               // DOCTYPE declaration
DateTime StartTime { get; }
string RefreshQueryString { get; }  // querystring to return to current page+addon
bool IsAdminSite { get; }
bool NoFollow { get; set; }         // set nofollow meta tag
string Body { get; set; }           // html body
```

### Head/Body Asset Injection

```csharp
void AddHeadStyle(string styleSheet)
void AddHeadStyleLink(string styleSheetLink)
void AddHeadJavascript(string code)
void AddHeadJavascriptLink(string codeLink)
void AddBodyJavascript(string code)
void AddBodyJavascriptLink(string codeLink)
void AddHeadTag(string htmlTag)
void AddMetaDescription(string metaDescription)
void AddMetaKeywordList(string metaKeywordList)
void AddOnLoadJavascript(string code)
void AddTitle(string pageTitle)
void AddBodyEnd(string html)
```

### Document Properties (key-value store)

Used to pass data between addons on the same page, or to read form/querystring values.

```csharp
// Set properties
void SetProperty(string key, string value)
void SetProperty(string key, bool value)
void SetProperty(string key, int value)
void SetProperty(string key, DateTime value)
void SetProperty(string key, double value)

// Get properties (includes form post, querystring, and doc properties)
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

bool IsProperty(string key)          // true if key exists
List<string> Properties { get; }     // all available property keys

// Refresh querystring management
void AddRefreshQueryString(string key, string value)
void AddRefreshQueryString(string key, int value)
void AddRefreshQueryString(string key, double value)
void AddRefreshQueryString(string key, bool value)
void AddRefreshQueryString(string key, DateTime value)
```

---

## CPRequestBaseClass (cp.Request)

Access incoming HTTP request data.

### Request Properties

```csharp
string Browser { get; }
bool BrowserIsMobile { get; }
string Host { get; }           // requested domain
string Link { get; }           // full requested URL
string Page { get; }           // right-most URL segment
string Path { get; }           // URL between domain and page
string PathPage { get; }       // path + page
string Protocol { get; }       // http, https
string QueryString { get; }
string Referer { get; }
string RemoteIP { get; }
bool Secure { get; }           // true if HTTPS
string Body { get; }           // request entity body
string ContentType { get; }
string Form { get; }           // full form key=value list
string FormAction { get; }     // request verb
string Language { get; }
```

### Get Request Values

```csharp
string GetText(string key)
string GetTextSafe(string key)   // HTML removed for XSS safety
int GetInteger(string key)
double GetNumber(string key)
bool GetBoolean(string key)
DateTime GetDate(string key)
string Cookie(string cookieName)
string CookieString { get; }
```

---

## CPResponseBaseClass (cp.Response)

Control the HTTP response.

```csharp
string ContentType { get; set; }
bool isOpen { get; }

void Redirect(string link)             // 302 redirect
void RedirectPermanent(string link)     // 301 redirect
void SetStatus(string status)
void SetType(string contentType)
void AddHeader(string key, string value)

// Cookies
void SetCookie(string key, string value)
void SetCookie(string key, string value, DateTime dateExpires)
void SetCookie(string key, string value, DateTime dateExpires, string domain)
void SetCookie(string key, string value, DateTime dateExpires, string domain, string path)
void SetCookie(string key, string value, DateTime dateExpires, string domain, string path, bool secure)

void Clear()
void Close()
void Flush()
```

---

## CPEmailBaseClass (cp.Email)

### Send Email

```csharp
void send(string toAddress, string fromAddress, string subject, string body, bool sendImmediately, bool bodyIsHTML, ref string adminErrorMessage)
void send(string toAddress, string fromAddress, string subject, string body, bool sendImmediately, bool bodyIsHTML)
void send(string toAddress, string fromAddress, string subject, string body, bool sendImmediately)
void send(string toAddress, string fromAddress, string subject, string body)

string fromAddressDefault { get; }    // site's default from address
bool validateEmail(string toAddress)
bool validateUserEmail(int toUserId)
```

### Send to Groups

```csharp
void sendGroup(string groupName, string fromAddress, string subject, string body, bool sendImmediately, bool bodyIsHTML, ref string adminErrorMessage)
void sendGroup(string groupName, string fromAddress, string subject, string body)
void sendGroup(int groupId, string fromAddress, string subject, string body)
void sendGroup(List<string> groupNameList, string fromAddress, string subject, string body)
void sendGroup(List<int> groupIdList, string fromAddress, string subject, string body)
// ... additional overloads with fewer parameters
```

### Send to User

```csharp
void sendUser(int toUserId, string fromAddress, string subject, string body, bool sendImmediately, bool bodyIsHTML, ref string adminErrorMessage)
void sendUser(int toUserId, string fromAddress, string subject, string body)
// ... additional overloads
```

### System Email and Password

```csharp
void sendSystem(string emailName)           // by name
void sendSystem(int emailId)                // by id
void sendSystem(string emailName, string additionalCopy, int additionalUserID)
void sendPassword(string userEmailAddress)
// ... additional overloads with error message ref and sendImmediately
```

---

## CPLogBaseClass (cp.Log)

Structured logging via NLog with AWS CloudWatch support.

```csharp
void Add(string logMessage)                          // Info level
void Add(CPLogBaseClass.LogLevel level, string logMessage)
void Trace(string logMessage)
void Debug(string logMessage)
void Info(string logMessage)
void Warn(string logMessage)
void Warn(Exception ex, string logMessage)
void Warn(Exception ex)
void Error(string logMessage)
void Error(Exception ex, string logMessage)
void Error(Exception ex)
void Fatal(string logMessage)
void Fatal(Exception ex, string logMessage)
void Fatal(Exception ex)
```

### Log Levels

```csharp
public enum LogLevel {
    Trace = 0,
    Debug = 1,
    Info = 2,
    Warn = 3,
    Error = 4,
    Fatal = 5
}
```
