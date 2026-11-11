# API Cleanup and Tree Validation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Fix the nullable element type of `Query()`/`QueryRoots()`, make `DetachNodeAsync`/`RemoveNodeAsync` save once, add `ValidateAsync`, `IsAncestorOf`/`IsDescendantOf`, and make `IntIdentifierSerializer` culture-invariant.

**Architecture:** Every change lives in `TreeRepository` / `ITreeRepository` / `IdentifierSerializers.cs`, using the existing private `ParsePath`/`FormatPath`/`TrackedInstanceOf` helpers. New validation results are a small public record + enum in a new file. Tests are added to the existing InMemory test classes (re-run on SQLite/PostgreSQL/SQL Server by the integration suites), and `FailureChecks` is rewritten for the new single-save behaviour.

**Tech Stack:** .NET 10, EF Core 10.0.15, xUnit v2, FluentAssertions 7.2.2, Testcontainers.

**Spec:** the maintainer's request in chat (11.11.2026): "optimize both [nullable return, DetachNodeAsync], even with the breaking change; add ValidateAsync, IsAncestorOf / IsDescendantOf, and the serializer culture fix". Project rules: `CLAUDE.md`.

## Global Constraints

- Breaking changes are approved by the maintainer for this release; each one gets a **Breaking:** line with migration instructions in the changelog.
- All changelog text goes into the existing `### Unreleased` entry in `README.md` (10.0.15 is already released). Also update `<PackageReleaseNotes>` in `EFMaterializedPath/EFMaterializedPath.csproj`. Do not change `<Version>`/`<AssemblyVersion>`.
- No database shape changes: `MaterializedEntityMapping` and the path format `|1|2|3|` stay as they are.
- `dotnet build` must finish with 0 warnings.
- XML doc comments on every new or changed public member.
- FluentAssertions stays on 7.2.2. Never publish to nuget.org.
- LINQ changes must pass `dotnet test` (all providers, Docker running), not only `--filter "Category!=Docker"`.
- Precondition: the current uncommitted work on `master` is committed (or stashed into its own branch) by the maintainer before Task 1; work happens on a new branch `api-cleanup-validation`.

## Review Focus

- Detach/Remove with an entity loaded `AsNoTracking()` must still persist all changes (existing behaviour, now on the new code path) → `DetachUntrackedNode` test in Task 2.
- Detach when the context already tracks a *different* instance of the same row → both instances end up as roots → `DetachUpdatesTrackedDuplicate` test in Task 2.
- A failure of the single save without a transaction must leave the database exactly as before → rewritten `FailureChecks` in Task 2.
- `ValidateAsync` on InMemory, where never-moved roots have a `null` Path, must report nothing → `ValidTreeHasNoProblems` in Task 3 (runs on InMemory first).
- `IsAncestorOf` on a root node (empty path) and on the node itself must return `false`, not throw → tests in Task 4.

---

### Task 1: `Query()` / `QueryRoots()` return non-nullable elements

**Files:**
- Modify: `EFMaterializedPath/ITreeRepository.cs:21,27` (`QueryRoots`, `Query`)
- Modify: `EFMaterializedPath/TreeRepository.cs:24-32`
- Modify: `EFMaterializedPath.Test/TestTreeRepository_RandomOperations.cs:170` (drop `c!` → `c`)
- Test: `EFMaterializedPath.Test/TestTreeRepository_Query.cs`
- Docs: `README.md` (`### Unreleased`), `EFMaterializedPath.csproj` release notes

**Interfaces:**
- Produces: `IQueryable<TEntity> QueryRoots()`, `IQueryable<TEntity> Query()` on `ITreeRepository<TEntity, TId>` and `TreeRepository<TDbContext, TEntity, TId>`.

- [ ] **Step 1: Write the failing test** in `TestTreeRepository_Query.cs`

```csharp
[Theory]
[InlineData(nameof(ITreeRepository<Category, int>.Query))]
[InlineData(nameof(ITreeRepository<Category, int>.QueryRoots))]
public void ReturnsNonNullableElements(string method)
{
    // Open generic definition: nullability annotations live there, not on the constructed type.
    var returnParameter = typeof(ITreeRepository<,>).GetMethod(method)!.ReturnParameter;
    var info = new System.Reflection.NullabilityInfoContext().Create(returnParameter);
    info.GenericTypeArguments.Single().ReadState.Should().Be(System.Reflection.NullabilityState.NotNull);
}
```

