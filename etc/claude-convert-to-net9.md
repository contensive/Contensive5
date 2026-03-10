


saved as C:\Users\jay\.claude\plans\convert-to-core.md

When you're ready to revisit it in a future session, you can either:
  - Ask me to "read the migration plan" and I'll load it from that file
  - Reference it by name: convert-to-core






/clear
ere is Claude's plan:
╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌
 Contensive5 Migration Plan: .NET Framework 4.8 → .NET 9

 Context

 The Contensive5 solution is a mix of .NET Framework 4.8 and .NET 9 targets. Several projects (Processor, CLI, tests) already multi-target net48;net9.0-windows. CPBase and Models are already netstandard2.0. WebApi    
 already runs on .NET 9. The remaining Framework-only projects are TaskService (Windows Service) and iisDefaultSite (VB.NET WebForms). This plan removes all net48 targets and migrates those two projects, completing   
 the transition to .NET 9.

 ---
 Phase 1: Branch Setup & Dependency Cleanup

 Create branch migrate/net9 from master.

 Clean up Processor dependencies that will block removing net48:

 1. Replace Nustache.Core with the Stubble.Core project already in the solution
   - source/Processor/Controllers/MustacheController.cs — switch API call
   - source/Processor/Processor.csproj — remove raw DLL reference
 2. Remove dead references from Processor.csproj:
   - System.Data.Entity.Design HintPath (vestigial EF6 reference)
   - <Reference Include="netstandard"> HintPath to Framework facades dir
   - Entity Framework sections from app.config
 3. Evaluate <UseWPF>true</UseWPF> in Processor.csproj and WebApi.csproj — remove if no WPF types are used
 4. Remove netstandard HintPath from Cli.csproj

 ---
 Phase 2: Migrate TaskService to Worker Service

 Convert from ServiceBase pattern to Microsoft.Extensions.Hosting Worker Service targeting net9.0-windows.

 - Rewrite taskService.csproj as SDK-style with Sdk="Microsoft.NET.Sdk.Worker", targeting net9.0-windows
 - Add packages: Microsoft.Extensions.Hosting, Microsoft.Extensions.Hosting.WindowsServices
 - Rewrite Program.cs — use Host.CreateApplicationBuilder + AddWindowsService() + AddHostedService<ContensiveWorker>()
 - Rewrite TaskService.cs — inherit BackgroundService instead of ServiceBase, override ExecuteAsync
 - Delete ProjectInstaller.cs, ProjectInstaller.Designer.cs, related .resx files — service installed via sc create instead of installutil
 - Verify CLI→TaskService reference — likely removable since TaskSchedulerController/TaskRunnerController live in Processor

 Key files:
 - source/TaskService/taskService.csproj
 - source/TaskService/Program.cs
 - source/TaskService/TaskService.cs
 - source/TaskService/ProjectInstaller.cs

 ---
 Phase 3: Retire iisDefaultSite

 WebApi already serves as the replacement. Complete the transition:

 1. Move app name config — WebApi Program.cs line 61 hardcodes "c5test". Move to appsettings.json or environment variable (matching how iisDefaultSite reads ConfigurationManager.AppSettings("ContensiveAppName"))      
 2. Route map refresh — iisDefaultSite calls HttpRuntime.UnloadAppDomain() when routes change. Replace with in-process route map refresh (the RouteMapModel.dateCreated tracking already exists)
 3. Remove iisDefaultSite project and ContensiveAspx.sln

 Key files:
 - source/WebApi/Program.cs — app name configuration
 - source/iisDefaultSite/ — entire directory removed

 ---
 Phase 4: Remove net48 Target from Multi-Targeted Projects

 Change each project from <TargetFrameworks>net48;net9.0-windows</TargetFrameworks> to <TargetFramework>net9.0-windows</TargetFramework>:

 Processor.csproj

 - Remove: AutoGenerateBindingRedirects, GenerateBindingRedirectsOutputType
 - Remove: app.config (binding redirects not used in .NET 9)
 - Remove built-in packages: NETStandard.Library, System.Net.Http, Microsoft.Win32.Primitives, System.Reflection.TypeExtensions, Microsoft.Bcl.AsyncInterfaces
 - Remove StrongNamer (strong name validation relaxed in .NET 9 — test without it)
 - Consider: System.Data.SqlClient → Microsoft.Data.SqlClient (Microsoft's recommended replacement)

 Cli.csproj

 - Same cleanup as Processor

 ProcessorTests.csproj

 - Remove GAC-only references: System.ComponentModel.Composition, System.Configuration, System.Data.OracleClient, System.ServiceModel, System.ServiceProcess

 ModelsTests.csproj

 - Same pattern

 ---
 Phase 5: Strong Naming & Addon Compatibility

 Strong Naming (no changes needed)

 - CPBase stays netstandard2.0, signed with CPBase2.snk, AssemblyVersion pinned at 4.1.2.0
 - Models stays netstandard2.0, signed with CPModelKey.snk, AssemblyVersion pinned at 20.0.0.0
 - Processor stays signed with signingKey.snk

 Addon Backward Compatibility

 - Assembly.LoadFrom works in .NET 9 — no code changes needed for initial migration
 - External addons compiled against CPBase netstandard2.0 on .NET Framework 4.8 should load successfully since they only reference netstandard2.0 APIs
 - Addons that reference Framework-specific types (System.Web, etc.) will fail and need recompilation
 - Future improvement: Wrap addon loading in AssemblyLoadContext for better isolation (not required for initial migration)
 - The type check in AddonController.cs:1657 is string-based (FullName.Equals), which is resilient across runtime boundaries

 IIS Management

 - Microsoft.Web.Administration NuGet package already supports netstandard2.0 — no code changes for compilation
 - Update WebServerController.cs verifyAppPool to set ManagedRuntimeVersion = "" for .NET 9 apps (Kestrel runs out-of-process)

 P/Invoke

 - Kernel32.dll GetSystemTimePreciseAsFileTime in CoreController.cs:134 works as-is with net9.0-windows TFM

 ---
 Phase 6: Update Build Script & CI

 Update scripts/jay-build.cmd:
 - Change all -p:TargetFramework=net48 → -p:TargetFramework=net9.0-windows
 - Remove iisDefaultSite NuGet update and MSBuild steps (lines 288-314)
 - Add WebApi dotnet publish step
 - Remove cc.exe.config copy step (no binding redirects)
 - Update NuGet pack target frameworks

 ---
 Verification

 1. Build — Clean build of entire solution targeting net9.0-windows only
 2. Tests — Run ProcessorTests and ModelsTests against net9.0-windows; all previously-passing tests must pass
 3. Addon loading — Load a known .NET Framework 4.8 addon via Assembly.LoadFrom
 4. WebApi — HTTP requests through route map, cookie handling, file operations
 5. TaskService — Starts and runs as Windows Service via sc create
 6. IIS management — verifyAppPool/verifyWebsite with updated runtime version
 7. Build script — jay-build.cmd produces correct deployment artifacts

 ---
 Risk Summary

 ┌───────────────────────────────────────┬──────────┬──────────────────────────────────────────────────┐
 │                 Risk                  │ Severity │                    Mitigation                    │
 ├───────────────────────────────────────┼──────────┼──────────────────────────────────────────────────┤
 │ net48 addon loading fails             │ Medium   │ CPBase netstandard2.0 bridge; test early         │
 ├───────────────────────────────────────┼──────────┼──────────────────────────────────────────────────┤
 │ Nustache→Stubble template differences │ Low      │ Stubble.Core already in solution; test rendering │
 ├───────────────────────────────────────┼──────────┼──────────────────────────────────────────────────┤
 │ Build script breaks                   │ Medium   │ Update incrementally per phase                   │
 ├───────────────────────────────────────┼──────────┼──────────────────────────────────────────────────┤
 │ WiX installer incompatibility         │ Medium   │ May need separate WiX update                     │
 └───────────────────────────────────────┴──────────┴──────────────────────────────────────────────────┘