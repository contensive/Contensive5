# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Contensive5 is an enterprise-grade addon execution framework that provides hardware abstraction and simplifies code reuse through a modular addon system. It's a sophisticated runtime environment that executes addons (extensible modules) while providing abstraction layers for system resources.

## Build Process & Commands

### Prerequisites
- Visual Studio 2022 (MSBuild)
- .NET SDK (for dotnet commands)
- 7-Zip (required for packaging help files and UI assets)
- Local NuGet package folder: `C:\NuGetLocalPackages\`

### Main Build Script
The primary build entry point is `scripts/jay-build.cmd`. This script:
1. Cleans all bin/obj folders across projects
2. Generates version numbers based on current date
3. Builds and packages CPBase, Models, and Processor (core suite)
4. Zips help files and UI assets (BaseAssets)
5. Builds CLI installer (WiX)
6. Updates test project NuGet packages
7. Creates deployment package to `C:\Deployments\Contensive5\Dev\`

### Building Individual Projects
```bash
# Build Processor (net48 target)
dotnet build source/Processor/Processor.csproj -p:TargetFramework=net48

# Build CPBase (netstandard2.0)
dotnet build source/CPBase/CPBase.csproj -p:TargetFramework=netstandard2.0

# Build Models (netstandard2.0)
dotnet build source/Models/Models.csproj -p:TargetFramework=netstandard2.0

# Build WebApi (net9.0)
dotnet build source/WebApi/WebApi.csproj

# Build TaskService (net48)
dotnet build source/TaskService/taskService.csproj -p:TargetFramework=net48

# Build CLI (net48)
dotnet build source/Cli/Cli.csproj -p:TargetFramework=net48
```

### Running Tests
```bash
# Run Processor unit tests
dotnet test source/ProcessorTests/ProcessorTests.csproj

# Run Models unit tests
dotnet test source/ModelsTests/ModelsTests.csproj
```

### Cleaning
```bash
# Clean entire solution
dotnet clean source/ContensiveCommon.sln