- [ ] **Step 2: Run it, expect FAIL** (`ReadState` is `Nullable`)

Run: `dotnet test EFMaterializedPath.Test --filter "FullyQualifiedName~TestTreeRepository_Query.ReturnsNonNullableElements"`

- [ ] **Step 3: Change both return types to `IQueryable<TEntity>`** in the interface and the class; update the XML `<returns>` of `Query()` (currently empty) to "All nodes, unordered".

- [ ] **Step 4: Run `dotnet build` (0 warnings) and the test, expect PASS**

- [ ] **Step 5: Changelog** — add to `### Unreleased`:
  "**Breaking:** `Query()` and `QueryRoots()` now return `IQueryable<TEntity>` instead of `IQueryable<TEntity?>`; rows were never null. Compiled code keeps working (only nullability annotations changed). Remove `!` / `?` you added to work around it; classes implementing `ITreeRepository` must change these two signatures." Append a short clause to the csproj `<PackageReleaseNotes>`.

- [ ] **Step 6: Commit** — `git commit -m "Query/QueryRoots return non-nullable elements"`

---

### Task 2: `DetachNodeAsync` and `RemoveNodeAsync` save once

**Files:**
- Modify: `EFMaterializedPath/TreeRepository.cs` (`DetachNodeAsync`, `RemoveNodeAsync`, new private helper)
- Modify: `EFMaterializedPath/ITreeRepository.cs` (XML remarks of both methods)
- Test: `EFMaterializedPath.Test/TestTreeRepository_DetachNodeAsync.cs`, `EFMaterializedPath.Test/TestTreeRepository_RemoveNodeAsync.cs`
- Modify: `EFMaterializedPath.IntegrationTest/Checks/FailureChecks.cs`
- Docs: `README.md` (Considerations paragraph about non-atomic detach, `DetachNodeAsync`/`RemoveNodeAsync` sections, `### Unreleased`), csproj release notes

**Interfaces:**
- Consumes: existing private `ParsePath`, `FormatPath`, `TrackedInstanceOf`, `QueryDescendants`.
- Produces: private `Task<TEntity> DetachWithoutSavingAsync(TEntity entity)` returning the tracked instance (used by `RemoveNodeAsync` to `Set.Remove` it).

- [ ] **Step 1: Write failing tests** in `TestTreeRepository_DetachNodeAsync.cs` (count saves with the `DbContext.SavedChanges` event, works on every provider):

```csharp
[Fact]
public async Task SavesOnce()
{
    var saves = 0;
    DbContext.SavedChanges += (_, _) => saves++;
    await Repository.DetachNodeAsync(await DbContext.Categories.FindAsync(2));
    saves.Should().Be(1);
}

[Fact]
public async Task DetachInnerNode()
{
    await Repository.DetachNodeAsync(await DbContext.Categories.FindAsync(2));
    DbContext.ChangeTracker.Clear();
    var rows = await DbContext.Categories.AsNoTracking().ToDictionaryAsync(c => c.Id);
    rows[2].Should().BeEquivalentTo(new { ParentId = (int?)null, Level = 0, Path = "" });
    rows[5].Should().BeEquivalentTo(new { ParentId = (int?)1, Level = 1, Path = "|1|" });
    rows[9].Should().BeEquivalentTo(new { ParentId = (int?)5, Level = 2, Path = "|1|5|" });
    rows[7].Should().BeEquivalentTo(new { ParentId = (int?)9, Level = 3, Path = "|1|5|9|" });
    rows[10].Path.Should().Be("|1|6|");
}

[Fact]
public async Task DetachUntrackedNode()   // two loaded AsNoTracking(); same expectations as DetachInnerNode for 2, 5, 7

[Fact]
public async Task DetachUpdatesTrackedDuplicate()
// tracked = FindAsync(2); untracked = AsNoTracking().SingleAsync(Id == 2); DetachNodeAsync(untracked);
// both tracked and untracked: ParentId null, Level 0, Path ""; stored row 5 has Path "|1|".
```

  In `TestTreeRepository_RemoveNodeAsync.cs` add `SavesOnce` (same pattern, `RemoveNodeAsync(two)`, expect `1`).

