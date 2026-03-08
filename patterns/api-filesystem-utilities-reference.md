# Filesystem & Utilities API Reference

> Source files: `source/CPBase/BaseClasses/CPFileSystemBaseClass.cs`, `CPHttpBaseClass.cs`, `CPUtilsBaseClass.cs`, `CPGroupBaseClass.cs`, `CPHtmlBaseClass.cs`, `CPMustacheBaseClass.cs`, `CPSecurityBaseClass.cs`, `CPSecretsBaseClass.cs`

---

## CPFileSystemBaseClass (cp.CdnFiles, cp.WwwFiles, cp.PrivateFiles, cp.TempFiles)

Four filesystem instances are available, each for a different purpose:

| Instance | Description |
|----------|-------------|
| `cp.CdnFiles` | CDN files — content uploads, images, file fields. Served via CDN URL. |
| `cp.WwwFiles` | Application root — files in the website root folder |
| `cp.PrivateFiles` | Private storage — not web-accessible |
| `cp.TempFiles` | Temporary files — auto-deleted periodically |

All share the same API:

### Read/Write

```csharp
string Read(string pathFilename)
byte[] ReadBinary(string pathFilename)
void Save(string pathFilename, string fileContent)
void SaveBinary(string pathFilename, byte[] fileContent)
void Append(string filename, string fileContent)
```

### File Operations

```csharp
void Copy(string sourcePathFilename, string destinationPathFilename)
void Copy(string sourcePathFilename, string destinationPathFilename, CPFileSystemBaseClass destinationFileSystem)
void CopyPath(string sourcePath, string destinationPath)
void CopyPath(string sourcePath, string destinationPath, CPFileSystemBaseClass destinationFileSystem)
void DeleteFile(string pathFilename)
bool FileExists(string pathFilename)
```

### Folder Operations

```csharp
void CreateFolder(string pathFolder)
string CreateUniqueFolder()
void DeleteFolder(string folderPath)
bool FolderExists(string folderName)
```

### File/Folder Listings

```csharp
List<FileDetail> FileList(string folderPath)
List<FileDetail> FileList(string folderPath, int pageSize)
List<FileDetail> FileList(string folderPath, int pageSize, int pageNumber)
FileDetail FileDetails(string pathFilename)
List<FolderDetail> FolderList(string folderPath)
```

### Upload Handling

```csharp
bool SaveUpload(string htmlFormName, ref string returnFilename)
bool SaveUpload(string htmlFormName, string folderPath, ref string returnFilename)
```

### Zip Operations

```csharp
void ZipPath(string archivePathFilename, string path)
void UnzipFile(string pathFilename)
```

### HTTP File Downloads

```csharp
void SaveHttpGet(string url, string pathFilename)
void SaveHttpPost(string url, string pathFilename, List<KeyValuePair<string, string>> requestArguments)
void SaveHttpPost(string url, string pathFilename, string entity)
```

### Remote/Local Sync

```csharp
void CopyLocalToRemote(string pathFilename)
void CopyRemoteToLocal(string pathFilename)
```

### Path Utilities

```csharp
string GetPath(string pathFilename)           // extract path portion
string GetFilename(string pathFilename)        // extract filename portion
bool IsValidPathFilename(string pathFilename)
string EncodePathFilename(string pathFilename) // fix invalid characters
string PhysicalFilePath { get; }               // local storage path
```

### Example

```csharp
// Save a report to CDN
string path = $"reports/{DateTime.Now:yyyy-MM-dd}/summary.csv";
cp.CdnFiles.Save(path, csvContent);

// Copy between filesystems
cp.TempFiles.Save("export.zip", data);
cp.TempFiles.Copy("export.zip", "exports/export.zip", cp.CdnFiles);

// List files
var files = cp.CdnFiles.FileList("uploads/images");
foreach (var file in files) {
    // file.Name, file.Size, file.DateLastModified
}
```

---

## CPHttpBaseClass (cp.Http)

HTTP request helpers and CDN path configuration.

```csharp
string Get(string url)
string Post(string url)
string Post(string url, NameValueCollection requestArguments)

string CdnFilePathPrefix { get; }          // prefix for database file fields (relative)
string CdnFilePathPrefixAbsolute { get; }  // prefix for absolute CDN links
string WebAddressProtocolDomain { get; }   // preferred protocol + domain (e.g., "https://example.com")
```

### Example

```csharp
// Build a full CDN URL for a file field
string imageUrl = $"{cp.Http.CdnFilePathPrefixAbsolute}{person.photoFilename.filename}";
```

---

## CPSecurityBaseClass (cp.Security)

Encryption and password generation.

```csharp
string GetRandomPassword()

// One-way hashing
string EncryptOneWay(string unencryptedString)
string EncryptOneWay(string unencryptedString, string salt)
bool VerifyOneWay(string unencryptedString, string encryptedString)

// Two-way AES encryption (symmetric)
string EncryptTwoWay(string unencryptedString)
string DecryptTwoWay(string encryptedString)
```

