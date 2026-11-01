# Test Coverage Pipeline Plan

Phase 2 of a three-phase plan to automate the monthly Contensive release process.
This phase adds test projects to addon repos that currently lack them, integrated
into the release pipeline built in Phase 1.

Related plans:
- [deployment-pipeline.md](deployment-pipeline.md) — Phase 1: manifest, runner, NuGet update
- [cicd-pipeline.md](cicd-pipeline.md) — Phase 3: GitHub Actions CI/CD

---

## Problem Statement

Of the ~52 addon repos in the release pipeline, only 9 currently have test projects:

| Repo | Test Project | Framework |
|------|-------------|-----------|
| Contensive5 | ProcessorTests, ModelsTests | MSTest |
| aoAddonManager | addonManager51Tests | xUnit v3 |
| aoCalender2 | aoCalendar2Tests | xUnit v3 |
| aoCrm | aoCrmTests | xUnit v3 |
| aoDesignBlocks | aoDesignBlocksTests | xUnit v3 |
| aoEcommerce | accountBillingTests | xUnit v3 |
| aoHelpCenter | Tests | (unknown) |
| aoHtmlImport | UnitTestProject1 | (unknown) |
| aoMeetingManager | MeetingManager.tests | (unknown) |
| aoMembershipManager | Testing | (unknown) |

The remaining ~43 repos with C# code have no automated tests. This means the
`test` step in the release pipeline is either skipped or meaningless for most repos,
and regressions are caught only during manual smoke testing.

---

## Goals

1. Every Tier 0 and Tier 1 repo has at minimum smoke-level unit tests
2. Every Tier 2 repo with C# code has at least a scaffold test project that builds
3. Test projects follow a consistent pattern across all repos
4. Tests can run without a database connection (unit tests) or with a named
   Contensive app (integration tests)
5. The release runner's `test` step has something meaningful to execute for every repo

---

## Approach: Incremental, Not Big-Bang

Adding comprehensive tests to 43 repos at once is not realistic. Instead:

1. **Scaffold first** — create empty test projects that build and have one placeholder test
2. **Add smoke tests** — each repo gets 1-3 tests that verify core addon classes load
3. **Add targeted tests** — as bugs are fixed or features are added, require tests
4. **Prioritize by tier** — Tier 0 and 1 failures cascade, so they get tests first

---

## Standard Test Project Structure

All new test projects follow this convention (matching aoDesignBlocks and aoCrm):

```
server/
  <RepoName>Tests/
    <RepoName>Tests.csproj    # xUnit v3, net8.0, OutputType=exe
    Tests/
      UnitTests/              # no database required
        SmokeTests.cs         # verifies addon classes can be instantiated
      IntegrationTests/       # requires a Contensive app
        (added later)
```

### .csproj template

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <OutputType>exe</OutputType>
    <LangVersion>latest</LangVersion>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="xunit.v3" Version="3.2.2" />
    <PackageReference Include="xunit.runner.visualstudio" Version="3.1.5" />
    <PackageReference Include="Contensive.DbModels" Version="*" />
    <PackageReference Include="Contensive.Processor" Version="*" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\<MainProject>\<MainProject>.csproj" />
  </ItemGroup>
</Project>
```

### SmokeTests.cs template

```csharp
using Xunit;

namespace Contensive.<AddonName>.Tests.UnitTests;

/// <summary>
/// Verifies that core classes can be loaded and instantiated.
/// These tests run without a database connection.
/// </summary>
public class SmokeTests
{
    [Fact]
    public void MainAddonAssemblyLoads()
    {
        // Verify the assembly can be loaded — catches missing dependencies
        var assembly = typeof(<SomePublicClass>).Assembly;
        Assert.NotNull(assembly);
    }
}
```

---

## Implementation Phases

### Phase 2A: Scaffold Test Projects for Tier 1 Repos

Priority: Tier 1 repos produce NuGet packages. A broken build here blocks
downstream repos.

| Repo | Status | Action |
|------|--------|--------|
| aoMCP | no tests | scaffold + smoke tests |
| aoCrm | has tests | verify tests pass, add to release runner |
| aoDesignBlocks | has tests | verify tests pass, add to release runner |
| aoEcommerce | has tests | verify tests pass, add to release runner |

Tasks:
1. Scaffold `aoMcpTests` project in aoMCP
2. Add aoMCP test project to the solution
3. Verify all existing Tier 1 test projects build and pass
4. Update `repo-catalog.yml` to set `has_tests: true` for all Tier 1 repos

### Phase 2B: Scaffold Test Projects for High-Value Tier 2 Repos

Start with repos that have the most client usage or the most complex logic:

| Priority | Repo | Rationale |
|----------|------|-----------|
| High | aoBlog | Used by many client sites, MCP integration |
| High | aoNewsletter2 | Used by many client sites, MCP integration |
| High | aoMenuing | Critical for site navigation |
| High | aoFormWizard | User-facing data collection |
| Medium | aoSecurity | Security-critical |
| Medium | aoTextSearch2 | Complex search logic |
| Medium | aoContentPortal | Admin-facing, complex |
| Medium | aoAddonManager | Already has tests — verify they pass |

Tasks:
1. Use the `scaffold-tests` skill to generate test projects for each
2. Add one smoke test per repo (assembly loads, key class instantiates)
3. Add test projects to each repo's solution file
4. Verify each test project builds against current NuGet packages

### Phase 2C: Scaffold Remaining Tier 2 Repos

Batch-scaffold test projects for all remaining Tier 2 repos that have C# code.
These start as build-only smoke tests and get real tests added over time.

Tasks:
1. Run `scaffold-tests` for each remaining Tier 2 repo
2. Commit test projects to each repo's dev branch
3. Update `repo-catalog.yml` for all repos

### Phase 2D: Normalize Existing Test Projects

Some existing test projects use older patterns or frameworks:

| Repo | Issue | Action |
|------|-------|--------|
| aoHelpCenter | unknown framework | verify, migrate to xUnit v3 if needed |
| aoHtmlImport | named "UnitTestProject1" | rename to match convention |
| aoMembershipManager | named "Testing" | rename to match convention |
| Contensive5 | uses MSTest | keep as-is (mature, well-maintained) |

---

## Integration with Release Runner

Once test projects exist, the release runner's `test` step needs to know how to
run them. Add to `repo-catalog.yml`:

```yaml
- name: aoDesignBlocks
  test_command: "dotnet run --project server/aoDesignBlocksTests/aoDesignBlocksTests.csproj -- -filter /UnitTests"
  test_type: xunit-v3-exe    # OutputType=exe, run with 'dotnet run', not 'dotnet test'
```

The release runner checks `test_command`. If present, it runs that command. If
the repo has `has_tests: false` and no `test_command`, the step is marked `skipped`.

---

## Success Criteria

- All Tier 0 and Tier 1 repos have tests that run in the release pipeline
- All Tier 2 repos with C# code have at least a scaffold test project
- The release runner's `test` step returns a meaningful pass/fail for every repo
  with tests
- No test requires a live database connection to pass (unit tests only; integration
  tests are opt-in and run separately)

---

## Timeline Guidance

- **Phase 2A** (Tier 1): implement during the first release cycle after Phase 1 is
  complete. There is only 1 repo to scaffold (aoMCP).
- **Phase 2B** (High-value Tier 2): implement during the second release cycle.
  ~8 repos to scaffold.
- **Phase 2C** (Remaining Tier 2): implement over 2-3 release cycles. ~18 repos
  to scaffold, but each is a 10-minute task using the scaffold-tests skill.
- **Phase 2D** (Normalize): can happen in parallel with 2B/2C as each repo is touched.
