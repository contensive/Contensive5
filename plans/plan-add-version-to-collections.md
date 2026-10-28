# Plan: Add `version` field to Addon Collection system

## Context

The addon manager determines upgrade availability by comparing `DateTime` values (`ModifiedDate` from the installed DB record vs `lastmodifieddate` from the library XML). This is unreliable — when a collection version bumps from `26.7.23.39289` to `26.7.23.39290`, the upgrade may not be detected because the dates don't reflect the version change.

Collections already have a version string (matching their primary DLL's assembly version, e.g. `26.7.23.39290`), but this version is never stored in the database.

This plan adds a `version` field to the collection record in the Contensive5 core. The version is read from a new `version` attribute on the `<Collection>` root element in the collection XML file (set by each project's build script). As a fallback for collections that don't yet have the attribute, the version is determined from the onInstall addon's DLL assembly version. On export, the version is written back to the collection XML from the DB record.

## All changes are in `c:\git\contensive5`

---

### Step 1: Add `version` property to `AddonCollectionModel`

**File:** `source/Models/Models/Db/AddonCollectionModel.cs`

Add after the `oninstalladdonid` property (line 53):

```csharp
/// <summary>
/// version string from the collection XML or primary DLL assembly version (e.g. "26.7.23.39290")
/// </summary>
public string version { get; set; }
```

The framework auto-maps model properties to DB columns via reflection. The column will be created by the CDef sync process.

---

### Step 2: Add `GetAssemblyVersion` and `ContainsType` to `AssemblyMetadataHelper`

**File:** `source/Processor/Controllers/Addon/AssemblyMetadataHelper.cs`

Add two new methods inside the existing `#if NET` block (after `GetAssemblyReferences`, before the closing `}`):

```csharp
/// <summary>
/// Read the assembly version from a DLL without loading it.
/// Returns the version as a string (e.g. "26.7.23.39290") or empty string on error.
/// </summary>
public static string GetAssemblyVersion(string dllPath) {
    try {
        using var stream = File.OpenRead(dllPath);
        using var peReader = new PEReader(stream);
        if (!peReader.HasMetadata) { return ""; }
        var metadataReader = peReader.GetMetadataReader();
        var version = metadataReader.GetAssemblyDefinition().Version;
        return version.ToString();
    } catch {
        // -- file is not a valid PE/managed assembly, or I/O error
    }
    return "";
}

/// <summary>
/// Check if a DLL contains a type with the given full name (e.g. "Contensive.Addons.MyClass")
/// without loading the assembly. Returns true if the type is found.
/// </summary>
public static bool ContainsType(string dllPath, string fullTypeName) {
    try {
        using var stream = File.OpenRead(dllPath);
        using var peReader = new PEReader(stream);
        if (!peReader.HasMetadata) { return false; }
        var metadataReader = peReader.GetMetadataReader();
        foreach (var typeDefHandle in metadataReader.TypeDefinitions) {
            var typeDef = metadataReader.GetTypeDefinition(typeDefHandle);
            string ns = metadataReader.GetString(typeDef.Namespace);
            string name = metadataReader.GetString(typeDef.Name);
            string fullName = string.IsNullOrEmpty(ns) ? name : $"{ns}.{name}";
            if (string.Equals(fullName, fullTypeName, StringComparison.OrdinalIgnoreCase)) {
                return true;
            }
        }
    } catch {
        // -- file is not a valid PE/managed assembly, or I/O error
    }
    return false;
}
```

Both follow the same PE-metadata pattern as existing methods. Never throw.

---

### Step 3: Read version during install and set on collection record

**File:** `source/Processor/Controllers/Collection/Install/CollectionInstallController.cs`

**A) Read the `version` attribute from the collection XML root element.** In the block around lines 260-280 where other root attributes are parsed (name, system, onInstallAddonGuid, updatable, blockNavigatorNode), add:

```csharp
string collectionVersion = XmlController.getXMLAttribute(core, Doc.DocumentElement, "version", "");
```

This follows the exact same pattern as lines 260, 269, 270, 280, 281.

**B) Add a private static fallback method** that finds the primary DLL's version when the XML doesn't include a version attribute. The primary DLL is determined by:

1. If the collection has an `onInstallAddonGuid`, find the `<Addon>` node matching that guid, get its `<DotNetClass>`, and find the DLL containing that class.
2. If no onInstall addon, find the first `<Addon>` node with a `<DotNetClass>` and find the DLL containing that class.
3. If the DLL can't be found or the version can't be read, return empty string.

