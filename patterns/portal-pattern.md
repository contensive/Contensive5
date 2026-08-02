
# Portal Pattern

> All patterns and API reference: [Patterns Index](https://raw.githubusercontent.com/contensive/Contensive5/refs/heads/master/patterns/index.md)

## Overview
A portal is a way to group one or more features together into a single UI environment with a common subject. For example the Account Manager portal groups together all individual features related to managing ecommerce accounts. Features can be data from content definitions or executable code form addons. Portals should be installed from the same Addon Collection xml file.

All executable code in a portal comes from Addons, defined in Contensive-architecture. A portal is the structure of database records in the Portals and Portal Features content, and are created through the xml nodes of the addon collection file, described here.

## Architecture
[In-depth architecture discussion]
A portal is executed by executing the Portal-addon. Each portal environment is defined with a record in the Portals content definition. For example the Portals record named "Account Manager" defines the Account Manager Portal and the Portals record "Add-ons" defines the Add-on Manager portal. A user opens a portal to work with those features by selecting in from the left-nav of the control panel. To run a portal from the url, use the following format:

/admin?addonGuid=%7BA1BCA00C-2657-42A1-8F98-BED1E5A42025%7D&setPortalGuid=(portal-guid)

/admin -- the control panel endpoint, defined in the config.json file and available in cp.GetAppConfig().adminRoute
addonGuid=%7BA1BCA00C-2657-42A1-8F98-BED1E5A42025%7D -- executes the Portals addon
setPortalGuid=(portal-guid) -- where portal-guid is the ccguid field on the Portals record

A portal is installed with the xml addon collection file with <record> nodes in <data> nodes. For example, this is the xml structure from the Crm.xml file that installs the CRM Portal

	<data>
		<record content="Portals" Guid="{ebf26a0a-e058-4933-a57c-baf7d46e6543}" Name="CRM">
			<field Name="defaultfeatureid">{19665f07-ae26-4ebb-93c2-235461fd1991}</field>
			<field Name="icon">crm/crm.svg</field>
		</record>
  </data>

the record guid -- a unique guid for this portal
defaultfeatureid -- the guid of the portal feature record that is executed when no other feature is selected
icon -- optional, the url of the icon that displays on the control-panel left navigation for the portal

Portal and portal feature access is controlled by standard Contensive role-based permissions. Assign roles to portals and features in the control panel to restrict access.

Within each portal there can be one or more Portal Features. Each Portal Feature can be one of three types, an empty feature, a Data Feature, or an Addon Feature. The type of feature is determined by the xml nodes datacontentid and addonid.
- An empty feature has both nodes empty and is used by the Portal to create a menuing entry under which other features may be listed.
- A Data Feature has the datacontentid populated with the Content Definition to be displayed.
- an Addon Feature has the addon node populated with the Addon to be executed.
- A feature should populate both addonid and datacontentid. If it does, it is an Addon Feature.

An Addon Feature should include
- An authorization test requiring the admin role
- a verification that the addon is running within the port. If not it should redirect
- this is a typical example of these tests:

	// 
	// -- authenticate/authorize
	if (!CP.User.IsAdmin)
		return "<p class=\">You are not authorized to use this application.</p>";
	// 
	// -- validate portal environment
	if (!CP.AdminUI.EndpointContainsPortal())
		return CP.AdminUI.RedirectToPortalFeature( (guid of portal), ( guid of portal feature ), "");
	// 

The Portal creates a navigation element at the top of the portal and executes the current portal feature under the navigation.

Navigation is hierarchical using the parentfeatureid field:
- Features with empty parentfeatureid appear at the top level
- Features with a parentfeatureid appear nested under that parent feature
- Parent features can be empty features (menu headings) or active features
- Navigation depth is unlimited but typically 2-3 levels for usability

### Standard top-level nav headings

When a collection defines a portal (a `<record content="Portals">` data record), it should also define the standard top-level navigation headings as empty Portal Feature records. These are nav-only entries (no `addonid`, no `datacontentid`) that group child features:

1. **Reports** — groups report-type addons. All `<Addon type="Report">` features should use this as their `parentfeatureid`.
2. **Tools** — groups tool-type addons. All `<Addon type="Tool">` features should use this as their `parentfeatureid`.
3. **Settings** (conditional) — if the collection includes a settings addon, add a top-level feature that links directly to that addon (not an empty heading). Only include this when a settings addon exists in the collection.

Example — defining the standard headings alongside the portal record:

```xml
<Data>
    <!-- define the portal -->
    <record content="Portals" guid="{portal-guid}" name="My Portal">
        <field Name="defaultfeatureid"></field>
    </record>
    <!-- Reports nav heading (empty feature, no addon, no data content) -->
    <record content="Portal Features" guid="{reports-heading-guid}" name="My Portal - Reports">
        <field name="portalid">{portal-guid}</field>
        <field name="heading"><![CDATA[Reports]]></field>
        <field name="parentfeatureid"></field>
        <field name="addonid"></field>
        <field name="datacontentid"></field>
        <field name="sortorder"><![CDATA[20 Reports]]></field>
    </record>
    <!-- Tools nav heading (empty feature, no addon, no data content) -->
    <record content="Portal Features" guid="{tools-heading-guid}" name="My Portal - Tools">
        <field name="portalid">{portal-guid}</field>
        <field name="heading"><![CDATA[Tools]]></field>
        <field name="parentfeatureid"></field>
        <field name="addonid"></field>
        <field name="datacontentid"></field>
        <field name="sortorder"><![CDATA[10 Tools]]></field>
    </record>
    <!-- Settings feature (links to the settings addon, top-level) -->
    <record content="Portal Features" guid="{settings-feature-guid}" name="My Portal - Settings">
        <field name="portalid">{portal-guid}</field>
        <field name="heading"><![CDATA[Settings]]></field>
        <field name="parentfeatureid"></field>
        <field name="addonid">{settings-addon-guid}</field>
        <field name="datacontentid"></field>
        <field name="sortorder"><![CDATA[90 Settings]]></field>
    </record>
</Data>
```

When a collection adds features to a portal defined in a **different** collection (e.g., a base collection), the top-level headings are owned by that other collection. The child collection should reference the heading GUIDs from the base collection in its `parentfeatureid` fields and document them with XML comments:

```xml
<!-- portal defined in base collection {portal-guid} -->
<!-- reports heading defined in base collection {reports-heading-guid} -->
<!-- tools heading defined in base collection {tools-heading-guid} -->
<Data>
    <record content="Portal Features" guid="{feature-guid}" name="My Portal - Reports - My Report">
        <field name="portalid">{portal-guid}</field>
        <field name="parentfeatureid">{reports-heading-guid}</field>
        <field name="addonid">{my-report-addon-guid}</field>
        <field name="heading"><![CDATA[My Report]]></field>
    </record>
</Data>
```

A Portal Feature is installed with the xml addon collection file with <record> nodes in <data> nodes. For example, this is the xml structure from the Crm.xml file that installs a list of people records in the CRM Portal


  <data>
		<record content="Portal Features" Guid="{c623d7fe-6a72-4e9e-a463-f0ae33bcb718}" Name="CRM Data People">
			<field Name="portalid">{ebf26a0a-e058-4933-a57c-baf7d46e6543}</field>
			<field Name="addonid"></field>
			<field Name="heading"><![CDATA[People]]></field>
			<field Name="parentfeatureid">{D60C44EB-8857-4809-9207-4843A42D6197}</field>
			<field Name="datacontentid">{C1985DC4-C985-4406-9934-61A911543454}</field>
			<field Name="addpadding">False</field>
		</record>	
  </data>


the record guid -- a unique guid for this record
portalid -- the guid of the portal where this feature is used
addonid -- the addon that is executed when a user clicks this feature. Non-widget addons return the html/css/js for this feature, displayed in the main section of the portal display. They follow the addon pattern defined in contensive-architecture.md. Dashbaord Widget adds return objects defined in the dashboard-widget-pattern.md file.
heading -- the navigation heading for the user
parentfeatureid -- a feature in the navigation in which this feature appears
datacontentid -- the guid of the content definition record (from the "Content Definitions" table, also called "Content") that defines the data table to display


In the Portals record there is a selection for the default Feature which is run in the portal when no other feature is selected. If no default portal feature is selected in the portals record, the portal creates a widget dashboard environment similar to the main control panel dashboard. The Contensive portal code displays all dashboard widgets from portal features where the addon specified has <DashboardWidget>Yes</DashboardWidget>. Only dashboard widgets from features in the current portal are displayed.

For example, this xml block defines the Account Manager portal with one Data Feature and two Addon Features. One of the Addon Features is in the Portal, and one is a dashboard widget that displays when the portal is opened

## Portal and Portal Feature Installation Rules

1. **Collections that define a portal**: All portal features in that collection should belong to that portal.
2. **Collections that do not define a portal**: The collection can install portal features in portals defined by other collections. When doing so, the collection must include an `<ImportCollection>` node for the collection that defines the target portal, ensuring it is installed first.
3. **Addon validate-portal requirement**: If an addon includes a validate-portal line (`CP.AdminUI.EndpointContainsPortal()` / `RedirectToPortalFeature`), then the collection file must include a portal feature record assigning that addon to the referenced portal.
4. **Addons without portal features**: An addon that has neither a portal feature record nor validate-portal code in its class runs outside a portal and is allowed. Do not add a portal feature or portal detection/redirect code independently — they must always be added together.

## Portal and Portal Feature Installation

When a new portal is installed, it should also include portal features for Reports, Tools, and Settings. These will not appear if they have no child records, but they should be created so other collections can install child features under them as needed.

Portals and portal features are installed through addon collection XML files. The key architectural principle is **separation of concerns between content/addon definitions and portal structure**.

### Preferred approach: portal features in the portal collection

The preferred pattern is to include portal features in the same collection that defines the portal. The portal collection defines:
- The Portal record itself
- All Portal Feature records within that portal
- Any addons that are specific to the portal UI

For example, the collection `aoContentPortal` defines the Content Portal record and the portal features that appear within it.

### Adding features to a portal from another collection

When a collection includes an addon or content that should appear in a portal defined by a different collection, the preferred approach is to define the portal feature record in the collection that installs the addon or content — not in the portal collection. This keeps the feature definition alongside the code or data it references, so installing the collection automatically adds its features to the portal.

The collection must include an `<ImportCollection>` node for the collection that defines the portal record. This ensures the portal exists before the feature referencing it is installed. The portal feature record can then reference the portal by GUID even though the portal is defined elsewhere.

For example, if `aoReports` defines a reporting addon that belongs in the Content Portal:

```xml
<Collection name="Reports" guid="{...}">
    <!-- import the collection that defines the Content Portal -->
    <ImportCollection name="Content Portal">{content-portal-collection-guid}</ImportCollection>

    <!-- define the addon -->
    <Addon name="Usage Report" guid="{usage-report-addon-guid}" type="Add-on">
        <DotNetClass><![CDATA[Contensive.Reports.UsageReportAddon]]></DotNetClass>
    </Addon>

    <!-- add a portal feature for this addon in the Content Portal -->
    <Data>
        <record content="Portal Features" guid="{feature-guid}" name="Content - Reports - Usage Report">
            <field name="portalid">{content-portal-guid}</field>
            <field name="heading"><![CDATA[Usage Report]]></field>
            <field name="addonid">{usage-report-addon-guid}</field>
            <field name="parentfeatureid">{reports-heading-feature-guid}</field>
            <field name="datacontentid"></field>
        </record>
    </Data>
</Collection>
```

### Base collection content vs. portal collections

When the base Contensive collection (`aoBase51`) includes content definitions or addons that belong in a portal, the base collection should **only** define the content definition (CDef) or addon — it should **not** define the portal or portal features. A separate portal collection owns the portal structure and creates the portal features that reference the base content or addons.

For example:
- `aoBase51` defines the "Page Content" CDef (the database table and fields)
- `aoContentPortal` defines the Content Portal and includes a Portal Feature for Page Content that points to the CDef

This separation ensures that:
1. The base collection stays focused on core data and behavior
2. Portal organization can be changed or extended without modifying the base collection
3. Multiple portal collections can reference the same base content or addons
4. Portal collections can be installed or updated independently

### Cross-collection references

Portal features reference addons and content definitions by GUID. Since GUIDs are globally unique, a portal feature in one collection can reference an addon or CDef defined in a different collection. The referenced collection must be installed for the feature to work — use `<ImportCollection>` to declare the dependency:

```xml
<Collection name="Content Portal" guid="{...}">
    <!-- declare dependency on the base collection that defines the CDefs -->
    <ImportCollection name="Base Collection">{base-collection-guid}</ImportCollection>

    <!-- define the portal -->
    <Data>
        <record content="Portals" guid="{portal-guid}" name="Content">
            <field Name="defaultfeatureid">{default-feature-guid}</field>
            <field Name="icon">content/content.svg</field>
        </record>

        <!-- feature referencing a CDef from the base collection -->
        <record content="Portal Features" guid="{feature-guid}" name="Content - Page Content">
            <field name="portalid">{portal-guid}</field>
            <field name="heading"><![CDATA[Page Content]]></field>
            <field name="datacontentid">{page-content-cdef-guid-from-base}</field>
            <field name="addonid"></field>
            <field name="parentfeatureid"></field>
        </record>
    </Data>
</Collection>
```

### Bundle installer collections

For convenience, a bundle collection can install multiple portal collections together using `<ImportCollection>` references. For example, `InstallBasePortals` imports all the standard portal collections (Ecommerce, Add-on Manager, Distance Learning, etc.) so they can be installed in a single operation without defining any portals or features itself.

## Complete Example

<!-- collection file -->
<Collection name="Ecommerce" guid="{8dbe6b55-18db-44eb-9a4a-5cbba84df35a}">
  <!-- define the executable addon for a Sales Dashboard Widget -->
	<Addon name="Sales Dashboard Widget" guid="{F7B0EA1B-85FB-43C4-84B3-190806562BA2}" type="Add-on">
    <!-- the dotnet class of the addon in the assembly installed with the xml file -->
		<DotNetClass><![CDATA[Contensive.AccountBilling.Addons.DashboardWidgets.SalesNumberWidget]]></DotNetClass>
    <!-- this addon is a dashboard widget -->
		<DashboardWidget>Yes</DashboardWidget>
	</Addon>	
  <!-- define teh executable addon for the cash register -->
	<Addon name="Cash Register" guid="{CE7EA329-6F3E-4F3F-BF90-572AFBD9F73A}" type="Tool">
		<DotNetClass><![CDATA[Contensive.AccountBilling.AccountManagerToolCashRegisterAddon]]></DotNetClass>
    <!-- designate that it can be used in the control panel so it also appears on the control panel navigation -->
		<Admin>Yes</Admin>
	</Addon>
  <!-- define the content definition for the Accounts data table (sample only) -->
	<CDef Name="Accounts"  isbasecontent="0" guid="{9F5AF044-9256-47F2-86D8-A4B7C90C86C0}" >
		<Field Name="name"  FieldType="Text"></Field>
	</CDef>
  <Data>
  <!-- define the Account Manager Portal -->
		<record content="Portals" guid="{12528435-EDBF-4FBB-858F-3731447E24A3}" name="Account Manager">
			<field Name="defaultfeatureid"></field>
			<field Name="icon">dashboardIcons\account.svg</field>
		</record>
  <!-- define the feature widget record that uses the Sales Widget Addon -->
		<record content="Portal Features" guid="{47C84B4A-0EF6-47DD-8725-6368080D624A}" name="Ecommerce - Dashboard Sales Widget">
			<field name="portalid">{12528435-EDBF-4FBB-858F-3731447E24A3}</field>
			<field name="addonid">{F7B0EA1B-85FB-43C4-84B3-190806562BA2}</field>
			<field name="datacontentid"></field>
		</record>
    <!-- Reports nav heading (empty feature, groups report addons) -->
		<record content="Portal Features" guid="{D60C44EB-8857-4809-9207-4843A42D6197}" name="Ecommerce - Reports">
			<field name="portalid">{12528435-EDBF-4FBB-858F-3731447E24A3}</field>
			<field name="heading"><![CDATA[Reports]]></field>
			<field name="parentfeatureid"></field>
			<field name="addonid"></field>
			<field name="datacontentid"></field>
			<field name="sortorder"><![CDATA[20 Reports]]></field>
		</record>
    <!-- Tools nav heading (empty feature, groups tool addons) -->
		<record content="Portal Features" guid="{CA913E7D-00EF-4466-8C75-9B82A2EA4136}" name="Ecommerce - Tools">
			<field name="portalid">{12528435-EDBF-4FBB-858F-3731447E24A3}</field>
			<field name="heading"><![CDATA[Tools]]></field>
			<field name="parentfeatureid"></field>
			<field name="addonid"></field>
			<field name="datacontentid"></field>
      <!-- sortorder determines the order in which these features appear in navigation -->
			<field name="sortorder"><![CDATA[10 Tools]]></field>
		</record>
    <!-- Tool addon feature nested under the Tools heading -->
    <record content="Portal Features" guid="{C882E99E-6130-4F5E-B20C-32126D68D3E8}" name="Ecommerce - Tools - Cash Register">
        <field name="portalid">{12528435-EDBF-4FBB-858F-3731447E24A3}</field>
        <field name="heading"><![CDATA[Cash Register]]></field>
        <field name="parentfeatureid">{CA913E7D-00EF-4466-8C75-9B82A2EA4136}</field>
        <field name="addonid">{CE7EA329-6F3E-4F3F-BF90-572AFBD9F73A}</field>
        <field name="datacontentid"></field>
    </record>
    <!-- Data Feature that lists the Accounts records -->
		<record content="Portal Features" guid="{1036FDCB-6745-4F78-B946-BD81179DB24A}" name="Ecommerce - Data - Accounts">
			<field name="portalid">{12528435-EDBF-4FBB-858F-3731447E24A3}</field>
			<field name="addonid"></field>
			<field name="heading"><![CDATA[Accounts]]></field>
			<field name="parentfeatureid"></field>
			<field name="datacontentid">{9F5AF044-9256-47F2-86D8-A4B7C90C86C0}</field>
		</record>
	</Data>
</Collection>
