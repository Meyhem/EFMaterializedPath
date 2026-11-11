# Integration Tests Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Run the existing `TreeRepository` test suite, plus five relational-only checks, against SQLite, PostgreSQL and SQL Server, locally and in GitHub Actions.

**Architecture:** `TreeRepositoryTestBase` gets a pluggable `ITestDatabase` (default: InMemory). A new `EFMaterializedPath.IntegrationTest` project references the unit test project and re-runs every existing test class through one-line subclasses per provider. Postgres and SQL Server run in Testcontainers (one container per provider per run, shared via xUnit collection fixtures, a fresh database per test); SQLite runs in-process.

**Tech Stack:** .NET 10, xUnit v2, FluentAssertions 7.2.2, Testcontainers 4.16.0, EF Core providers SQLite / SqlServer 10.0.15, Npgsql 10.0.x.

**Spec:** agreed in chat on 11.11.2026 (no separate design doc). Summary: existing suite on 4 providers; extra checks = (1) schema default + indexes from `MaterializedEntityMapping`, (2) ids sharing leading digits, (3) Guid ids, (4) root-first order of `GetPathFromRootAsync`, (5) moving a ~50-level subtree; Docker tests tagged `Category=Docker`; new CI job; docs updated. Library failures are reported to the maintainer, never fixed in this work.

## Global Constraints

- No change to anything in `EFMaterializedPath/` (public API, mapping, package dependencies). If a test exposes a library bug: stop, report it, mark that test `Skip = "Known issue: …"` only with the maintainer's agreement.
- FluentAssertions stays 7.2.2. xUnit stays v2 (2.9.3).
- EF Core provider packages pinned to 10.0.15 where available (`Sqlite`, `SqlServer`); Npgsql: latest 10.0.x.
- `dotnet build` must stay at 0 warnings.
- Integration project: `IsPackable=false`, `net10.0`.
- Container images pinned: `postgres:17-alpine`, `mcr.microsoft.com/mssql/server:2022-latest`.
- Plain `dotnet test --filter Category!=Docker` must pass on a machine without Docker.

## Review Focus

1. Explicit ids on SQL Server identity columns: inserting `Id = 1..10` fails unless the test model sets `ValueGeneratedNever()` — handled in Task 1's `TestDbContext`.
2. `Path` inserted as `null` on a NOT NULL column: relies on `HasDefaultValue("")` being applied by the database — pinned by the schema check in Task 3.
3. Moving a *root* node that has descendants (`oldPath == ""` → `string.Replace("", …)`): suspected library bug on every provider — probed in Task 3, reported, not fixed.
4. SQL Server indexes `Path` as `nvarchar(450)`: Guid paths (37 chars per level) overflow after ~12 levels — probed in Task 3's Guid test, reported.
5. Traits on derived classes must apply to inherited `[Fact]`s, or the `Category!=Docker` filter silently runs Docker tests — verified with `--list-tests` in Task 2.

---

### Task 1: Pluggable database in the unit test base

**Files:**
- Create: `EFMaterializedPath.Test/TestUtils/ITestDatabase.cs`, `EFMaterializedPath.Test/TestUtils/InMemoryTestDatabase.cs`, `EFMaterializedPath.Test/TestUtils/GuidCategory.cs`
- Modify: `EFMaterializedPath.Test/TreeRepositoryTestBase.cs`, `EFMaterializedPath.Test/TestUtils/TestDbContext.cs`

**Interfaces:**
- Produces: `public interface ITestDatabase { Task<TestDbContext> CreateContextAsync(); Task DropAsync(TestDbContext context); }`
- Produces: `InMemoryTestDatabase.Instance` (uses `TestHelpers.CreateTestDb()`; `DropAsync` disposes).
- Produces: `TreeRepositoryTestBase.Database` — `protected virtual ITestDatabase Database => InMemoryTestDatabase.Instance;`; `InitializeAsync` creates the context through it, `DisposeAsync` calls `Database.DropAsync(DbContext)`.
- Produces: `TestDbContext` applies `MaterializedEntityMapping<Category,int>` and `<GuidCategory,Guid>`, both ids `ValueGeneratedNever()`; adds `DbSet<GuidCategory> GuidCategories`.
- Produces: `GuidCategory : IMaterializedPathEntity<Guid>` (same shape as `Category`).

- [ ] **Step 1:** Add the files/changes above.
- [ ] **Step 2:** Run `dotnet build && dotnet test` — expect 0 warnings and the same passing test count as before (pure refactor).
- [ ] **Step 3:** Commit `test: make test database pluggable`.

### Task 2: Integration project, providers, re-run of the existing suite

**Files:**
- Create: `EFMaterializedPath.IntegrationTest/EFMaterializedPath.IntegrationTest.csproj` (refs: library, `EFMaterializedPath.Test`, the packages above), `GlobalUsings.cs`
- Create: `Databases/SqliteTestDatabase.cs`, `Databases/PostgresFixture.cs`, `Databases/SqlServerFixture.cs`, `Databases/Collections.cs`
- Create: `Suites/SqliteSuite.cs`, `Suites/PostgresSuite.cs`, `Suites/SqlServerSuite.cs`
- Modify: `EFMaterializedPath.sln`

