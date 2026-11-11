using System.Collections.Generic;
using EFMaterializedPath.IntegrationTest.Databases;
using EFMaterializedPath.Test.TestUtils;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace EFMaterializedPath.IntegrationTest.Checks;

/// <summary>
/// Base for checks that only make sense on a real relational database. Starts each test with an empty schema.
/// </summary>
public abstract class RelationalCheckBase : IAsyncLifetime
{
    protected abstract IRelationalTestDatabase Database { get; }

    protected TestDbContext DbContext { get; private set; }
    protected TreeRepository<TestDbContext, Category, int> Repository { get; private set; }

    /// <summary>Interceptors the test's context is created with, e.g. to inject failures.</summary>
    protected virtual IInterceptor[] Interceptors => [];

    public async Task InitializeAsync()
    {
        DbContext = await Database.CreateContextAsync(Interceptors);
        Repository = new TreeRepository<TestDbContext, Category, int>(DbContext, new IntIdentifierSerializer());
    }

    public Task DisposeAsync() => Database.DropAsync(DbContext);

    /// <summary>Inserts root nodes with the given ids (Path left to the database default).</summary>
    protected async Task<Category[]> AddRootsAsync(params int[] ids)
    {
        var nodes = ids.Select(id => new Category { Id = id }).ToArray();
        DbContext.AddRange(nodes);
        await DbContext.SaveChangesAsync();
        return nodes;
    }

    /// <summary>
    /// Builds a tree from (child, parent) pairs; every id mentioned is inserted first, as a root.
    /// </summary>
    protected async Task<Dictionary<int, Category>> AddTreeAsync(params (int Child, int Parent)[] edges)
    {
        var ids = edges.SelectMany(e => new[] { e.Parent, e.Child }).Distinct().ToArray();
        var nodes = (await AddRootsAsync(ids)).ToDictionary(c => c.Id);
        foreach (var (child, parent) in edges)
        {
            await Repository.SetParentAsync(nodes[child], nodes[parent]);
        }

        return nodes;
    }

    /// <summary>Every stored node, read past the change tracker, ordered by id.</summary>
    protected Task<List<Category>> StoredTreeAsync() =>
        DbContext.Categories.AsNoTracking().OrderBy(c => c.Id).ToListAsync();

    /// <summary>Re-reads a node from the database, bypassing the change tracker.</summary>
    protected Task<Category> ReloadAsync(int id) =>
        DbContext.Categories.AsNoTracking().SingleAsync(c => c.Id == id);
}
