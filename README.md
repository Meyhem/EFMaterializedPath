# EFMaterializedPath
![Tests](https://github.com/Meyhem/EFMaterializedPath/actions/workflows/dotnet.yml/badge.svg)
[![GitHub license](https://img.shields.io/github/license/Naereen/StrapDown.js.svg)](https://github.com/Naereen/StrapDown.js/blob/master/LICENSE)
[![Nuget](https://img.shields.io/nuget/dt/EFMaterializedPath)](https://www.nuget.org/packages/EFMaterializedPath/)

A simple repository library that provides easy way how to store tree hierarchies in Entity framework

## Contents
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
* * [QueryChildren](#querychildren)
* [ITreeRepository WRITE API](#itreerepository-write-api)
* * [SetParentAsync](#setparentasync)
* * [DetachNodeAsync](#detachnodeasync)
* * [DeleteNodeAsync](#deletenodeasync)



## Considerations
There are multiple approaches to storing hierarchical data in SQL tables, each having pros/cons.
Materialized path is viable in case you have hierarchy that is ofter read, but rarely written. 
E.g. product catalogue, categories...


It's not possible to ensure referential integrity by SQL constraints, and it's
solely dependent on correct usage of TreeRepository. In case of manual tampering
with IMaterializedPathEntity properties outside TreeRepository, it's likely you will
end up with inconsistent tree. Therefore always rely on using TreeRepository when
changing tree hierarchy. 

## Usage
### 1. Create entity implementing IMaterializedPathEntity
```c#
public class Category : IMaterializedPathEntity
{
    /* Props from IMaterializedPathEntity */
    public int Id { get; set; }
    public string Path { get; set; }
    public int Level { get; set; }
    public int? ParentId { get; set; }
    
    /* Custom props */
    public string Name { get; set; }
    public string  { get; set; }
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
        modelBuilder.ApplyConfiguration(new MaterializedEntityMapping<Category>());
    }
}
```

### 3. Register ITreeRepository IoC & use repository 
```c#
public void ConfigureServices(IServiceCollection services)
{
    services.AddDbContext<MyDbContext>();
    // This registers ITreeRepository<Category> for you to use
    services.AddTreeRepository<MyDbContext, Category>();
}
```
**or** create your own instance
```c#
var repo = new TreeRepository<MyDbContext, Category>(myDbContextInstance);
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
    
    var node = dbContext.Categories.FindAsync(2);
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
    
    var node = dbContext.Categories.FindAsync(10);
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
    
    var node = dbContext.Categories.FindAsync(5);
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
    
    var node = dbContext.Categories.FindAsync(2);
    var descendantsOfTwo = await repository.QueryDescendants(node).ToListAsync();
    // will yield nodes 5, 6, 9, 10, 7
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
    
    var node = dbContext.Categories.FindAsync(1);
    var childrenOfOne = await repository.QueryChildren(node).ToListAsync();
    // will yield nodes 2,3,4
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

    var nodeTwo = dbContext.Categories.FindAsync(2);
    var nodeThree = dbContext.Categories.FindAsync(3);
    
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

    var nodeTwo = dbContext.Categories.FindAsync(2);
    
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

    var nodeTwo = dbContext.Categories.FindAsync(2);
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

### DeleteNodeAsync
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

    var nodeTwo = dbContext.Categories.FindAsync(2);
    await repository.DetachNodeAsync(nodeTwo);
    // Will produce following tree
    ┌─────┬──1──┬─────┐     
    │     │     │     │
    5     6     3     4
    │     │           │
    9     10          8
    │         
    7        
```
