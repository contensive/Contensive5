# Contensive Patterns

Documentation for key patterns used in Contensive5 development.

## Architecture

- [Contensive Architecture](contensive-architecture.md) — overall system architecture, addon lifecycle, and design patterns

## API Reference

Comprehensive API documentation for downstream projects consuming CPBase, Models, and Processor via NuGet:

- [Database API](api-database-reference.md) — DbBaseModel, CPDbBaseClass (cp.Db), CPCSBaseClass (cp.CSNew())
- [CPBase API](api-cpbase-reference.md) — CPBaseClass (the `cp` object), addon execution, doc/request/response, email, logging
- [Site, User & Cache API](api-site-user-cache-reference.md) — site properties, user identity/auth, caching, content metadata, visit/visitor
- [Filesystem & Utilities API](api-filesystem-utilities-reference.md) — file systems, HTTP, security, secrets, groups, HTML helpers, Mustache, utilities
- [LayoutBuilder API](api-layoutbuilder-reference.md) — admin UI lists, forms, tabs, two-column layouts, name-value forms

## Addon Patterns

- [Addon Collection Pattern](addon-collection-pattern.md) — packaging addons into installable collections
- [Page Widget Pattern](page-widget-pattern.md) — addons that render content on a page
- [Dashboard Widget Pattern](dashboard-widget-pattern.md) — addons that render dashboard widgets
- [Remote Method Pattern](remote-method-pattern.md) — addons called via AJAX/API endpoints
- [Process Addon Pattern](process-addon-pattern.md) — background process addons
- [Diagnostic Addon Pattern](diagnostic-addon-patter.md) — diagnostic/health-check addons
- [Portal Pattern](portal-pattern.md) — building portal-style admin interfaces
- [Control Panel Pattern](control-panel-pattern.md) — control panel addons

## UI & Layout Patterns

- [AdminUI Pattern](adminui-pattern.md) — building admin user interfaces
- [Layout Design Pattern](layout-design-pattern.md) — HTML layout records and mustache templates
- [Page Template Design Pattern](page-template-design-pattern.md) — page templates for websites

## Data Patterns

- [Database Models Pattern](database-models-pattern.md) — creating and using typed database model classes
- [Database Management Pattern](database-management-pattern.md) — database schema management and conventions

## Documentation

- [Help Doc Pattern](help-doc-pattern.md) — creating help documentation for addons