- [ ] **Step 2: Run, expect `SavesOnce` tests FAIL** (detach of 2 saves 3 times, remove 4 times); the others may already pass.

Run: `dotnet test EFMaterializedPath.Test --filter "FullyQualifiedName~DetachNodeAsync|FullyQualifiedName~RemoveNodeAsync"`

- [ ] **Step 3: Implement `DetachWithoutSavingAsync`**, then `DetachNodeAsync` = helper + one `SaveChangesAsync`; `RemoveNodeAsync` = helper, `Set.Remove(tracked)`, one `SaveChangesAsync`. Algorithm:

```text
assert stored; tracked = TrackedInstanceOf(entity)
k = ParsePath(entity.Path).Count            // position of entity.Id in every descendant path
for each d in await QueryDescendants(entity).ToListAsync():   // materialize before modifying
    ids = ParsePath(d.Path); ids.RemoveAt(k)
    d.Path = FormatPath(ids); d.Level = ids.Count
    if d.ParentId == entity.Id: d.ParentId = entity.ParentId
for target in {entity, tracked}.Distinct(): Path = "", Level = 0, ParentId = null
return tracked
```

  Update XML remarks on both interface methods: "saves the underlying context once"; drop "has to update paths of all descendants" wording only if it becomes untrue (it stays true — keep it).

- [ ] **Step 4: Run unit tests, expect PASS**: `dotnet test EFMaterializedPath.Test`

- [ ] **Step 5: Rewrite `FailureChecks`** for single-save behaviour:
  - Replace theory `FailedSaveDuringDetachLeavesConsistentTree` with `[Fact] FailedSaveDuringDetachChangesNothing`: `FailOnSave(1)`, detach throws `InjectedFailureException`, `StoredTreeAsync()` equals `before` with strict ordering.
  - Add `[Fact] FailedSaveDuringRemoveChangesNothing`: same with `RemoveNodeAsync`, no transaction.
  - `FailedSaveDuringRemoveInsideTransactionRollsBackEverything`: change to `[Fact]` with `FailOnSave(1)` (saves 3 and 5 no longer happen).
  - Update the class summary ("detach and remove save once per child" → "detach and remove save once").

- [ ] **Step 6: Run everything** (Docker running): `dotnet test` → all PASS on InMemory, SQLite, PostgreSQL, SQL Server.

- [ ] **Step 7: Docs**
  - README Considerations: replace "Without one, `DetachNodeAsync` and `RemoveNodeAsync` are not atomic: they save once per child…" with "`SetParentAsync`, `DetachNodeAsync` and `RemoveNodeAsync` each save once, so each is atomic on its own; use a transaction to combine several of them with your own changes."
  - `### Unreleased`: "**Breaking (behaviour):** `DetachNodeAsync` and `RemoveNodeAsync` now call `SaveChanges` once instead of once per child (plus once more for the node, and once for the delete). A failure now leaves the tree unchanged instead of partly moved. Affects code that counts saves, e.g. `SaveChanges` interceptors or audit logs expecting one event per child; no signature changes." Add a clause to the csproj release notes.

- [ ] **Step 8: Commit** — `git commit -m "DetachNodeAsync and RemoveNodeAsync save once"`

---

### Task 3: `ValidateAsync`

**Files:**
- Create: `EFMaterializedPath/TreeProblem.cs`
- Modify: `EFMaterializedPath/ITreeRepository.cs`, `EFMaterializedPath/TreeRepository.cs`
- Create test: `EFMaterializedPath.Test/TestTreeRepository_ValidateAsync.cs`
- Modify: `EFMaterializedPath.IntegrationTest/Suites/SqliteSuite.cs`, `PostgresSuite.cs`, `SqlServerSuite.cs` (one subclass each, Postgres/SQL Server with the same `[Collection(...), Trait(Traits.Category, Traits.Docker)]` attributes as their neighbours)
- Docs: README (new `### ValidateAsync` under READ API + Contents link, `### Unreleased`), csproj release notes

