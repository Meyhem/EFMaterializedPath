# EFMaterializedPath
![Tests](https://github.com/Meyhem/EFMaterializedPath/actions/workflows/dotnet.yml/badge.svg)
[![GitHub license](https://img.shields.io/github/license/Meyhem/EFMaterializedPath.svg)](https://github.com/Meyhem/EFMaterializedPath/blob/master/LICENSE)
[![Nuget](https://img.shields.io/nuget/dt/EFMaterializedPath)](https://www.nuget.org/packages/EFMaterializedPath/)

A simple repository library that provides easy way how to store tree hierarchies in Entity framework

## Contents
* [Installation](#installation)
* [Considerations](#considerations)
* [Usage](#usage)
* [ITreeRepository READ API](#itreerepository-read-api)
* * [GetByIdAsync](#getbyidasync)
* * [GetParentAsync](#getparentasync)
* * [GetPathFromRootAsync](#getpathfromrootasync)
* * [Query](#query)
* * [QueryRoots](#queryroots)
* * [QueryAncestors](#queryancestors)
* * [QueryDescendants](#querydescendants)
* * [QuerySiblings](#querysiblings)
* * [QueryChildren](#querychildren)
* * [IsAncestorOf / IsDescendantOf](#isancestorof--isdescendantof)
* * [ValidateAsync](#validateasync)
* [ITreeRepository WRITE API](#itreerepository-write-api)
* * [SetParentAsync](#setparentasync)
* * [DetachNodeAsync](#detachnodeasync)
* * [RemoveNodeAsync](#removenodeasync)
* [Identifier types](#identifier-types) 
* [Running tests](#running-tests)
* [Changelog](#changelog)

## Installation
```
dotnet add package EFMaterializedPath
```

| Package version | Target framework | EF Core |
|-----------------|------------------|---------|
| 10.0.x          | .NET 10          | 10.0.x  |
| 8.0.x           | .NET 8           | 8.0.x   |
| 1.0.x           | .NET 5           | 5.0.x   |

Pick the package version matching your EF Core major version.

## Considerations
There are multiple approaches to storing hierarchical data in SQL tables, each having pros/cons.
Materialized path is viable in case you have hierarchy that is often read, but rarely written. 
E.g. product catalogue, categories...

It's not possible to ensure referential integrity by SQL constraints, and it's
solely dependent on correct usage of TreeRepository. In case of manual tampering
with IMaterializedPathEntity properties outside TreeRepository, it's likely you will
end up with inconsistent tree. Therefore always rely on using TreeRepository when
changing tree hierarchy. 

To ensure consistency, TreeRepository often saves underlying context when manipulating
tree hierarchy. To be able to contain these saves as an atomic operation along with your DB updates
**use transactions**. `SetParentAsync`, `DetachNodeAsync` and `RemoveNodeAsync` each save once, so each of them is
atomic on its own; a transaction is what makes several of them, together with your own changes, atomic as a whole.

TreeRepository trusts the `Path` of the entities you pass in and has no concurrency check. If another context or
process changed the tree after you loaded an entity, using that outdated entity as the node or the parent of a write
operation stores wrong paths. Load (or reload) the entities in the same context, and ideally the same transaction,
right before changing the tree.

### Database limits
These follow from the schema `MaterializedEntityMapping` creates (an indexed `Path` column) and from how each
database compares text. They are covered by the integration tests.

| | SQL Server | PostgreSQL | SQLite |
|---|---|---|---|
| `Path` column type | `nvarchar(450)` | `text` | `TEXT` |
| Longest path | 450 characters | about 2700 bytes (btree index entry limit) | no limit |
| Deepest level with `Guid` ids (37 characters per level) | 12 | 72 | no limit |
| `QueryDescendants` uses the `Path` index | yes | no, full table scan | no, full table scan |
| `QueryChildren` / `QuerySiblings` use the `Path` index | yes | yes | yes |

* **Depth.** A path holds one serialized id per ancestor, so the depth limit depends on how long your ids are:
  with `int` ids below 1 000 000 SQL Server allows about 64 levels. Moving a node deeper than the limit fails with
  `DbUpdateException` and stores nothing. On SQL Server the 450 comes from EF Core's default size for indexed
  string columns.
* **Descendant queries on PostgreSQL** scan the table unless the database or the `Path` column uses the `"C"`
  collation (`builder.Property(e => e.Path).UseCollation("C")`), or you add an index with `text_pattern_ops`:
  `CREATE INDEX ... ON "Categories" ("Path" text_pattern_ops)`.
* **Descendant queries on SQLite** scan the table unless you add an index with `COLLATE NOCASE` on `Path`, or run
  `PRAGMA case_sensitive_like = ON` on the connection.
* **Custom identifier serializers** must not produce `|`, and two ids must not serialize to texts that differ only
  by letter case: SQL Server (default collation) and SQLite (`LIKE`) compare paths ignoring case and would mix the
  two subtrees up. The built-in `int` and `Guid` serializers are safe. `%`, `_`, `[` and `\` are safe.

## Usage
### 1. Create entity implementing IMaterializedPathEntity
```c#
public class Category : IMaterializedPathEntity<int>
{
    /* Props from IMaterializedPathEntity */
    public int Id { get; set; }
    public string Path { get; set; }
    public int Level { get; set; }
    public int? ParentId { get; set; }
    
    /* Custom props */
    public string Name { get; set; }
}
```

### 2. Register it in your DbContext
```c#
public class MyDbContext : DbContext
{
    // Register as DbSet
    public DbSet<Category> Categories { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        // Apply prepared configuration
        modelBuilder.ApplyConfiguration(new MaterializedEntityMapping<Category, int>());
    }
}
```

### 3. Register ITreeRepository IoC & use repository 
```c#
public void ConfigureServices(IServiceCollection services)
{
    services.AddDbContext<MyDbContext>();
    // This registers ITreeRepository<Category, int> for you to use
    services.AddTreeRepository<MyDbContext, Category, int>();
    // This adds serializers for the default identifier types (int, Guid)
    services.AddDefaultIdentifierSerializers();
}
```
**or** create your own instance
```c#
var repo = new TreeRepository<MyDbContext, Category, int>(
    myDbContextInstance, 
    new IntIdentifierSerializer()
);
```

## ITreeRepository READ API


### GetByIdAsync
Gets node by PK
```c#
        ┌───────1───────┐   
        │       │       │
    ┌───2───┐   3       4
    │       │           │
    5       6           8
    │       │
    9       10
    │
    7
    
    await repository.GetByIdAsync(1)    
    // will yield node 1
```

### GetParentAsync
Gets parent of queried entity
```c#
        ┌───────1───────┐   
        │       │       │
    ┌───2───┐   3       4
    │       │           │
    5       6           8
    │       │
    9       10
    │
    7
    
    var node = await dbContext.Categories.FindAsync(2);
    var parentOfTwo = await repository.GetParentAsync(node);
    // will yield node 1
```

### GetPathFromRootAsync
Returns nodes from root to queried node in order
```c#
        ┌───────1───────┐   
        │       │       │
    ┌───2───┐   3       4
    │       │           │
    5       6           8
    │       │
    9       10
    │
    7
    
    var node = await dbContext.Categories.FindAsync(10);
    var pathFromRootToTen = await repository.GetPathFromRootAsync(node);
    // will yield nodes 1,2,6
```

### Query
Queries all nodes
```c#
        ┌───────1───────┐   
        │       │       │
    ┌───2───┐   3       4
    │       │           │
    5       6           8
    │       │
    9       10
    │
    7
    
    repository.Query()    
    // will yield nodes 1,2,3,4,5,6,7,8,9,10
```

### QueryRoots
Get all root nodes
```c#
        ┌───────1───────┐   
        │       │       │
    ┌───2───┐   3       4
    │       │           │
    5       6           8
    │       │
    9       10
    │
    7
    
    repository.QueryRoots()    
    // will yield node 1
```

### QueryAncestors
Get all nodes that precede queried node (not always in order from root)
```c#
        ┌───────1───────┐   
        │       │       │
    ┌───2───┐   3       4
    │       │           │
    5       6           8
    │       │
    9       10
    │
    7
    
    var node = await dbContext.Categories.FindAsync(5);
    var ancestorsOfFive = await repository.QueryAncestors(node).ToListAsync();
    // will yield nodes 2, 1
```

### QueryDescendants
Get all nodes which have queries node as ancestor
```c#
        ┌───────1───────┐   
        │       │       │
    ┌───2───┐   3       4
    │       │           │
    5       6           8
    │       │
    9       10
    │
    7
    
    var node = await dbContext.Categories.FindAsync(2);
    var descendantsOfTwo = await repository.QueryDescendants(node).ToListAsync();
    // will yield nodes 5, 6, 9, 10, 7
```

### QuerySiblings
Gets all sibling nodes (self is not included)
```c#
        ┌───────1───────┐   
        │       │       │
    ┌───2───┐   3       4
    │       │           │
    5       6           8
    │       │
    9       10
    │
    7
    
    var node = await dbContext.Categories.FindAsync(2);
    var childrenOfOne = await repository.QuerySiblings(node).ToListAsync();
    // will yield nodes 3,4
```

### QueryChildren
Get all direct children of queried node
```c#
        ┌───────1───────┐   
        │       │       │
    ┌───2───┐   3       4
    │       │           │
    5       6           8
    │       │
    9       10
    │
    7
    
    var node = await dbContext.Categories.FindAsync(1);
    var childrenOfOne = await repository.QueryChildren(node).ToListAsync();
    // will yield nodes 2,3,4
```

### IsAncestorOf / IsDescendantOf
Tells whether one node is above the other in the tree, from the paths of the entities as loaded, without a query.
A node is not its own ancestor or descendant.
```c#
        ┌───────1───────┐   
        │       │       │
    ┌───2───┐   3       4
    │       │           │
    5       6           8
    │       │
    9       10
    │
    7

    var two = await dbContext.Categories.FindAsync(2);
    var seven = await dbContext.Categories.FindAsync(7);
    var three = await dbContext.Categories.FindAsync(3);

    repository.IsAncestorOf(two, seven);    // true
    repository.IsDescendantOf(seven, two);  // true
    repository.IsAncestorOf(three, seven);  // false
```

### ValidateAsync
Checks the whole stored tree and reports inconsistent nodes, e.g. after the tree was changed outside
TreeRepository. `ParentId` is taken as the truth: a wrong `Path` or `Level` is reported for that node only, but if a
node's `ParentId` was changed by hand, that node and everything below it are reported. Nodes below a missing parent
or a cycle are not reported. Reads the whole table into memory in one query and never modifies data.
```c#
    var problems = await repository.ValidateAsync();
    foreach (var problem in problems)
    {
        // problem.Kind is MissingParent, Cycle, WrongPath or WrongLevel
        Console.WriteLine($"{problem.Id}: {problem.Kind} - {problem.Message}");
    }
    // Empty for a consistent tree
```

## ITreeRepository WRITE API
### SetParentAsync
Updates node and all its descendants, will save the underlying context

```c#
        ┌───────1───────┐   
        │       │       │
    ┌───2───┐   3       4
    │       │           │
    5       6           8
    │       │
    9       10
    │
    7

    var nodeTwo = await dbContext.Categories.FindAsync(2);
    var nodeThree = await dbContext.Categories.FindAsync(3);
    
    await repository.SetParentAsync(nodeTwo, nodeThree);
    // Will produce following tree
       1───────┐   
       │       │
       3       4
       │       │
   ┌───2───┐   8
   │       │   
   5       6 
   │       │ 
   9       10
   │         
   7
```         

```c#
        ┌───────1───────┐   
        │       │       │
    ┌───2───┐   3       4
    │       │           │
    5       6           8
    │       │
    9       10
    │
    7

    var nodeTwo = await dbContext.Categories.FindAsync(2);
    
    await repository.SetParentAsync(nodeTwo, null);
    // Will produce following trees

    ┌───2───┐      ┌───1───┐ 
    │       │      │       │ 
    5       6      3       4 
    │       │              │ 
    9       10             8 
    │
    7               

```

### DetachNodeAsync
Detaches node from tree, attaching children to detachee's parent.
```c#
        ┌───────1───────┐   
        │       │       │
    ┌───2───┐   3       4
    │       │           │
    5       6           8
    │       │
    9       10
    │
    7

    var nodeTwo = await dbContext.Categories.FindAsync(2);
    await repository.DetachNodeAsync(nodeTwo);
    // Will produce following trees (notice detached 2, and reparented 5,6)
    2       ┌─────┬──1──┬─────┐     
            │     │     │     │
            5     6     3     4
            │     │           │
            9     10          8
            │         
            7        
```

### RemoveNodeAsync
Deletes node from the tree. Children are parented to parent of deleted node.
```c#
        ┌───────1───────┐   
        │       │       │
    ┌───2───┐   3       4
    │       │           │
    5       6           8
    │       │
    9       10
    │
    7

    var nodeTwo = await dbContext.Categories.FindAsync(2);
    await repository.RemoveNodeAsync(nodeTwo);
    // Will produce following tree
    ┌─────┬──1──┬─────┐     
    │     │     │     │
    5     6     3     4
    │     │           │
    9     10          8
    │         
    7        
```

## Identifier types
This package supports various types used as primary key, including possibility
to add your own. Only condition is it must be struct. It's necessary for the 
library to be able to serialize/deserialize the identifier types to be used
in path strings.
Package ships support for int & Guid by using AddDefaultIdentifierSerializers()
on your ServiceCollection. (A string serializer is registered too, but string keys
can't be used because the identifier type must be a struct.)

If you need to support your own identifier type, all you need to do is implement
simple interface IIdentifierSerializer<TId> & register it in container.

This is sample implementation of "int" type serializer.
```c#
public class IntIdentifierSerializer: IIdentifierSerializer<int>
{
    public string SerializeIdentifier(int id) => id.ToString();
    public int DeserializeIdentifier(string id) => int.Parse(id);
}
// ...
// And register via
services.AddTransient<IIdentifierSerializer<int>, IntIdentifierSerializer>();
```

## Running tests
The test suite runs on EF Core InMemory and SQLite, and in Docker containers on PostgreSQL and SQL Server.
```
dotnet test                               # all databases, requires Docker
dotnet test --filter "Category!=Docker"   # InMemory and SQLite only
```

## Changelog
### Unreleased
**Breaking:** `Query()` and `QueryRoots()` now return `IQueryable<TEntity>` instead of `IQueryable<TEntity?>`; rows
were never null. Already compiled code keeps working, since only nullability annotations changed. Remove `!` or `?`
you added to work around it; classes implementing `ITreeRepository` must change these two signatures.

**Breaking (behaviour):** `DetachNodeAsync` and `RemoveNodeAsync` now call `SaveChanges` once. Previously they saved
once per child, once more for the node and, for `RemoveNodeAsync`, once more for the delete. A failure now leaves the
tree unchanged instead of partly moved (in the database and in the change tracker), and large subtrees need one descendants query instead of one per child.
Affects code that counts saves, e.g. `SaveChanges` interceptors or audit logs expecting one event per child; no
signature changes.

Added [`ValidateAsync`](#validateasync), which reports nodes whose `ParentId` points to a missing row or forms a
cycle, or whose `Path` or `Level` does not match their ancestors. New public types `TreeProblem<TId>` and
`TreeProblemKind`. **Breaking** only for classes implementing `ITreeRepository`: they must add this member.

Added [`IsAncestorOf` and `IsDescendantOf`](#isancestorof--isdescendantof), which check whether one node is above the
other from the loaded paths, without a query. **Breaking** only for classes implementing `ITreeRepository`: they
must add these members.

Fixed `IntIdentifierSerializer` depending on the current culture: under a culture with a different negative sign,
negative ids were written and parsed in that culture's form. `GuidIdentifierSerializer` now names its format (`"D"`)
explicitly; its output is unchanged.
**Breaking (stored data):** only if you have negative `int` ids *and* the application ran under a culture whose
negative sign is not `-` (e.g. `sv-SE`, `fi-FI`, `nb-NO` write U+2212 `−`). Those rows' paths keep the old form, so
descendant, children and sibling queries no longer match them. Reading them still works. `ValidateAsync` reports them
as `WrongPath`; fix them once with:
```sql
UPDATE "Categories" SET "Path" = REPLACE("Path", N'−', '-') WHERE "Path" LIKE N'%−%';
```

Fixed `DetachNodeAsync` and `RemoveNodeAsync` throwing on PostgreSQL and SQL Server (`A command is already in
progress` / `There is already an open DataReader`) when the node has children. Affects every user of those providers;
SQLite and InMemory were not affected. No API or schema changes.

Fixed `SetParentAsync` throwing `ArgumentException` when moving a root node that has descendants under another node.

Fixed `QueryAncestors` on a root node returning an in-memory queryable, which threw on `ToListAsync()`, `CountAsync()`
and other async operators. It now returns an empty database query (one extra round trip when enumerated).

Behaviour changes, all providers:
* `SetParentAsync` now throws `InvalidOperationException` when the new parent is the entity itself or one of its
  descendants. Previously the cycle was stored and corrupted the tree. Code that relied on this never had a valid
  tree; no migration needed, but existing data created this way has to be repaired manually.
* `SetParentAsync`, `DetachNodeAsync` and `RemoveNodeAsync` now persist the change when the entity passed in is not
  tracked by the context (e.g. loaded with `AsNoTracking()` or by another context). Previously the descendants were
  updated but the entity itself was not, leaving an inconsistent tree. The entity is attached to the context as a
  side effect; if the context already tracks another instance of the same row, both instances are updated.

Documented the per-database limits (maximum tree depth, index use of descendant queries, rules for custom identifier
serializers) under [Database limits](#database-limits), and the behaviour on partial failure and with outdated
entities under [Considerations](#considerations). No change in behaviour.
```
No framework or dependency changes
```

### 10.0.15 (11.11.2026)
Upgraded to .NET 10. Requires .NET 10 and EF Core 10; projects on .NET 8 should stay on 8.0.2.
```
Target framework net8.0 -> net10.0
Microsoft.EntityFrameworkCore 8.0.2 -> 10.0.15
Microsoft.EntityFrameworkCore.Relational 8.0.2 -> 10.0.15
```

### 8.0.2 (03.03.2024)
Upgraded to .NET 8. Package version now follows the EF Core version.
`Query()`, `QueryRoots()` and `GetByIdAsync()` return nullable entity types.
```
Target framework net5.0 -> net8.0
Microsoft.EntityFrameworkCore 5.0.9 -> 8.0.2
Microsoft.EntityFrameworkCore.Relational 5.0.9 -> 8.0.2
```

### 1.0.8 (28.08.2021)
Dependency version bump
``` 
Microsoft.EntityFrameworkCore 5.0.5 -> 5.0.9
Microsoft.EntityFrameworkCore.Relational 5.0.5 -> 5.0.9
FluentAssertions 5.10.3 -> 6.1.0
```
