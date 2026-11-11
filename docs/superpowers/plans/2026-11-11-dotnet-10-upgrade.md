# .NET 10 Upgrade Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Retarget the EFMaterializedPath library and its tests from .NET 8 / EF Core 8.0.2 to .NET 10 / EF Core 10.0.15 and publish-ready version 10.0.15.

**Architecture:** Pure retarget: project files, CI workflow and README changelog change; no `.cs` file changes. The library stays single-targeted (`net10.0`), as it was single-targeted at `net8.0` before.

**Tech Stack:** .NET SDK 10 (10.0.115 installed locally), C# 14, EF Core 10.0.15, xUnit 2.9.3, FluentAssertions 7.2.2, GitHub Actions.

**Spec:** none — the request was "upgrade for .NET 10". Decisions made here are listed under Global Constraints.

**Dry run:** the exact edits below were applied to a scratch copy of `HEAD` (b8d3501) on 2026-11-11: `dotnet build` gave 0 warnings / 0 errors, `dotnet test` passed 34/34, `dotnet list package --vulnerable --include-transitive` reported nothing.

## Global Constraints

- Target framework: `net10.0` in both projects. No multi-targeting (EF Core 10 requires .NET 10, so `net8.0` consumers stay on package 8.0.2).
- `LangVersion`: `14`.
- EF Core packages: `10.0.15` (latest 10.x patch on 2026-11-11). Not 11.0.0.
- Package `Version` and `AssemblyVersion`: `10.0.15` — the project's convention is that the package version equals the EF Core version (8.0.2 ↔ EF 8.0.2).
- FluentAssertions: `7.2.2`, the last Apache-2.0 release. Do not go to 8.x — it is under a paid commercial licence. (A wrong patch number silently floats up to 8.0.0 with warning NU1603; treat that warning as a failure.)
- xUnit stays on v2 (`2.9.3`, final v2 release). No xunit.v3 migration.
- No `.cs` changes. If a build error seems to need one, stop and report instead.
- Work on branch `dotnet-10-upgrade`, not `master`.

## Review Focus

Not exercised by any test in this plan (tests use only the InMemory provider):

1. **`path.Contains(e.Id)` on a real relational provider** (`QueryAncestors`, `GetPathFromRootAsync`): EF Core 10 changed the default translation of parameterised collections from one JSON parameter (`OPENJSON`) to one SQL parameter per element. Results are the same; generated SQL and plan caching differ. Very deep trees (>2000 ancestors on SQL Server) would hit the parameter limit — unrealistic for this library, but it is the one behavioural change consumers can observe.
2. **`e.Path.StartsWith(prefix)` on a relational provider** (`QueryDescendants`): translation unchanged since EF 8 (`LIKE` with escaped parameter); listed only because it is the other untested translation.
3. **Consumers still on .NET 8 / EF 8**: they cannot install 10.0.15. Expected; stated in the changelog entry.
4. **NuGet package content**: `GeneratePackageOnBuild` produces `EFMaterializedPath.10.0.15.nupkg`; Task 1 Step 4 checks the file name, nothing checks its dependency group beyond that.

Adding a SQLite-backed test project to cover 1–2 is out of scope for this upgrade; raise it separately if wanted.

---

### Task 1: Retarget both projects to net10.0 and bump packages

Both projects change together: a `net8.0` test project cannot reference a `net10.0` library.

**Files:**
- Modify: `EFMaterializedPath/EFMaterializedPath.csproj`
- Modify: `EFMaterializedPath.Test/EFMaterializedPath.Test.csproj`

- [ ] **Step 1: Create the branch and confirm the baseline**

```bash
git switch -c dotnet-10-upgrade
dotnet test
```

Expected: `Passed!  - Failed: 0, Passed: 34` on `net8.0`.

- [ ] **Step 2: Edit `EFMaterializedPath/EFMaterializedPath.csproj`**

