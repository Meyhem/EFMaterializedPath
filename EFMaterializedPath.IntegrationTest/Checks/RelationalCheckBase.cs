using EFMaterializedPath.IntegrationTest.Databases;
using EFMaterializedPath.Test.TestUtils;
using Microsoft.EntityFrameworkCore;

namespace EFMaterializedPath.IntegrationTest.Checks;

/// <summary>
/// Base for checks that only make sense on a real relational database. Starts each test with an empty schema.
/// </summary>
public abstract class RelationalCheckBase : IAsyncLifetime
{
    protected abstract IRelationalTestDatabase Database { get; }

    protected TestDbContext DbContext { get; private set; }
    protected TreeRepository<TestDbContext, Category, int> Repository { get; private set; }

    public async Task InitializeAsync()
    {
        DbContext = await Database.CreateContextAsync();
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

    /// <summary>Re-reads a node from the database, bypassing the change tracker.</summary>
    protected Task<Category> ReloadAsync(int id) =>
        DbContext.Categories.AsNoTracking().SingleAsync(c => c.Id == id);
}
