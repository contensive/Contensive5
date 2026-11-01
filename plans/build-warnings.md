# Build Warnings

Build version: 26.4.23.4
Date captured: 2026-04-23

## Summary

| Warning Code | Count | Description |
|---|---|---|
| CS1591 | 477 | Missing XML comment for publicly visible type or member |
| CS0618 | 13 | Use of obsolete member |
| CS0108 | 2 | Member hides inherited member |
| CS1573 | 1 | Parameter has no matching param tag in XML comment |
| CS0809 | 1 | Obsolete member overrides non-obsolete member |
| CS0169 | 1 | Field is never used |
| CS0162 | 1 | Unreachable code detected |
| NU5128 | 1 | NuGet pack target framework mismatch |
| WiX | 1 | Solution properties not available outside IDE |
| **Total** | **~498** | **(unique warnings, some emit twice for dual-target builds)** |

## By Project

| Project | Warnings |
|---|---|
| CPBase.csproj | ~480 (almost all CS1591) |
| Processor.csproj | ~16 |
| Models.csproj | 1 |
| cli.installer.wixproj | 1 |

---

## CS1591 - Missing XML comment (477 unique)

**Project:** CPBase

CPBase has `<GenerateDocumentationFile>true</GenerateDocumentationFile>` which requires XML doc comments on all public members. The vast majority of warnings come from public API base classes missing `<summary>` comments.

**Affected classes (most warnings):**
- `CPHtmlBaseClass` - ~100+ overloads (div, h1-h6, InputText, Hidden, Form, Button, CheckBox, etc.)
- `CPHtml5BaseClass` - ~100+ overloads (same pattern as above)
- `CPCSBaseClass` - GetFormInput, OpenSQL2, OpenGroupListUsers, etc.
- `CPContentBaseClass` - fileTypeIdEnum values, GoogleVisualizationStatusEnum
- `CPDocBaseClass` - GetNumber, GetInteger, get_Var, etc.
- `CPUserBaseClass` - GetProperty, IsInGroup, IsMember, etc.
- `CPVisitBaseClass` / `CPVisitorBaseClass` - GetProperty, GetNumber, etc.
- `CPAddonBaseClass` - Open, Template, RemoteAssetLink
- `CPAdminUIBaseClass` - CreateLayoutBuilder*, GetPortalHtml
- `CPFileSystemBaseClass` - FolderDetail properties, FileList
- `CPRequestBaseClass` - BrowserVersion, BrowserIsWindows, etc.
- `CPSiteBaseClass` - ThrowEvent, ThrowEventByGuid
- `CPSecurityBaseClass` / `CPSecretsBaseClass`
- `CPMessageQueueBaseClass` - GetMessageList, DeleteMessage
- `CPImageBaseClass` - GetBestFit, GetBestFitWebP
- `CPLayoutBaseClass` - ImporttypeEnum values
- `CPUtilsBaseClass` - EncodeContentForWeb
- `LayoutBuilderListBaseClass` - setCell overloads, addRow, addColumn, column* properties
- `LayoutBuilderBaseClass` - portalSubNavTitle, cp
- `LayoutBuilderTwoColumnLeftBaseClass` - headline, contentLeft, contentRight
- `LayoutBuilderTabbedBodyBaseClass`, `LayoutBuilderToolFormBaseClass`, `LayoutBuilderNameValueBaseClass`
- `AddonBaseClass`
- `HtmlAttributesGlobal` - onclick
- `EllipseMenuDataModel` / `EllipseMenuDataItemModel` / `EllipseMenuItem`
- `PortalBuilderNavItemViewModel` / `PortalBuilderSubNavItemViewModel`
- `QueueMessageDetail`
- `ReportListColumnBaseClass` - name, caption, visible, sortable, etc.
- `NameValueSelected`
- `ServerConfigBaseModel` - useSecretManager

**Fix options:**
1. Add XML doc comments to all public members (thorough but labor-intensive)
2. Suppress CS1591 in CPBase.csproj with `<NoWarn>1591</NoWarn>` (quick fix)
3. Remove `<GenerateDocumentationFile>` if XML docs are not being consumed

---

## CS0618 - Obsolete member usage (13 instances)

### TimeZone is obsolete (use TimeZoneInfo)
- `CPDateBaseClass.cs:38` - `TimeZone` usage
- `CPDateBaseClass.cs:60` - `TimeZone` usage
- **Project:** CPBase

### LayoutBuilderBaseClass.baseAjaxUrl is obsolete
- `SampleLayoutBuilderNameValue.cs:36`
- `SampleLayoutBuilder.cs:36`
- `LayoutBuilderClass.cs:238`
- **Project:** Processor

### LayoutBuilderBaseClass.baseUrl is obsolete
- `SampleLayoutBuilderNameValue.cs:40`
- `SampleLayoutBuilder.cs:41`
- **Project:** Processor

### LayoutBuilderBaseClass.csvDownloadFilename is obsolete
- `SampleLayoutBuilderNameValue.cs:70`
- `SampleLayoutBuilderNameValue.cs:71`
- **Project:** Processor

### LayoutBuilderBaseClass.portalSubNavTitle is obsolete
- `LayoutBuilderTabbedBodyClass.cs:359` (2 references on same line)
- **Project:** Processor

### CPUserBaseClass.IsEditingAnything is obsolete
- `AuthStatus.cs:31` - should use `IsEditing()` instead
- **Project:** Processor

### AddonModel.javaScriptBodyEnd is obsolete
- `BuildDataMigrationController.cs:239` - should use `javascriptInHead`
- **Project:** Processor

---

## CS0108 - Member hides inherited member (2 instances)

- `LayoutBuilderNameValueClass.cs:18` - `cp` field hides `LayoutBuilderBaseClass.cp`
- `LayoutBuilderTabbedBodyClass.cs:16` - `cp` field hides `LayoutBuilderBaseClass.cp`
- **Project:** Processor
- **Fix:** Add `new` keyword or remove the duplicate field and use the inherited one

---

## CS1573 - Missing param tag in XML comment (1 instance)

- `CPUtilsBaseClass.cs:272` - Parameter `ignoreId` has no matching param tag for `EncodeContentForWeb(string, string, int, int)`
- **Project:** CPBase
- **Fix:** Add `<param name="ignoreId">` to the XML comment

---

## CS0809 - Obsolete member overrides non-obsolete member (1 instance)

- `CPContentClass.cs:472` - `GetListLink(string)` is marked `[Obsolete]` but the base class method is not
- **Project:** Processor
- **Fix:** Mark the base class method as `[Obsolete]` too, or remove the attribute from the override

---

## CS0169 - Field never used (1 instance)

- `DashboardUserConfigModel.cs:14` - field `cp` is never used
- **Project:** Processor
- **Fix:** Remove the unused field

---

## CS0162 - Unreachable code (1 instance)

- `DbBaseModel.cs:983` - unreachable code detected
- **Project:** Models
- **Fix:** Remove or restructure the unreachable code

---

## NU5128 - NuGet pack target framework mismatch (1 instance)

- Processor.csproj - `net9.0-windows7.0` target framework declared in dependencies but no matching lib/ref assemblies
- **Project:** Processor
- **Fix:** Align nuspec dependencies with actual build outputs

---

## WiX Warning - Solution properties unavailable (1 instance)

- `cli.installer.wixproj` - Solution properties only available during IDE builds
- **Fix:** Add `<DefineSolutionProperties>false</DefineSolutionProperties>` to the .wixproj file
