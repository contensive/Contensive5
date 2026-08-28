# Contensive Patterns

Documentation for key patterns used in Contensive5 development.

## Best Practices

- [Contensive Best Practices](best-practices-pattern.md) — error handling conventions, try/catch patterns, and exception reporting

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

- [Addon Pattern](addon-pattern.md) — addon architecture, types, and field reference
- [Addon Collection Pattern](addon-collection-pattern.md) — packaging addons into installable collections
- [Page Widget Pattern](addon-page-widget-pattern.md) — addons that render content on a page
- [Dashboard Widget Pattern](dashboard-widget-pattern.md) — addons that render dashboard widgets
- [Remote Method Pattern](addon-remote-method-pattern.md) — addons called via AJAX/API endpoints
- [Task Addon Pattern](addon-task-pattern.md) — background task addons
- [Diagnostic Addon Pattern](diagnostic-addon-pattern.md) — diagnostic/health-check addons
- [Portal Pattern](portal-pattern.md) — building portal-style admin interfaces
- [Control Panel Pattern](control-panel-pattern.md) — control panel addons
- [Addon Event Pattern](addon-event-pattern.md) — publish/subscribe events between addons
- [MQTT Pattern](mqtt-pattern.md) — publishing commands to IoT devices and receiving device messages via AWS IoT Core

## UI & Layout Patterns

- [AdminUI Pattern](adminui-pattern.md) — building admin user interfaces
- [Layout Design Pattern](layout-design-pattern.md) — HTML layout records and mustache templates
- [Page Template Design Pattern](page-template-design-pattern.md) — page templates for websites

## Data Patterns

- [Database Models Pattern](database-models-pattern.md) — creating and using typed database model classes
- [Database Management Pattern](database-management-pattern.md) — database schema management and conventions

## Security

- [Authentication Pattern](authentication-pattern.md) — session tracking model (Pageview/Visit/Visitor/People), username/password, bearer token, HTTP Basic, and session cookie authentication flows
- [Security Best Practices](security-best-practices.md) — authentication vs authorization, the recognized state, secure coding patterns, and common vulnerabilities

## People & Tracking

- [People Tracking Pattern](people-tracking-pattern.md) — People record types (Bot/Guest/Contact), creation source, lifecycle management, and the Contact funnel stages

## Infrastructure

- [Redis Cache Setup](redis-cache-setup.md) — setting up AWS ElastiCache for Redis as the remote cache service
- [Secrets Management](secrets-management-pattern.md) — switchable config storage (file vs AWS Secrets Manager), EC2/Docker setup, and migration guide

## Build & Deployment

- [Build Script Pattern](build-script-pattern.md) — automating compile, package, and deploy of addon collections

## Testing

- [Testing Pattern](testing-pattern.md) — xUnit integration tests, Playwright E2E tests, test organization, AI agent workflows, and implementation guidance

## Code Review

The `/review-collection` slash command reviews an addon repo against all Contensive patterns and security practices.

**Using the command** (from any addon repo in Claude Code):
```
/review-collection
```
Or with a focus area:
```
/review-collection security
```

**Installing for use in any repo** (one-time per machine):
```
cp "C:\Git\Contensive5\.claude\commands\review-collection.md" "$HOME\.claude\commands\review-collection.md"
```
The command definition is maintained in `C:\Git\Contensive5\.claude\commands\review-collection.md`. When used from within the Contensive5 repo it is available automatically. To use it from other repos, copy it to your user-level commands folder.

## Documentation

- [Help Doc Pattern](help-doc-pattern.md) — creating help documentation for addons