**Interfaces:**
- Consumes: `ITestDatabase`, `TreeRepositoryTestBase.Database`.
- Produces: `IRelationalTestDatabase : ITestDatabase { string IndexedColumnsSql { get; } }` — SQL returning a single `Value` column with every column covered by a non-primary index on `Categories`.
- Produces: `SqliteTestDatabase` (open `DataSource=:memory:` connection per context, `EnsureCreated`), `PostgresFixture` / `SqlServerFixture` (`IAsyncLifetime`, start container once; `CreateContextAsync` uses a unique database name `t_<guid:N>` and `EnsureCreatedAsync`; `DropAsync` → `EnsureDeletedAsync` + dispose).
- Produces: collections `"Postgres"` and `"SqlServer"` (`ICollectionFixture<…>`) with constants `PostgresCollection.Name`, `SqlServerCollection.Name`.
- Suites: for each of the 12 `TestTreeRepository_*` classes one subclass per provider, e.g.
  `[Collection(PostgresCollection.Name), Trait("Category", "Docker")] public class Postgres_QueryDescendants(PostgresFixture db) : TestTreeRepository_QueryDescendants { protected override ITestDatabase Database => db; }`. SQLite suites carry no trait.

- [ ] **Step 1:** Create project and providers, add to solution.
- [ ] **Step 2:** `dotnet test EFMaterializedPath.IntegrationTest --list-tests --filter Category!=Docker` — expect only `Sqlite_*` tests listed (Review Focus 5).
- [ ] **Step 3:** `dotnet test EFMaterializedPath.IntegrationTest` — expect every inherited test to pass on all 3 providers; any failure is reported, not fixed.
- [ ] **Step 4:** Commit `test: run existing suite on SQLite, PostgreSQL and SQL Server`.

### Task 3: Relational-only checks

**Files:**
- Create: `EFMaterializedPath.IntegrationTest/Checks/SchemaChecks.cs`, `Checks/PathChecks.cs`, `Checks/GuidIdChecks.cs` (abstract, `protected abstract IRelationalTestDatabase Database { get; }`, own `IAsyncLifetime`)
- Modify: the three `Suites/*.cs` to add one subclass per check class per provider.

Tests (exact assertions):
- `SchemaChecks.PathDefaultsToEmptyString` — add `Category { Id = 1 }` with `Path = null`, save, `ChangeTracker.Clear()`, reload → `Path == ""`.
- `SchemaChecks.PathAndParentIdAreIndexed` — `Database.SqlQueryRaw<string>(IndexedColumnsSql)` contains `"Path"` and `"ParentId"`.
- `PathChecks.DescendantsDoNotMatchIdsWithSamePrefix` — nodes 1, 11, 12; 2 under 1; 21 under 11; 3 under 12 → `QueryDescendants(1)` ids `== {2}`, `QueryDescendants(11)` `== {21}`, `QueryChildren(1)` `== {2}`, `QueryAncestors(21)` `== {11}`.
- `PathChecks.PathFromRootIsRootFirst` — chain 30 → 20 → 10 (root 30) → `GetPathFromRootAsync(10)` ids in order `[30, 20]`.
- `PathChecks.MovingDeepSubtreeUpdatesAllDescendants` — roots 1 and 2; chain ids 100..149 hung under 1 (100 under 1, 101 under 100 …); move 100 under 2; `ChangeTracker.Clear()`; every node 100+k has `Path` starting `"|2|100|"` (k>0), `Level == k + 1`, and `QueryDescendants(2)` count `== 50`.
- `GuidIdChecks.QueriesWorkWithGuidIds` — tree root → a → b with `Guid.NewGuid()` ids and `GuidIdentifierSerializer`: `QueryDescendants(root)` count 2, `GetPathFromRootAsync(b)` `[root, a]`, after `SetParentAsync(b, root)` `b.Path == $"|{root.Id}|"`.
- Probe (Review Focus 3, 4), not committed as tests unless they pass: move a root with children; a 15-level Guid chain on SQL Server.

- [ ] **Step 1:** Write the checks and provider subclasses.
- [ ] **Step 2:** `dotnet test EFMaterializedPath.IntegrationTest` — expect all pass on 3 providers.
- [ ] **Step 3:** Run probes, record outcomes for the maintainer.
- [ ] **Step 4:** Commit `test: add relational-only integration checks`.

### Task 4: CI and docs

**Files:**
- Modify: `.github/workflows/dotnet.yml` — existing job's test step gets `--filter "Category!=Docker"`; new job `integration` (`ubuntu-latest`, same checkout/setup-dotnet/restore/build) running `dotnet test --no-build --filter "Category=Docker" --verbosity normal`.
- Modify: `CLAUDE.md` (Build & test: both commands; replace the InMemory-only / manual SQLite rule with "integration tests cover SQLite, PostgreSQL, SQL Server").
- Modify: `README.md` — short "Running tests" section (table of contents link too). No changelog entry: nothing user-visible ships.

- [ ] **Step 1:** Edit files.
- [ ] **Step 2:** `dotnet build` (0 warnings), `dotnet test --filter "Category!=Docker"`, `dotnet test --filter "Category=Docker"` — all pass.
- [ ] **Step 3:** Commit `ci: run integration tests against PostgreSQL and SQL Server`.