---

## CPSecretsBaseClass (cp.Secrets)

Access to credentials and secret management.

```csharp
// AWS credentials
string AwsAccessKey { get; }
string AwsSecretAccessKey { get; }

// Default datasource
string DefaultDataSourceAddress { get; }    // url:port
string DefaultDataSourceUsername { get; }
string DefaultDataSourcePassword { get; }

// Secret Manager
string GetSecret(string secretName)
void SetSecret(string secretName, string secretValue)
```

---

## CPGroupBaseClass (cp.Group)

Group management and user membership.

### Group CRUD

```csharp
void Add(string groupName)
void Add(string groupName, string groupCaption)
void Delete(string groupNameIdOrGuid)
void Delete(int groupId)
int GetId(string groupNameOrGuid)
string GetName(string groupIdOrGuid)
string GetName(int groupId)
int verifyGroup(string groupGuid, string groupName)
int verifyGroup(string groupGuid, string groupName, string groupCaption)
bool exists(string groupGuid)
bool exists(string groupGuid, out int groupId)
```

### User Membership

```csharp
// Add user to group
void AddUser(int groupId)                                         // current user
void AddUser(string groupNameIdOrGuid)                            // current user
void AddUser(string groupNameIdOrGuid, int userId)
void AddUser(string groupNameIdOrGuid, int userId, DateTime dateExpires)
void AddUser(int groupId, int userId)
void AddUser(int groupId, int userId, DateTime dateExpires)

// Remove user from group
void RemoveUser(string groupNameOrGuid)                           // current user
void RemoveUser(int groupId)                                      // current user
void RemoveUser(string groupNameOrGuid, int userId)
void RemoveUser(int groupId, int userId)
```

---

## CPHtmlBaseClass (cp.Html)

HTML element generators and form input helpers.

### HTML Elements

```csharp
// Each has overloads: (innerHtml), (innerHtml, htmlName), (innerHtml, htmlName, htmlClass), (innerHtml, htmlName, htmlClass, htmlId)
string div(string innerHtml, ...)
string p(string innerHtml, ...)
string h1(string innerHtml, ...) through h6(string innerHtml, ...)
string ul(string innerHtml, ...)
string ol(string innerHtml, ...)
string li(string innerHtml, ...)
string Form(string innerHtml, ...)    // also accepts actionQueryString, method
string adminHint(string innerHtml)
```

### Form Inputs

```csharp
// Text input
string InputText(string htmlName)
string InputText(string htmlName, string htmlValue)
string InputText(string htmlName, string htmlValue, int maxLength)
string InputText(string htmlName, string htmlValue, int maxLength, string htmlClass)
string InputText(string htmlName, string htmlValue, int maxLength, string htmlClass, string htmlId)

// Expandable text area
string InputTextExpandable(string htmlName)
string InputTextExpandable(string htmlName, string htmlValue)
string InputTextExpandable(string htmlName, string htmlValue, int rows)
string InputTextExpandable(string htmlName, string htmlValue, int rows, string styleWidth)
string InputTextExpandable(string htmlName, string htmlValue, int rows, string styleWidth, bool isPassword)
// ... additional overloads with htmlClass, htmlId

// WYSIWYG editor
string InputWysiwyg(string htmlName, string htmlValue, EditorUserScope userScope, EditorContentScope contentScope)
// ... additional overloads with height, width, htmlClass, htmlId

// Date input
string InputDate(string htmlName)
string InputDate(string htmlName, DateTime htmlValue)
// ... additional overloads

// File upload
string InputFile(string htmlName)
string InputFile(string htmlName, string htmlClass)
string InputFile(string htmlName, string htmlClass, string htmlId)

// Checkbox
string CheckBox(string htmlName)
string CheckBox(string htmlName, bool htmlValue)
string CheckBox(string htmlName, bool htmlValue, string htmlClass)
string CheckBox(string htmlName, bool htmlValue, string htmlClass, string htmlId)

// Radio button
string RadioBox(string htmlName, string htmlValue, string currentValue)
string RadioBox(string htmlName, string htmlValue, string currentValue, string htmlClass)
string RadioBox(string htmlName, string htmlValue, string currentValue, string htmlClass, string htmlId)

// Hidden input
string Hidden(string htmlName, string htmlValue)
string Hidden(string htmlName, string htmlValue, string htmlClass)
string Hidden(string htmlName, string htmlValue, string htmlClass, string htmlId)

// Button
string Button(string htmlName)
string Button(string htmlName, string htmlValue)
string Button(string htmlName, string htmlValue, string htmlClass)
string Button(string htmlName, string htmlValue, string htmlClass, string htmlId)

// Select dropdowns
string SelectContent(string htmlName, string htmlValue, string contentName)
string SelectContent(string htmlName, string htmlValue, string contentName, string sqlCriteria)
string SelectContent(string htmlName, string htmlValue, string contentName, string sqlCriteria, string noneCaption)
// ... additional overloads with htmlClass, htmlId

string SelectList(string htmlName, string htmlValue, string optionList)
string SelectList(string htmlName, string htmlValue, string optionList, string noneCaption)
// ... additional overloads with htmlClass, htmlId

string SelectUser(string htmlName, int htmlValue, int groupId)
string SelectUser(string htmlName, int htmlValue, int groupId, string noneCaption)
// ... additional overloads with htmlClass, htmlId

// Many-to-many checklist
string CheckList(string htmlName, string primaryContentName, int primaryRecordId, string secondaryContentName, string rulesContentName, string rulesPrimaryFieldName, string rulesSecondaryFieldName)
// ... additional overloads with sqlCriteria, captionFieldName, isReadOnly, htmlClass, htmlId

void ProcessCheckList(string htmlName, string primaryContentName, int primaryRecordId, string secondaryContentName, string rulesContentName, string rulesPrimaryFieldName, string rulesSecondaryFieldName)
void ProcessInputFile(string htmlName)
void ProcessInputFile(string htmlName, string virtualFilePath)
```