# Clean specific project
dotnet clean source/Processor/Processor.csproj
```

### Important Build Notes
- **Assembly Redirects**: After NuGet updates, manually update assembly bindings in `Processor/bin/Debug/net48/Processor.dll.config` and copy to TaskService's App.Config and IISDefault's Web.Config. The build script includes instructions for this manual step.
- **Package Management**: Built NuGet packages from CPBase, Models, and Processor are automatically copied to `C:\NuGetLocalPackages\` for use by downstream projects.
- **Version Numbering**: Uses format `YYYY.MM.DD.Revision` (e.g., 24.12.20.1), auto-incremented if same date build occurs.
- **Multi-targeting**: Processor and ProcessorTests target both `net48` and `net9.0-windows`; CPBase and Models target `netstandard2.0` for broad compatibility.

## Architecture & Design

### Layered Architecture
1. **Presentation Layer**: AdminSite addon, WebApi (ASP.NET Core)
2. **Addon/Extension Layer**: 20+ built-in addons providing functionality
3. **Core Processing Layer**: CoreController and Processor orchestrating execution
4. **Data Layer**: Models with 50+ database entity definitions
5. **Base Framework Layer**: CPBase abstract classes and interfaces
6. **Infrastructure**: AWS services, logging (NLog), caching (Redis), templating

### Core Dependency Injection Pattern
The **CoreController** (Processor/Controllers/CoreController.cs) is the central hub:
- Serves as the dependency injection container
- Manages persistent objects and document scope
- Maintains session state (user, visit, visitor data)
- Provides access to route mappings, server config, and logging
- Passed to addons and components for resource access
- Exposes logging context with thread ID, user, visit, and URL information

### Base Classes & Addon Interface
**CPBase** (netstandard2.0) defines abstract base classes that form the public API:
- `CPBaseClass`: Main API interface
- `CPAddonBaseClass`: Base for addon implementations
- `CPDbBaseClass`: Database access abstractions
- `CPCacheBaseClass`: Caching abstractions
- `CPHtmlBaseClass`: HTML/UI utilities
- `LayoutBuilder*` classes: UI component building
- `PortalBuilder`: Portal UI framework

**Addons** implement these base classes and receive a CPClass instance providing typed access to all framework capabilities.

### Solution Organization

**ContensiveCommon.sln** (core):
- `CPBase`: Base classes & interfaces (netstandard2.0, signed assembly)
- `Models`: Database entity models (netstandard2.0, signed assembly)
- `Processor`: Core execution engine with embedded addons (net48/net9.0, signed assembly)
- `TaskService`: Windows Service for background jobs (net48)
- `Cli`: Command-line interface (net48)

**ContensiveAspx.sln** (ASP.NET deployment):
- `WebApi`: ASP.NET Core REST API (net9.0-windows)
- `iisDefaultSite`: IIS website (.NET Framework 4.8)

**Test Projects**:
- `ProcessorTests`: Unit tests using MSTest (net48/net9.0)
- `ModelsTests`: Unit tests using MSTest

### Built-in Addons (20+)
Located in `source/Processor/Addons/`, key ones include:
- **AdminSite**: Web-based admin interface with controllers for content management
- **Authentication**: User authentication/authorization
- **Email**: Email sending capabilities
- **Download**: File download management
- **EditControls/EditModal**: Form UI components
- **Housekeeping**: Maintenance and cleanup tasks
- **PageManager**: Site page management
- **Personalize**: User personalization features
- **ResourceLibrary**: Asset management
- **PortalFramework**: Portal UI framework
- **Diagnostics, Redirect, SiteExplorer, TextMessage**: Additional utilities

### Key Models & Entities
Database entities in `source/Models/Models/Db/` include:
- Activity logs, Addon management, Content definitions
- User authentication (Users, UserDetails), Admin records
- Field definitions, Form definitions, Gender codes
- Page templates, Role permissions, Site properties
- Visit tracking, Content instances

### Package Signing
- **CPBase** and **Models** use `netstandard2.0` with strong name signing (`CPBase2.snk`)
- **Processor** uses strong name signing (signingKey.snk)
- Assembly version pinning: CPBase=4.1.2.0, Models=20.0.0.0
- Ensures binary compatibility across dependent projects

### Configuration & Deployment
- **ServerConfigModel**: Centralized server configuration including database, cache, filesystem, and AWS credentials
- **RouteMapModel**: Local cache of route definitions; auto-refreshes on updates
- **SessionController**: Manages per-request/per-session state (user, visit, visitor, properties)
- **NLog Integration**: Structured logging with AWS CloudWatch support
- **AWS Services**: S3, CloudWatch Logs, SES, SQS, SNS, Secrets Manager, IAM, STS

## Code Structure Patterns

### Addon Development
Addons follow this pattern:
1. Create folder in `Processor/Addons/{AddonName}`
2. Implement addon class inheriting from CPAddonBaseClass
3. Use injected CPClass instance to access framework resources
4. Define addon in XML collection file for installation
5. Reference addon resources via addon's resource definitions

### Database Access
- Use Models from `source/Models/Models/Db/` for entity definitions
- Access via CPDbBaseClass interface from injected cp instance
- Entity Framework integration for ORM
- SQL Server as primary database

### Routing
- Defined in RouteMapModel
- Loaded dynamically; updates cause cache refresh
- Local instance caching for performance
- IIS integration at page load end

### UI & Forms
- LayoutBuilder classes for dynamic UI construction
- Edit controls and modal forms for data entry
- AdminSite addon provides admin panel interface
- HTML utilities via CPHtmlBaseClass

## Development Workflow

1. **Create/Modify Code**: Edit source files in `source/` projects
2. **Build**: Use specific project build commands (see Build Commands) for incremental changes
3. **Test Locally**: Run MSTest projects with `dotnet test`
4. **Assembly Redirects**: If adding NuGet packages, update app.config binding redirects
5. **Full Build**: Run `scripts/jay-build.cmd` when ready for deployment package
6. **Deployment**: Packages are created in versioned folder at `C:\Deployments\Contensive5\Dev\`

## Important Implementation Notes

### Signing & Versioning
- CPBase and Models are strongly signed; never change their assembly versions without coordinating across all dependent projects
- Use FileVersion for build-specific versioning (set by build script)
- AssemblyVersion for API compatibility

### NuGet Package Versioning
- Version number follows build date format: `YYYY.MM.DD.Revision`
- Packages published to local folder for internal dependency resolution
- Downstream projects pull from `C:\NuGetLocalPackages\`

### Multi-targeting Considerations
- CPBase/Models target `netstandard2.0` for maximum compatibility
- Processor targets `net48` and `net9.0-windows` (multi-target)
- WebApi is `net9.0-windows` (modern ASP.NET Core)
- TaskService is `net48` (Windows Service requirement)

### Code Generation
- Processor includes auto-generated binding redirects (AutoGenerateBindingRedirects=true)
- Resources (Settings.Designer.cs, Resources.Designer.cs) are auto-generated from .settings and .resx files
- XML schemas and collection definitions used for addon installation

## Logging & Diagnostics

- **NLog**: Primary logging framework with structured logging support
- **AWS CloudWatch Logs**: Cloud logging sink
- **Common Log Context**: CoreController provides logCommonMessage with app, user, visit, thread, and URL context
- **Structured Logging**: Use logCpommonMessage_forStructuredLogging for logs requiring escaped braces

## Key Files to Understand

- `source/Processor/Controllers/CoreController.cs`: Central dependency injection hub
- `source/CPBase/BaseClasses/CPBaseClass.cs`: Main public API definition
- `source/Models/Models/Db/`: Database entity definitions
- `source/Processor/Processor.csproj`: Main project with dependencies and package configuration
- `scripts/jay-build.cmd`: Complete build orchestration