| Element | Old | New |
|---|---|---|
| `TargetFramework` | `net8.0` | `net10.0` |
| `LangVersion` | `12` | `14` |
| `AssemblyVersion` | `8.0.2` | `10.0.15` |
| `Version` | `8.0.2` | `10.0.15` |
| `PackageReleaseNotes` | `Updated framework to 8.0.2` | `Updated to .NET 10 and EF Core 10.0.15` |
| `Microsoft.EntityFrameworkCore` | `8.0.2` | `10.0.15` |
| `Microsoft.EntityFrameworkCore.Relational` | `8.0.2` | `10.0.15` |

- [ ] **Step 3: Edit `EFMaterializedPath.Test/EFMaterializedPath.Test.csproj`**

| Element | Old | New |
|---|---|---|
| `TargetFramework` | `net8.0` | `net10.0` |
| `FluentAssertions` | `6.12.0` | `7.2.2` |
| `Microsoft.EntityFrameworkCore.InMemory` | `8.0.2` | `10.0.15` |
| `Microsoft.NET.Test.Sdk` | `17.9.0` | `18.11.1` |
| `xunit` | `2.7.0` | `2.9.3` |
| `xunit.runner.visualstudio` | `2.5.7` | `3.1.5` |

- [ ] **Step 4: Build**

Run: `dotnet build`
Expected: `0 Warning(s)`, `0 Error(s)`, and a line `Successfully created package '…/EFMaterializedPath.10.0.15.nupkg'`. Any `NU1603` warning means a package version above was mistyped.

- [ ] **Step 5: Test**

Run: `dotnet test --no-build`
Expected: `Passed!  - Failed: 0, Passed: 34, Skipped: 0, Total: 34 … (net10.0)`

- [ ] **Step 6: Vulnerability check**

Run: `dotnet list package --vulnerable --include-transitive`
Expected: `has no vulnerable packages` for both projects.

- [ ] **Step 7: Commit**

```bash
git add EFMaterializedPath/EFMaterializedPath.csproj EFMaterializedPath.Test/EFMaterializedPath.Test.csproj
git commit -m "Upgrade to .NET 10 and EF Core 10.0.15"
```

### Task 2: CI workflow and changelog

**Files:**
- Modify: `.github/workflows/dotnet.yml`
- Modify: `README.md` (the `## Changelog` section, currently line 388)

- [ ] **Step 1: Edit `.github/workflows/dotnet.yml`**

| Line | Old | New |
|---|---|---|
| checkout | `actions/checkout@v2` | `actions/checkout@v5` |
| setup | `actions/setup-dotnet@v1` | `actions/setup-dotnet@v5` |
| SDK | `dotnet-version: 8.0.x` | `dotnet-version: 10.0.x` |

The restore / build / test steps stay as they are.

- [ ] **Step 2: Add a changelog entry to `README.md`**

Insert directly under the `## Changelog` heading, above the `### 1.0.8 (28.08.2021)` entry (use the actual release date):

````markdown
### 10.0.15 (11.11.2026)
Upgraded to .NET 10. Requires .NET 10 and EF Core 10; projects on .NET 8 should stay on 8.0.2.
```
Target framework net8.0 -> net10.0
Microsoft.EntityFrameworkCore 8.0.2 -> 10.0.15
Microsoft.EntityFrameworkCore.Relational 8.0.2 -> 10.0.15
```
````

- [ ] **Step 3: Verify exactly what CI runs**

```bash
dotnet restore
dotnet build --no-restore
dotnet test --no-build --verbosity normal
```

Expected: build succeeds with 0 warnings; 34 tests pass.

- [ ] **Step 4: Commit**

```bash
git add .github/workflows/dotnet.yml README.md
git commit -m "CI on .NET 10, changelog for 10.0.15"
```

- [ ] **Step 5: Push and confirm the workflow is green**

Push the branch and open a PR against `master` (only with the maintainer's go-ahead); the `.NET` workflow must pass on `ubuntu-latest` with SDK 10.0.x. Publishing the package to nuget.org is a manual step for the maintainer and is not part of this plan.