**Interfaces:**
- Produces (in `TreeProblem.cs`, namespace `EFMaterializedPath`):

```csharp
public enum TreeProblemKind { MissingParent, Cycle, WrongPath, WrongLevel }
public sealed record TreeProblem<TId>(TId Id, TreeProblemKind Kind, string Message) where TId : struct;
```

- Produces on `ITreeRepository<TEntity, TId>`: `Task<IReadOnlyList<TreeProblem<TId>>> ValidateAsync();`

- [ ] **Step 1: Write failing tests** in `TestTreeRepository_ValidateAsync.cs : TreeRepositoryTestBase`. Corrupt rows by setting properties on tracked entities and calling `DbContext.SaveChangesAsync()` directly; at the end of each corrupting test restore the row (base `DisposeAsync` asserts the tree is valid).

| Test | Corruption | Expected result |
|---|---|---|
Only the node at fault is reported, never its descendants (each test asserts the exact list):

| Test | Corruption | Expected result (exact) |
|---|---|---|
| `ValidTreeHasNoProblems` | none (InMemory: root 1 has a `null` Path) | empty |
| `ReportsMissingParent` | node 9 `ParentId = 999` | `(9, MissingParent)` — 7 below it not reported |
| `ReportsWrongPath` | node 9 `Path = "|1|9|"` | `(9, WrongPath)` |
| `ReportsWrongLevel` | node 5 `Level = 7` | `(5, WrongLevel)` |
| `ReportsNonEmptyRoot` | node 1 `Path = "|3|"` | `(1, WrongPath)` |
| `ReportsSelfParent` | node 3 `ParentId = 3` | `(3, Cycle)` |
| `ReportsCycle` | node 2 `ParentId = 5` | `(2, Cycle)`, `(5, Cycle)` — 6, 7, 9, 10 not reported |

- [ ] **Step 2: Run, expect FAIL** (does not compile: `ValidateAsync` missing).

- [ ] **Step 3: Implement.** Load `Id, ParentId, Path, Level` with `AsNoTracking()` and a projection, into a dictionary by id. Derive each node's *canonical* ancestor list from `ParentId` alone (root → empty; otherwise canonical(parent) + parent.Id), memoized and **iterative** (SQLite trees can be deeper than a safe recursion depth). While walking up:
  - parent id not in the table → `MissingParent` on the node whose `ParentId` points nowhere;
  - a node seen twice on the current walk → `Cycle` on every node of that loop;
  - nodes whose chain ends in one of those two are unresolvable and are not reported.
  For each resolvable node compare stored Path (`null` treated as `""`) with `FormatPath(canonical)` → `WrongPath`, and stored Level with `canonical.Count` → `WrongLevel`. `Message` names the expected and stored value. Order results by Id, then Kind.
  XML doc: what is checked, that it reads the whole table into memory in one query, and that it never modifies data.

- [ ] **Step 4: Run `dotnet test --filter "Category!=Docker"`, then add the three suite subclasses and run `dotnet test`** → PASS everywhere.

- [ ] **Step 5: Docs** — README section with a short sample (`var problems = await repository.ValidateAsync();`), `### Unreleased`: "Added `ValidateAsync`, which reports nodes whose `Path`, `Level` or `ParentId` are inconsistent. **Breaking** for classes implementing `ITreeRepository`: they must add this member." csproj release notes.

- [ ] **Step 6: Commit** — `git commit -m "Add ValidateAsync"`

---

### Task 4: `IsAncestorOf` / `IsDescendantOf`

**Files:**
- Modify: `EFMaterializedPath/ITreeRepository.cs`, `EFMaterializedPath/TreeRepository.cs`
- Create test: `EFMaterializedPath.Test/TestTreeRepository_IsAncestorOf.cs`
- Modify: the three `Suites/*Suite.cs` files (one subclass each)
- Docs: README (new sections + Contents links, `### Unreleased`), csproj release notes

**Interfaces:**
- Produces on `ITreeRepository<TEntity, TId>`:
  - `bool IsAncestorOf(TEntity ancestor, TEntity node);` — true when `node`'s path contains `ancestor.Id`; strict (a node is not its own ancestor); no database query, uses the paths as loaded.
  - `bool IsDescendantOf(TEntity node, TEntity ancestor);` — `IsAncestorOf(ancestor, node)`.
  - Both throw like the other members: `ArgumentNullException` for null, `InvalidOperationException` for an entity without id.

