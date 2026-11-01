# Plan: Add PrivateFile Field Type and FileController.getFileUrl()

## Overview

Add a new field type `PrivateFile` that works like `File` but stores files in `privateFiles` instead of `cdnFiles`. Add a `getFileUrl()` method to `FileController` that returns the public URL for a file -- for cdnFiles this is the CDN endpoint, for privateFiles this is an AWS S3 pre-signed URL.

---

## 1. Add `PrivateFile` to `FieldTypeIdEnum`

**File:** `source/CPBase/BaseClasses/CPContentBaseClass.cs`

- Add `PrivateFile = 25` to the `FieldTypeIdEnum` enum (next available ID after `FileHTMLCode = 24`)
- Add `PrivateFile = 25` to the deprecated `fileTypeIdEnum` for backward compatibility
- Add XML doc comment: uploaded file stored in privateFiles, path stored as varchar(255)

**File:** `source/Processor/aoBase51.xml` (or relevant collection XML)

- Add the new field type to `ccfieldtypes` table data so the database registry knows about it:
  ```xml
  <Row><Field Name="name">PrivateFile</Field><Field Name="id">25</Field><Field Name="active">1</Field></Row>
  ```

---

## 2. Add `getFileUrl()` to `FileController`

**File:** `source/Processor/Controllers/FileController.cs`

Add a new public method:

```csharp
/// <summary>
/// Returns the public URL to access a file.
/// For cdnFiles: returns cdnFileUrl + pathFilename (the standard CDN endpoint).
/// For privateFiles: returns an AWS S3 pre-signed URL with a time-limited expiration.
/// For local-only filesystems: returns the local path (no remote URL available).
/// </summary>
public string getFileUrl(string pathFilename, int expirationMinutes = 60)
```

**Implementation logic:**

1. If the file system is local-only (`isLocal == true`), return `rootLocalPath + pathFilename` (or empty string / throw -- needs design decision, see Open Questions).
2. If remote (S3-backed):
   - **cdnFiles case:** Return `core.appConfig.cdnFileUrl + pathFilename` -- these files are publicly accessible via CDN.
   - **privateFiles case:** Generate an AWS S3 pre-signed URL using `GetPreSignedUrlRequest` from the AWS SDK:
     ```csharp
     var request = new GetPreSignedUrlRequest {
         BucketName = core.serverConfig.awsBucketName,
         Key = remotePathPrefix + pathFilename.ToLowerInvariant(),
         Expires = DateTime.UtcNow.AddMinutes(expirationMinutes)
     };
     return s3Client.GetPreSignedURL(request);
     ```

**Identifying which file system this is:** FileController doesn't currently know whether it's operating as cdnFiles, privateFiles, wwwFiles, or tempFiles. Options:
- **Option A (Recommended):** Add an enum parameter or property (e.g., `FileSystemTypeEnum { Cdn, Private, Www, Temp }`) set during construction via `CoreController`. This is the cleanest approach.
- **Option B:** Pass a `bool isPrivate` parameter to `getFileUrl()` and let the caller decide. Less clean but avoids changing the constructor.
- **Option C:** Compare `remotePathPrefix` against known app config paths to infer the type. Fragile.

With Option A, modify the `FileController` constructor to accept the file system type, and update the four instantiation sites in `CoreController` (cdnFiles, privateFiles, wwwFiles, tempFiles).

---

## 3. Add `GetFileUrl()` to `CPFileSystemBaseClass` (public API)

**File:** `source/CPBase/BaseClasses/CPFileSystemBaseClass.cs`

Add abstract method:
```csharp
/// <summary>
/// Returns a URL to publicly access a file. For CDN files this is the CDN endpoint.
/// For private files this is a time-limited signed URL.
/// </summary>
public abstract string GetFileUrl(string pathFilename);
public abstract string GetFileUrl(string pathFilename, int expirationMinutes);
```

**File:** `source/Processor/Views/CPFileSystemClass.cs` (the implementation of `CPFileSystemBaseClass`)

Implement by delegating to `FileController.getFileUrl()`.

---

## 4. Handle `PrivateFile` in Content Save/Load (CsModel)

**File:** `source/Processor/Models/Domain/CsModel.cs`

In `setFormInput()` (around line 1827), the existing `case File: case FileImage:` block uploads to `core.cdnFiles`. Add a new case:

```csharp
case CPContentBaseClass.FieldTypeIdEnum.PrivateFile: {
    // Same logic as File, but upload to core.privateFiles instead of core.cdnFiles
    if (!core.docProperties.containsKey(LocalRequestName)) { return; }
    var docProperty = core.docProperties.getProperty(LocalRequestName);
    string filename = docProperty.value;
    filename = FileController.encodeDosPathFilename(filename);
    filename = FileController.encodeUnixPathFilename(filename);
    string unixPathFilename = getFilename(fieldName, filename);
    string dosPathFilename = FileController.convertToDosSlash(unixPathFilename);
    string dosPath = FileController.getPath(dosPathFilename);
    var WindowsTempFiles = new FileController(core, System.IO.Path.GetTempPath());
    WindowsTempFiles.copyFile(docProperty.windowsTempfilename, dosPathFilename, core.privateFiles);
    core.privateFiles.upload(fieldName, dosPath, ref filename);
    set(fieldName, unixPathFilename);
    return;
}
```