```csharp
/// <summary>
/// Determine the collection version from the primary DLL's assembly version.
/// Finds the DLL containing the onInstall addon's DotNetClass, or if none,
/// the DLL containing the first addon's DotNetClass.
/// Returns empty string if the version cannot be determined.
/// </summary>
private static string getCollectionVersionFromPrimaryDll(CoreController core, XmlDocument collectionDoc, List<string> assembliesInZip, string collectionVersionFolder) {
    //
    // -- find the target DotNetClass: prefer onInstall addon, fall back to first addon
    string targetDotNetClass = "";
    string onInstallGuid = XmlController.getXMLAttribute(core, collectionDoc.DocumentElement, "onInstallAddonGuid", "");
    foreach (XmlNode node in collectionDoc.DocumentElement.ChildNodes) {
        string nodeName = node.Name.ToLowerInvariant();
        if (nodeName != "addon" && nodeName != "add-on") { continue; }
        string addonGuid = XmlController.getXMLAttribute(core, node, "guid", "");
        string dotNetClass = "";
        foreach (XmlNode childNode in node.ChildNodes) {
            if (childNode.Name.ToLowerInvariant() == "dotnetclass") {
                dotNetClass = childNode.InnerText.Trim();
                break;
            }
        }
        if (string.IsNullOrEmpty(dotNetClass)) { continue; }
        //
        // -- if this is the onInstall addon, use it
        if (!string.IsNullOrEmpty(onInstallGuid) && string.Equals(addonGuid, onInstallGuid, StringComparison.OrdinalIgnoreCase)) {
            targetDotNetClass = dotNetClass;
            break;
        }
        //
        // -- capture the first addon's DotNetClass as fallback
        if (string.IsNullOrEmpty(targetDotNetClass)) {
            targetDotNetClass = dotNetClass;
        }
    }
    if (string.IsNullOrEmpty(targetDotNetClass)) { return ""; }
    //
    // -- find the DLL containing the target class and return its version
    foreach (string dllName in assembliesInZip) {
        string dllAbsPath = core.privateFiles.joinPath(core.privateFiles.localAbsRootPath, collectionVersionFolder) + dllName;
        if (!System.IO.File.Exists(dllAbsPath)) { continue; }
#if NET
        if (AssemblyMetadataHelper.ContainsType(dllAbsPath, targetDotNetClass)) {
            return AssemblyMetadataHelper.GetAssemblyVersion(dllAbsPath);
        }
#else
        try {
            var assembly = System.Reflection.Assembly.ReflectionOnlyLoadFrom(dllAbsPath);
            if (assembly.GetType(targetDotNetClass, false, true) != null) {
                return assembly.GetName().Version.ToString();
            }
        } catch {
            // -- could not inspect DLL
        }
#endif
    }
    return "";
}
```

**C) Set `collection.version` before save.** In the "set or clear all fields" block (around line 454), after `collection.layoutFileList = layoutFileList;`, add:

```csharp
//
// -- set version: prefer explicit version from collection XML, fall back to primary DLL version
if (!string.IsNullOrEmpty(collectionVersion)) {
    collection.version = collectionVersion;
} else {
    collection.version = getCollectionVersionFromPrimaryDll(core, Doc, assembliesInZip, CollectionVersionFolder);
}
```

The `collectionVersion` variable (from step 3A), `Doc`, `assembliesInZip`, and `CollectionVersionFolder` are all in scope. This goes before `collection.save(core.cpParent)` at line 461.

---

### Step 4: Export version to collection XML

**File:** `source/Processor/Controllers/Collection/Export/ExportController.cs`

After the existing `BlockNavigatorNode` attribute (line 63) and before `OnInstallAddonGuid` (line 64), add:

```csharp
string collectionVersion = cs.GetText("version");
if (!string.IsNullOrEmpty(collectionVersion)) {
    collectionXml.AppendLine($"\tVersion=\"{collectionVersion}\"");
}
```

This writes the version stored in the DB back to the exported collection XML, so that when the collection is re-imported on another site, the version attribute is present and Step 3A reads it directly.

---

## Edge cases handled

- **Collection XML has `version` attribute** (new build scripts): Used directly, no DLL inspection needed.
- **Collection XML has no `version` attribute** (legacy collections): Falls back to DLL assembly version lookup.
- **Collections without DLLs** (HTML/CSS only): No addon DotNetClass to find, fallback returns `""`. Version stays empty.
- **Addons without DotNetClass** (script-only, content-only): Skipped during iteration. If no addon has a DotNetClass, returns `""`.
- **onInstall addon with no DotNetClass**: Falls through to use the first addon that has one.
- **DLL not found on disk**: Skipped, continues to next DLL. Returns `""` if no match found.
- **Column doesn't exist yet**: On first deployment before CDef sync, `save()` may silently skip the column. Subsequent installs after sync will populate it.
- **net48 vs net9.0 builds**: net9.0 uses PE metadata reader (no assembly loading). net48 uses `Assembly.ReflectionOnlyLoadFrom()` to inspect types and version. Both are safe.

## Verification

1. Build: `dotnet build source/Processor/Processor.csproj` (both targets)
2. Build models: `dotnet build source/Models/Models.csproj`
3. After deployment and CDef sync, install a collection with a `version` attribute in its XML and verify the `version` column in `ccaddoncollections` matches
4. Install a collection without a `version` attribute and verify the fallback populates the version from the DLL
5. Export a collection and verify the XML includes a `Version` attribute
