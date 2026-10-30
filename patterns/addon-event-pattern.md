# Contensive Addon Event Pattern

> All patterns and API reference: [Patterns Index](https://raw.githubusercontent.com/contensive/Contensive5/refs/heads/master/patterns/index.md)

This document describes the Contensive Addon Event pattern — a publish/subscribe system that allows addons to communicate through named events. An addon can throw an event by name, and any addon that has registered as a catcher for that event will be executed automatically.

All new projects that need cross-addon communication should follow this pattern.

## Overview

The event system has three parts:

1. **Events** — Named records in the `ccAddonEvents` table. An event is simply a name string (e.g., "Addon Collection Installed").
2. **Catchers** — Records in the `ccAddonEventCatchers` junction table that link an addon to an event it wants to handle.
3. **Throwers** — Code that calls `cp.Site.ThrowEventByName("event name")` to trigger all catchers for that event.

## Database Tables

### ccAddonEvents

The master list of event names.

| Column | Type | Purpose |
|---|---|---|
| id | int (PK) | Auto-increment primary key |
| name | text | The event name used when throwing |
| sortOrder | text | Alpha sort order for admin UI |

Content name: `Add-on Events`

### ccAddonEventCatchers

Junction table linking addons to the events they handle.

| Column | Type | Purpose |
|---|---|---|
| id | int (PK) | Auto-increment primary key |
| addonId | int (FK) | References the addon that handles the event |
| eventId | int (FK) | References the event to handle |

Content name: `Add-on Event Catchers`

### ccAddonEventThrowers

Records which addon collections define/throw specific events.

| Column | Type | Purpose |
|---|---|---|
| id | int (PK) | Auto-increment primary key |
| eventId | int (FK) | References the event |
| collectionId | int (FK) | References the collection that throws this event |

Content name: `Add-on Event Throwers`

## How Events Are Thrown

Events are thrown by calling `cp.Site.ThrowEventByName("event name")` from addon code, or internally via `EventController.throwEventByName(core, "event name")`.

The execution flow:

1. Query `ccAddonEvents` for a record matching the event name.
2. If the event does not exist, it is **auto-created** (inserted into `ccAddonEvents`). No catchers are called.
3. If the event exists, query `ccAddonEventCatchers` joined to `ccAggregateFunctions` (addons) to find all registered handler addons.
4. Execute each handler addon in `ContextSimple` mode, sequentially.
5. Concatenate and return all addon output strings.

Key behaviors:
- Events are identified by **name only**. The modern API does not use IDs or GUIDs.
- If no catchers are registered for an event, `throwEventByName` returns an empty string with no errors.
- Each catcher addon runs in `ContextSimple` — there is no page context, no HTML document, no request/response.
- Exceptions in one catcher do not prevent other catchers from executing.

### Public API

```csharp
// Throw an event by name
string result = cp.Site.ThrowEventByName("event name");

// Throw an event with arguments. Each catcher reads them with cp.Doc.GetText("key")
string result = cp.Site.ThrowEventByName("event name", new Dictionary<string, string> {
    { "myPrefixOrderId", orderId.ToString() }
});
```

### Event arguments

`ThrowEventByName(string eventName, Dictionary<string, string> eventArguments)` passes arguments to every catcher (internally `EventController.throwEventByName(core, eventName, eventArguments)`).

- Each catcher runs with its own copy of the arguments as `argumentKeyValuePairs`, so they are copied into document properties. The catcher reads them with `cp.Doc.GetText(key)`, `cp.Doc.GetInteger(key)`.
- Before each catcher the arguments overwrite any document property with the same key (including values from the request). After the catchers run, the previous values are restored and keys that did not exist are cleared, so arguments never leak into the rest of the request.
- Prefix the keys (for example `sms...`) to avoid collisions with other document properties.
- Pass every key your catchers read, with an empty string when it does not apply.
- Treat arguments as hints. Pass a record id and let the catcher reload the record for authoritative data.
- `null` arguments are treated as none. The original `ThrowEventByName(string)` is unchanged and passes no arguments.

The following methods are deprecated and should not be used in new code:
- `cp.Site.ThrowEvent(string eventNameIdOrGuid)` — Deprecated
- `cp.Site.ThrowEvent(int eventId)` — Deprecated
- `cp.Site.ThrowEventByGuid(string eventGuid)` — Deprecated

## How to Register an Addon as an Event Catcher

There are two ways to register an addon to catch an event:

### Option 1: Admin UI

In the admin site, edit the addon record. On the **Events** tab, check the events the addon should handle in the "Events To Handle" checklist. This creates records in `ccAddonEventCatchers`.

### Option 2: Collection XML Data Records

In your collection XML file, add data records to create the catcher registration. This approach is preferred for addon collections because it makes the registration automatic during installation.

```xml
<Data>
  <!-- Register your addon to catch an event -->
  <Record Content="Add-on Event Catchers" Guid="{YOUR-UNIQUE-GUID}" Name="My Addon catches Collection Installed">
    <field Name="addonId">{GUID-OF-YOUR-HANDLER-ADDON}</field>
    <field Name="eventId">{GUID-OF-THE-EVENT}</field>
  </Record>
</Data>
```

For lookup fields in data records, you can reference the target record by its GUID. The `installDataNode()` method resolves the GUID to the correct database ID during installation.

## How to Define a New Event

### Step 1: Add an Event Record to Your Collection XML

Add a data record for the event in your collection XML (or in `aoBase51.xml` for system events):

```xml
<Data>
  <Record Content="Add-on Events" Guid="{YOUR-EVENT-GUID}" Name="Your Event Name">
  </Record>
</Data>
```

The GUID ensures the event is created only once, even if the collection is reinstalled.

### Step 2: Throw the Event from Code

In the addon or system code where the event should fire:

```csharp
// From addon code (using cp)
cp.Site.ThrowEventByName("Your Event Name");

// From processor internals (using core)
EventController.throwEventByName(core, "Your Event Name");
```

### Step 3: Document the Event

Add the event to the "Base Installation Events" section of this document (for system events) or document it in your collection's own documentation.

## Base Installation Events

These events are defined in the base collection (`aoBase51.xml`) and are available in every Contensive installation.

### Addon Collection Installed

| Property | Value |
|---|---|
| Name | `Addon Collection Installed` |
| GUID | `{BC52E864-809D-4B53-A6FE-53468AF8BA7F}` |
| Thrown by | `CollectionInstallController.installCollectionFromCollectionFolder()` |
| When | After a collection is successfully installed or upgraded, after the collection's onInstall addon has executed and before the final cache invalidation |

Use this event when your addon needs to respond to any collection being installed. For example, the Help Center addon catches this event to reindex its search content, because newly installed collections may include help files.

**Catcher example** — to register your addon to handle this event in your collection XML:

```xml
<Data>
  <Record Content="Add-on Event Catchers" Guid="{YOUR-CATCHER-GUID}" Name="My Addon catches Collection Installed">
    <field Name="addonId">{GUID-OF-YOUR-HANDLER-ADDON}</field>
    <field Name="eventId">{BC52E864-809D-4B53-A6FE-53468AF8BA7F}</field>
  </Record>
</Data>
```

### Text message events

Thrown by core text messaging (AWS End User Messaging SMS). The GUIDs are fixed: add-ons (aoCrm) register catchers by these GUIDs. Every event passes all of these arguments, empty when not applicable: `smsEventName, smsTextMessageLogId, smsMemberId, smsMemberIdList, smsPhone, smsBody, smsSource, smsIsTest, smsStatus, smsStatusCode, smsStatusDetail, smsLinkUrl, smsKeyword, smsOptOutSource`.

The Text Message Log row (`ccTextMessageLog`, id = `smsTextMessageLogId`) is saved in its final state before the event is thrown, so a catcher should reload it (columns `isInbound, isTest, source, statusId, fromPhone, toPhone, body, memberId, providerStatusCode, statusDetail`). `smsPhone` is the contact's number (E.164). `smsIsTest` is `1` or `0`. `smsMemberIdList` is a comma list of people whose phone matches the number exactly.

| Event | GUID | Thrown by / when | Arguments filled |
|---|---|---|---|
| `Text Message Status Changed` | `{E912CCC6-7A50-48BE-8296-CF332CFDEA17}` | `SmsNotificationController.processDeliveryEvent`, when an outbound text changes to Delivered or Failed (once per text) | smsTextMessageLogId (outbound row), smsMemberId, smsMemberIdList, smsPhone, smsSource, smsIsTest, smsStatus (`Delivered`/`Failed`), smsStatusCode (for example `TEXT_DELIVERED`), smsStatusDetail |
| `Text Message Link Clicked` | `{21643295-9E4E-4B7B-8714-48184545937A}` | `SmsLinkController.processClick`, first click of a tracked link `/tl/{token}`; not for test sends | smsTextMessageLogId (outbound row), smsMemberId, smsMemberIdList, smsPhone, smsSource, smsIsTest, smsLinkUrl |
| `Text Message Received` | `{F4ABCC00-3CA6-448F-A0FB-912F76F714FD}` | `SmsNotificationController.processInbound`, any inbound text that is not an opt-out | smsTextMessageLogId (inbound row), smsMemberId, smsMemberIdList, smsPhone, smsBody, smsSource (`Inbound`), smsIsTest (`0`), smsKeyword (`START`, `HELP` or empty) |
| `Text Message Opt Out` | `{9E507644-11DE-4D71-868E-EA9D26A8A5DA}` | inbound STOP keyword (`processInbound`), or a send rejected because the recipient opted out (send pipeline). The number is on the Text Message Block List before the event | smsTextMessageLogId (inbound STOP row, or the outbound row that was rejected), smsMemberId, smsMemberIdList, smsPhone, smsBody (the text, or the provider code), smsSource, smsIsTest, smsOptOutSource (`Keyword`/`Provider`) |

Catchers run in `ContextSimple`. Most of these events are thrown on an anonymous request (the SNS endpoint or the link redirect), but a provider opt-out (`Text Message Opt Out` with `smsOptOutSource=Provider`) is thrown inside the sender's request, where the current user is the admin who sent the text. Do not depend on the current user: write through models and `cp.Site.AddActivity`, using the ids from the log row.

## Example: Help Center Reindex on Collection Install

This walkthrough shows how the Help Center addon would use the event pattern to reindex after every collection installation.

### 1. Create a handler addon

In the Help Center project, create an addon class that performs reindexing:

```csharp
namespace HelpPages.Addons {
    public class ReindexOnCollectionInstallAddon : AddonBaseClass {
        public override object Execute(CPBaseClass cp) {
            // Reindex all help files
            HelpSearchIndexer.IndexAllFiles(cp);
            return "";
        }
    }
}
```

### 2. Define the addon in the collection XML

```xml
<Addon Name="Help Center Reindex on Collection Install"
       Guid="{YOUR-ADDON-GUID}"
       Category="Help">
  <Copy></Copy>
  <DotNetClass>HelpPages.Addons.ReindexOnCollectionInstallAddon</DotNetClass>
</Addon>
```

### 3. Register the addon as an event catcher

```xml
<Data>
  <Record Content="Add-on Event Catchers"
          Guid="{YOUR-CATCHER-GUID}"
          Name="Help Center Reindex catches Collection Installed">
    <field Name="addonId">{YOUR-ADDON-GUID}</field>
    <field Name="eventId">{BC52E864-809D-4B53-A6FE-53468AF8BA7F}</field>
  </Record>
</Data>
```

After this collection is installed, every future collection installation will trigger the reindex addon automatically.

## Housekeeping

The system includes automatic cleanup for orphaned event catchers. The daily housekeeping task (`AddonEventCatchersClass.executeDailyTasks()`) deletes catcher records whose referenced addon no longer exists:

```sql
DELETE FROM ccAddonEventCatchers
FROM ccAddonEventCatchers c
LEFT JOIN ccAggregateFunctions a ON a.id = c.addonId
WHERE a.id IS NULL
```

This ensures that uninstalling an addon automatically cleans up its event registrations.

## Key Source Files

| File | Purpose |
|---|---|
| `source/Processor/aoBase51.xml` | Base collection — event CDefs and data records |
| `source/Processor/Controllers/EventController.cs` | `throwEventByName()` — event execution logic, with and without arguments |
| `source/Processor/Controllers/Sms/SmsNotificationController.cs` | Text message events: argument keys, delivery and inbound events |
| `source/CPBase/BaseClasses/CPSiteBaseClass.cs` | `ThrowEventByName()` — public API definition |
| `source/Processor/Views/CPSiteClass.cs` | `ThrowEventByName()` — public API implementation |
| `source/Models/Models/Db/AddonEventModel.cs` | Event database model |
| `source/Models/Models/Db/AddonEventCatcherModel.cs` | Event catcher database model |
| `source/Models/Models/Db/AddonEventThrowerModel.cs` | Event thrower database model |
| `source/Processor/Addons/Housekeeping/AddonEventCatchersClass.cs` | Daily cleanup of orphaned catchers |
| `source/Processor/Controllers/Collection/Install/CollectionInstallController.cs` | Throws "Addon Collection Installed" event |