- [ ] **Step 1: Write failing tests** (standard tree from the base class):
  - `IsAncestorOf(1, 7)`, `(2, 7)`, `(9, 7)` → true
  - `IsAncestorOf(7, 2)`, `(3, 7)`, `(6, 5)` → false (reverse direction, other branch, sibling)
  - `IsAncestorOf(1, 1)`, `IsAncestorOf(7, 1)` (root node with empty/null path) → false
  - `IsDescendantOf(7, 1)` → true, `IsDescendantOf(1, 7)` → false
  - `ThrowsOnNonStoredEntity`: null → `ArgumentNullException`, `new Category()` → `InvalidOperationException`, for both arguments of both methods

- [ ] **Step 2: Run, expect FAIL** (does not compile).
- [ ] **Step 3: Implement** using `ParsePath(node.Path).Contains(ancestor.Id)` after `AssertIsStoredEntity` on both.
- [ ] **Step 4: Add the suite subclasses; run `dotnet test`** → PASS.
- [ ] **Step 5: Docs** — README sections with the standard tree diagram, `### Unreleased`: "Added `IsAncestorOf` and `IsDescendantOf`, checked from the loaded paths without a query. **Breaking** for classes implementing `ITreeRepository`." csproj release notes.
- [ ] **Step 6: Commit** — `git commit -m "Add IsAncestorOf and IsDescendantOf"`

---

### Task 5: Culture-invariant `IntIdentifierSerializer`

**Files:**
- Modify: `EFMaterializedPath/IdentifierSerializers.cs`
- Create test: `EFMaterializedPath.Test/TestIdentifierSerializers.cs` (plain class, not a `TreeRepositoryTestBase`; no suite subclasses needed — it touches no database)
- Docs: `### Unreleased`, csproj release notes

**Interfaces:**
- Produces: unchanged signatures; `IntIdentifierSerializer` uses `CultureInfo.InvariantCulture` for `ToString` and `int.Parse`. `GuidIdentifierSerializer` is already culture-invariant: make its format explicit with `ToString("D")`, no behaviour change. `StringIdentifierSerializer` unchanged.

- [ ] **Step 1: Write failing test**

```csharp
[Fact]
public void IntSerializerIgnoresCurrentCulture()
{
    var original = CultureInfo.CurrentCulture;
    var culture = (CultureInfo)CultureInfo.InvariantCulture.Clone();
    culture.NumberFormat.NegativeSign = "~";
    try
    {
        CultureInfo.CurrentCulture = culture;
        var serializer = new IntIdentifierSerializer();
        serializer.SerializeIdentifier(-5).Should().Be("-5");
        serializer.DeserializeIdentifier("-5").Should().Be(-5);
    }
    finally { CultureInfo.CurrentCulture = original; }
}

[Fact]
public void GuidSerializerRoundTripsInDFormat()   // Guid g; Serialize(g) == g.ToString("D"); Deserialize(Serialize(g)) == g
```

- [ ] **Step 2: Run, expect `IntSerializerIgnoresCurrentCulture` FAIL** (`"~5"`).
- [ ] **Step 3: Implement** with `CultureInfo.InvariantCulture`.
- [ ] **Step 4: Run `dotnet test --filter "Category!=Docker"`** → PASS.
- [ ] **Step 5: Changelog** — "Fixed `IntIdentifierSerializer` depending on the current culture: under a culture with a non-ASCII negative sign, negative ids were stored in a different form. Only affects negative `int` ids." csproj release notes.
- [ ] **Step 6: Commit** — `git commit -m "IntIdentifierSerializer: use invariant culture"`

---

### Final check

- [ ] `dotnet build` → 0 warnings; `dotnet test` (Docker running) → all green on InMemory, SQLite, PostgreSQL, SQL Server.
- [ ] README: Contents links for the new sections; every new sample compiles against the API; `### Unreleased` lists all five changes with their **Breaking** lines; framework block still "No framework or dependency changes".
