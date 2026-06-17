
# Addon Pattern

> All patterns and API reference: [Patterns Index](https://raw.githubusercontent.com/contensive/Contensive5/refs/heads/master/patterns/index.md)

## Overview

Addons are the fundamental unit of extensibility in the Contensive platform. The primary architectural goal is to create an extensible system with dynamic routing and dynamic invocation based on an abstract class pattern. Every addon inherits from `AddonBaseClass` (or implements `CPBaseClass`) and exposes an `Execute()` method. The platform discovers, routes to, and invokes addons at runtime without compile-time knowledge of the specific implementation.

This design allows the platform core to remain stable while all application-specific behavior is delivered through addons packaged in [Addon Collections](addon-collection-pattern.md). Addons are registered in the database (`ccaggregatefunctions` table), and the platform uses metadata in the addon record to determine how and when each addon is invoked — whether through HTTP routing, page rendering, scheduled background execution, or event dispatch.

The addon execution engine (`AddonController`) handles dependency resolution, recursion protection, execution context management, asset injection (CSS/JS), and content rendering — all driven by the addon record's configuration fields.

## Addon Types

### General-Purpose Addons

Addons that are called from other addons using `cp.Addon.Execute()` or `cp.Addon.ExecuteByGuid()`. These serve as reusable components invoked programmatically. They have no special routing or scheduling configuration — they are simply called by name or GUID from within other addon code.

### Remote Methods

Addons configured as HTTP endpoints. When `remoteMethod` is true, the addon name (or alias) is registered as a route. Incoming HTTP requests matching the route invoke the addon, and its return value becomes the HTTP response body.

See [Remote Method Pattern](addon-remote-method-pattern.md).

### Page Widgets

Addons that content managers can drag and drop onto pages and templates using the page editor. When `content` is true, the addon appears in the widget toolbox. Each placement on a page creates an instance with its own settings record, allowing per-instance configuration.

Also referred to as Design Blocks.

See [Page Widget Pattern](addon-page-widget-pattern.md).

### Event Addons

Addons executed in response to platform events. The addon record includes boolean fields for specific event hooks (`onBodyStart`, `onBodyEnd`, `onPageStartEvent`, `onPageEndEvent`, `onNewVisitEvent`). When an event fires, all addons subscribed to that event are executed. Addons can also publish and subscribe to custom events through the addon event structure.

See [Addon Event Pattern](addon-event-pattern.md).

### Task Addons

Addons that run in the background outside the HTTP request pipeline on a scheduled or on-demand basis. Configured through `processInterval` (minutes between executions) or `processRunOnce` (execute once on next cycle). Task addons have no UI — their return output is ignored.

See [Task Addon Pattern](addon-task-pattern.md).

### Admin Tools

Addons that appear in the admin navigator and provide administrative interfaces. These depend on the AdminUI design pattern (`cp.AdminUI`) to build structured admin forms, lists, and settings pages. Configured by setting `admin` to true and using `navTypeId` to categorize them (report, setting, tool, etc.).

See [AdminUI Pattern](adminui-pattern.md), [Portal Pattern](portal-pattern.md), [Control Panel Pattern](control-panel-pattern.md).

### Dashboard Widgets

Addons that render exclusively on the admin dashboard. When `dashboardWidget` is true, the addon appears as a widget on the admin home screen. Dashboard widgets support types like number displays, charts (pie, line, bar), and custom HTML with periodic refresh.

See [Dashboard Widget Pattern](dashboard-widget-pattern.md).

### Diagnostic Addons

Addons used for health checks and system diagnostics. When `diagnostic` is true, the addon is included in diagnostic sweeps. These return status information about the application's health.

See [Diagnostic Addon Pattern](diagnostic-addon-pattern.md).

## Addon Record Field Reference

The addon record is stored in the `ccaggregatefunctions` table. The following fields control addon behavior.

### Identity and Metadata

| Field | Type | Description |
|-------|------|-------------|
| `name` | string | The addon's primary identifier. For remote methods, this is also the default route endpoint. Inherited from `DbBaseModel`. |
| `ccguid` | string | Globally unique identifier for the addon. Used for cross-environment references and collection packaging. Inherited from `DbBaseModel`. |
| `collectionId` | int | Foreign key to the addon collection that owns this addon. |
| `aliasList` | string | Comma-separated list of alternative names that can also be used to execute the addon (additional routes for remote methods). |
| `abbreviation` | string | Short name used in navigation when the full addon name is too long. |
| `category` | string | Category and optional subcategory (separated by a dot) for organizing addons in the widget editor toolbox. |
| `addonCategoryId` | int | Foreign key to the addon category record for grouping addons in admin lists. |
| `help` | string | Help content displayed in the admin interface for this addon. |
| `helpLink` | string | URL to external help documentation. |

### Execution Configuration

| Field | Type | Description |
|-------|------|-------------|
| `dotNetClass` | string | Fully qualified .NET class name (namespace + class) that implements the addon's `Execute()` method. |
| `argumentList` | string | Default name=value pairs (one per line) added to doc properties before execution. These are only set if the property does not already exist, allowing callers to override. |
| `contentSourceId` | int? | Enum specifying what content the addon returns. Values: All (1), Content Text (2), Content Wysiwyg (3), Remote Asset (4), Form Execution (5), Scripting Code Execution (6), DotNet Code Execution (7). |
| `remoteAssetLink` | string | URL to a remote asset. When `contentSourceId` is set to Remote Asset, this URL is fetched and its content is returned. |
| `link` | string | A URL associated with the addon. |

### Type Flags

| Field | Type | Description |
|-------|------|-------------|
| `remoteMethod` | bool | When true, the addon is registered as an HTTP endpoint. Requests matching the addon name or alias invoke it. |
| `content` | bool | When true, the addon can be placed on pages by content managers using the page editor (page widget / design block). |
| `template` | bool | When true, the addon can be placed on templates. |
| `email` | bool | When true, the addon can be used in email rendering. |
| `admin` | bool | When true, the addon appears in the admin navigator under its collection. |
| `filter` | bool | When true, the addon acts as a content filter. |
| `diagnostic` | bool | When true, the addon is included in diagnostic/health-check sweeps. |
| `dashboardWidget` | bool | When true, the addon renders as a widget on the admin dashboard. |
| `htmlDocument` | bool | When true, the addon returns a complete HTML document rather than a fragment. |

### Content Fields

| Field | Type | Description |
|-------|------|-------------|
| `copy` | string | WYSIWYG HTML content associated with the addon. |
| `copyText` | string | Plain text content associated with the addon. |
| `formXML` | string | XML definition for form-based addons. |

### Event Hooks

| Field | Type | Description |
|-------|------|-------------|
| `onBodyStart` | bool | When true, the addon executes at the start of every page body render. |
| `onBodyEnd` | bool | When true, the addon executes at the end of every page body render. |
| `onPageStartEvent` | bool | When true, the addon executes on the page start event. |
| `onPageEndEvent` | bool | When true, the addon executes on the page end event. |
| `onNewVisitEvent` | bool | When true, the addon executes when a new visit is detected. |

### Background Task Configuration

| Field | Type | Description |
|-------|------|-------------|
| `processInterval` | int? | Minutes between background executions. When set, the addon runs periodically as a task. |
| `processNextRun` | DateTime? | The next scheduled execution time. Updated automatically after each run. |
| `processRunOnce` | bool | When true, the addon executes once on the next background processing cycle, then this flag is cleared. |
| `processTimeout` | int? | Maximum seconds allowed for background execution before the task is terminated. |
| `processServerKey` | string | Server affinity key for distributed processing. When set, only the server with this key executes the task. |

### JavaScript Assets

| Field | Type | Description |
|-------|------|-------------|
| `jsHeadScriptSrc` | string | URL to an external JavaScript file. Used for the default platform (4) or when no platform 5 variant exists. |
| `jSHeadScriptPlatform5Src` | string | URL to an external JavaScript file specific to HTML platform 5. Takes precedence over `jsHeadScriptSrc` when the site is configured for platform 5. |
| `jsFilename` | FieldTypeJavascriptFile | Embedded JavaScript file stored in the addon record. Added to end-of-body or head depending on `javascriptForceHead`. |
| `minifyJsFilename` | FieldTypeJavascriptFile | Minified version of `jsFilename`, generated by the build process. Used when minification is enabled in site properties. |
| `javascriptForceHead` | bool | When true, JavaScript assets are injected into the `<head>` tag instead of end-of-body. |

### CSS Assets

| Field | Type | Description |
|-------|------|-------------|
| `stylesLinkHref` | string | URL to an external stylesheet. Used for the default platform (4) or when no platform 5 variant exists. |
| `StylesLinkPlatform5Href` | string | URL to an external stylesheet specific to HTML platform 5. Takes precedence over `stylesLinkHref` when the site is configured for platform 5. |
| `stylesFilename` | FieldTypeCSSFile | Embedded CSS file stored in the addon record. |
| `minifyStylesFilename` | FieldTypeCSSFile | Minified version of `stylesFilename`, generated by the build process. Used when minification is enabled in site properties. |

### Page Widget Configuration

| Field | Type | Description |
|-------|------|-------------|
| `instanceSettingPrimaryContentId` | int? | Foreign key to the content definition used for per-instance widget settings. Each time the widget is placed on a page, a new record is created in this content table, keyed by the instance GUID. |
| `editPlaceholderHtml` | string | HTML rendered in the page builder editor when the addon's actual output is not suitable for the editing experience. |
| `blockEditTools` | bool | When true, the page manager does not display the advanced edit toolbar for this addon. |
| `isInline` | bool | When true, the addon renders as an inline HTML element rather than a block element. |
| `inFrame` | bool | When true, the addon renders inside an iframe. |
| `asAjax` | bool | Deprecated. Previously rendered the addon via an AJAX callback. |

### Admin Navigation

| Field | Type | Description |
|-------|------|-------------|
| `navTypeId` | int | Categorizes the addon in the admin navigator. Values: Addon (1), Report (2), Setting (3), Tool (4), Comm (5), Design (6), Content (7), System (8). |

### Icon and Appearance

| Field | Type | Description |
|-------|------|-------------|
| `iconHtml` | string | HTML fragment used as the addon's icon in the dashboard and addon manager. Takes precedence over `iconFilename`. |
| `iconFilename` | string | URL to an image used as the addon's icon when `iconHtml` is not set. |
| `iconWidth` | int? | Width in pixels of the icon image. |
| `iconHeight` | int? | Height in pixels of the icon image. |
| `iconSprites` | int? | Number of sprites in the icon image (for sprite sheet icons). |

### SEO and Head Tags

| Field | Type | Description |
|-------|------|-------------|
| `pageTitle` | string | Browser tab title set when this addon renders as a page. |
| `metaDescription` | string | Meta description tag content for SEO. |
| `metaKeywordList` | string | Comma-separated meta keywords for SEO. |
| `otherHeadTags` | string | Additional HTML tags injected into the `<head>` element. |
| `robotsTxt` | string | Robots.txt directives associated with the addon. |

### Scripting (Legacy)

| Field | Type | Description |
|-------|------|-------------|
| `scriptingCode` | string | VBScript or JavaScript source code for script-based addons. |
| `scriptingLanguageId` | int | Script language identifier. Values: VBScript (1), JavaScript (2). |
| `scriptingEntryPoint` | string | The function name to call as the entry point in the scripting code. |
| `scriptingTimeout` | string | Maximum execution time for script-based addons. |
