# Plan: Move Email Block List Addon from Contensive5 to aoShare

## Summary

Move the `EmailBlockListAddon` from `Contensive5/source/Processor/Addons/Email/` to the `aoShare` repo at `c:\git\aoShare`. The addon is an admin UI tool that lists blocked email addresses (bounced + opted-out users) and lets admins unblock them. It uses only `CPBaseClass` APIs and raw SQL — no internal Processor dependencies — making it a clean candidate for extraction.

## Current State

### In Contensive5 (this repo)
- **Addon class:** `source/Processor/Addons/Email/EmailBlockListAddon.cs` — the full implementation (259 lines)
- **Addon XML registration:** `source/Processor/aoBase51.xml` line 3632-3635 — registers the addon with GUID `{B198244B-7B4B-45F3-A52D-34EC2B807E58}` and DotNetClass `Contensive.Processor.Addons.Email.EmailBlockListAddon`
- **Email Bounce List CDef:** `source/Processor/aoBase51.xml` line 1644-1659 — defines the `EmailBounceList` table (this stays; used by AWS SES, housekeeping, etc.)

### In aoShare (target repo)
- **Share.xml** already has two portal feature records referencing the Email Block List addon GUID:
  - Line 61: Under "Email" parent feature `{11074243-260D-403E-9765-8AAC22E01B5D}`
  - Line 96: Under "Reports" parent feature `{a6893923-6b27-4a34-99c4-86211fb659e5}` (note: has a typo `{{B198244B...}` — double opening brace)
- **No addon definition** exists in Share.xml yet (just the portal feature records pointing to the addon GUID)
- **No C# class** for EmailBlockList exists in aoShare yet

## What Does NOT Move

These items remain in Contensive5 because they are used by internal Processor subsystems:

- `EmailBounceListModel.cs` — used by `AwsSesController`, `AwsSesProcessClass`, `EmailBounceListClass` (housekeeping), `PeopleEmailBlockEditor`
- The `Email Bounce List` CDef in `aoBase51.xml` — defines the database table schema
- All AWS SES processing code that populates the bounce list

## Steps

### 1. Add addon definition to Share.xml (aoShare)

Add the `<Addon>` element to `c:\git\aoShare\collections\aoShare\Share.xml` (after the existing addon definitions, before `<Data>`):

```xml
<Addon Name="Email Block List" Guid="{B198244B-7B4B-45F3-A52D-34EC2B807E58}" Type="Add-on">
    <DotNetClass><![CDATA[Contensive.Share.EmailBlockListAddon]]></DotNetClass>
    <BlockEditTools>Yes</BlockEditTools>
</Addon>
```

Note the DotNetClass changes from `Contensive.Processor.Addons.Email.EmailBlockListAddon` to `Contensive.Share.EmailBlockListAddon` to match the aoShare namespace.

### 2. Fix the typo in Share.xml portal feature record (aoShare)

Line 98 has `{{B198244B` (double opening brace). Fix to `{B198244B`:

```xml
<field Name="addonid">{B198244B-7B4B-45F3-A52D-34EC2B807E58}</field>
```

### 3. Create the addon C# class in aoShare

Create `c:\git\aoShare\server\Share\share\Views\EmailBlockListAddon.cs`

This is a copy of `EmailBlockListAddon.cs` from Contensive5 with these changes:
- **Namespace:** `Contensive.Processor.Addons.Email` -> `Contensive.Share`
- **Remove** the portal-related GUID constants (`guidPortalFeature`, `guidCommunicatePortal`, `guidEmailParentFeature`) — the portal feature records are already defined in Share.xml data, not needed in code
- Keep all other logic identical (it only uses `CPBaseClass` APIs)

### 4. Remove the addon from Contensive5

#### 4a. Delete the addon class file
Delete `source/Processor/Addons/Email/EmailBlockListAddon.cs`

#### 4b. Remove the addon registration from aoBase51.xml
Remove lines 3632-3635 from `source/Processor/aoBase51.xml`:
```xml
  <Addon Name="Email Block List" Guid="{B198244B-7B4B-45F3-A52D-34EC2B807E58}" Type="Add-on">
    <DotNetClass><![CDATA[Contensive.Processor.Addons.Email.EmailBlockListAddon]]></DotNetClass>
    <BlockEditTools>Yes</BlockEditTools>
  </Addon>
```

### 5. Verify both solutions build

- Build aoShare: `dotnet build c:\git\aoShare\server\Share\Share.sln`
- Build Contensive5: `dotnet build` the Contensive5 solution

## Risk Assessment

- **Low risk:** The addon only uses `CPBaseClass` public API methods (`cp.User.IsAdmin`, `cp.Db.ExecuteQuery`, `cp.AdminUI.CreateLayoutBuilderList`, etc.) — no internal Processor types
- **Database tables stay:** Both `EmailBounceList` and `ccMembers` tables are defined by the base collection and will exist at runtime regardless of which DLL contains the addon
- **GUID preserved:** The addon GUID `{B198244B-7B4B-45F3-A52D-34EC2B807E58}` stays the same, so existing portal feature references continue to work
- **Typo fix:** The double-brace typo on Share.xml line 98 is a bug that should be fixed regardless