### Editor Enums

```csharp
public enum EditorUserScope {
    Developer = 1,
    Administrator = 2,
    ContentManager = 3,
    PublicUser = 4,
    CurrentUser = 5
}

public enum EditorContentScope {
    Page = 1,
    Email = 2,
    PageTemplate = 3,
    EmailTemplate = 4
}
```

### Utility

```csharp
string Indent(string sourceHtml)
string Indent(string sourceHtml, int tabCnt)
void AddEvent(string htmlId, string domEvent, string javaScript)
```

---

## CPMustacheBaseClass (cp.Mustache)

Mustache template rendering.

```csharp
string Render(string template, object dataSet)
```

### Example

```csharp
var data = new {
    name = "John",
    items = new[] {
        new { title = "Item 1" },
        new { title = "Item 2" }
    }
};
string html = cp.Mustache.Render("Hello {{name}}! {{#items}}<li>{{title}}</li>{{/items}}", data);
```

---

## CPUtilsBaseClass (cp.Utils)

Type conversion, encoding, and general utilities.

### Type Conversion

```csharp
bool EncodeBoolean(object expression)
DateTime EncodeDate(object expression)
int EncodeInteger(object expression)
double EncodeNumber(object expression)
string EncodeText(object expression)
string EncodeTextSafe(object expression)    // XSS-safe text input
```

### HTML/URL Encoding

```csharp
string EncodeHTML(string source)
string DecodeHTML(string source)
string EncodeUrl(string source)
string DecodeUrl(string url)
string EncodeUrlForHrefSrc(string source)
string ConvertHTML2Text(string source)
string ConvertText2HTML(string source)
string EncodeContentForWeb(string source)
string EncodeContentForWeb(string source, string contextContentName)
string EncodeContentForWeb(string source, string contextContentName, int contextRecordID)
string DecodeHtmlFromWysiwygEditor(string source)
string EncodeHtmlForWysiwygEditor(string source)
string EncodeAppRootPath(string link)
```

### URL Manipulation

```csharp
string ModifyLinkQueryString(string url, string key, string value)
string ModifyLinkQueryString(string url, string key, string value, bool addIfMissing)
string ModifyLinkQueryString(string url, string key, int value)
string ModifyLinkQueryString(string url, string key, bool value)
string ModifyLinkQueryString(string url, string key, DateTime value)
// ... additional overloads

string ModifyQueryString(string sourceQueryString, string key, string value)
string ModifyQueryString(string sourceQueryString, string key, string value, bool addIfMissing)
// ... additional overloads

void SeparateURL(string sourceUrl, ref string protocol, ref string domain, ref string path, ref string page, ref string queryString)
void SeparateURL(string sourceUrl, ref string protocol, ref string domain, ref string port, ref string path, ref string page, ref string queryString)
```

### String Utilities

```csharp
string CreateGuid()
bool isGuid(string guid)
string GetFilename(string pathFilename)
int GetListIndex(string itemToFind, string commaDelimitedListOfItems)
bool IsInDelimitedString(string delimitedString, string itemToFind, string delimiter)
string GetArgument(string key, string keyValueDelimitedString)
string GetArgument(string key, string keyValueDelimitedString, string defaultValue)
string GetArgument(string key, string keyValueDelimitedString, string defaultValue, string delimiter)
string DecodeResponseVariable(string source)
string EncodeRequestVariable(string source)
```

### Random Generation

```csharp
int GetRandomInteger()
int GetRandomInteger(int maxValue)
double GetRandomDouble()
string GetRandomString(int length)
```

### Authentication

```csharp
string GetAuthenticationToken(int userId)
string GetAuthenticationToken(int userId, DateTime expiration)
```

### Other

```csharp
void ExportCsv(string sql, string exportName, string filename)
void IISReset()
bool versionIsOlder(string versionStringToTest, string versionStringToTestAgainst)
DateTime GetDateTimeMockable()
void AppendLog(string logText)
```