Find all other `case File:` / `case FileImage:` switch locations (198 occurrences across 15 files) and add `case PrivateFile:` where appropriate. Key files:

| File | What it does | Action needed |
|------|-------------|---------------|
| `CsModel.cs` | `setFormInput` - upload handling | Route to `privateFiles` |
| `CsModel.cs` | `getText`, `getFilename` methods | Include PrivateFile in file-path cases |
| `EditRecordModel.cs` | Admin edit save/load | Add PrivateFile case alongside File |
| `EditorRowClass.cs` | Admin edit form rendering | Render file upload UI for PrivateFile |
| `DbController.cs` | Schema/column type mapping | Map PrivateFile to varchar(255) |
| `MetadataController.cs` | Field metadata processing | Include in file-type checks |
| `HtmlController.cs` | HTML rendering of file links | Use `getFileUrl()` for PrivateFile URLs |
| `XmlController.cs` | Collection XML import/export | Map "PrivateFile" string to enum value |
| `FileController.cs` | File type checks | Add to `isFileType()` or similar helpers |
| `ListGridController.cs` | Admin list view | Display PrivateFile fields |
| `ListView.cs` / `ListViewAdvancedSearch.cs` | List/search views | Include in file type handling |
| `ExportAsciiController.cs` | CSV export | Include in file type handling |
| `ExportDataRecordController.cs` | Data export | Include in file type handling |
| `ContentFieldMetadataModel.cs` | Field metadata model | Include in file type checks |
| `EditModalViewModel_Field.cs` | Edit modal rendering | Include in file type rendering |

---

## 5. Add `FieldTypePrivateFile` model class

**File:** `source/Models/Models/Db/DbBaseModel.cs`

Add a new class similar to `FieldTypeFile`:

```csharp
public class FieldTypePrivateFile {
    public string filename { get; set; } = null;
    public string tempFileCopySource { get; set; } = null;
    public string privateFileCopySource { get; set; } = null;
    public string cdnFileCopySource { get; set; } = null;
    public string wwwFileCopySource { get; set; } = null;
    public string uploadRequestName { get; set; } = null;
    [NonSerialized] public CPBaseClass cpInternal = null;
}
```

This mirrors `FieldTypeFile` but the save logic in `DbBaseModel.save()` will route to `privateFiles` instead of `cdnFiles`.

---

## 6. Update DbBaseModel Save Logic

**File:** `source/Models/Models/Db/DbBaseModel.cs`

In the `save()` method, find where `FieldTypeFile` properties are processed (copy source files, handle uploads). Add equivalent handling for `FieldTypePrivateFile` that targets `cp.PrivateFiles` instead of `cp.CdnFiles`.

---

## 7. Admin UI Changes

**File:** `source/Processor/Addons/AdminSite/Views/EditorRowClass.cs`

The admin editor renders different UI for different field types. For `PrivateFile`:
- Render the same file upload control as `File`
- Show a download link using `getFileUrl()` (pre-signed URL) instead of the CDN URL
- Add a visual indicator that the file is privately stored (e.g., a lock icon or "Private" label)

**File:** `source/Processor/Addons/AdminSite/Controllers/ListGridController.cs`

In the list view, PrivateFile fields should display similarly to File fields but link via `getFileUrl()`.

---

## 8. Collection XML Import/Export

**File:** `source/Processor/Controllers/XmlController.cs`

Update the field type string-to-enum mapping to include `"PrivateFile" -> FieldTypeIdEnum.PrivateFile` so collection XML can declare fields with `FieldType="PrivateFile"`.

---

## 9. NuGet Dependency Check

The AWS SDK `GetPreSignedUrlRequest` is in `AWSSDK.S3`. Verify this is already referenced in the Processor project (it should be since `AmazonS3Client` is already used in `FileController.cs`). No new NuGet packages should be needed.

---

## Open Questions

1. **Local file system URL for privateFiles:** When `isLocalFileSystem = true`, there is no S3 to generate a pre-signed URL from. Options:
   - Return a Contensive route (e.g., `/private-file-download?path=...`) that streams the file with authentication checks
   - Return empty string and document that private file URLs require S3
   - Serve via a controller endpoint in all cases (local and remote) for consistency

2. **Expiration default:** 60 minutes is suggested. Should this be configurable per-app in `appConfig`?

3. **Authentication on the download route:** If using a Contensive route for local mode, should it check that the current user is authenticated/authorized before serving the file?

4. **PrivateFileImage:** Should there also be a `PrivateFileImage = 26` for image-specific private files (with thumbnail generation, image validation, etc.)? Or is a single `PrivateFile` sufficient for now?

---

## Implementation Order

1. `FieldTypeIdEnum` enum + deprecated enum (CPBase)
2. `CPFileSystemBaseClass.GetFileUrl()` abstract method (CPBase)
3. `FileController.getFileUrl()` implementation with S3 pre-signed URLs (Processor)
4. `CPFileSystemClass` implementation of `GetFileUrl()` (Processor)
5. `FieldTypePrivateFile` model class (Models)
6. `XmlController` field type mapping (Processor)
7. `DbController` schema mapping (Processor)
8. `CsModel` save/load handling (Processor)
9. `DbBaseModel` save logic (Models)
10. Admin UI - editor, list views (Processor)
11. Collection XML field type registration (aoBase51.xml)
12. Update all remaining switch/case sites (15 files, ~198 occurrences to audit)
