using Microsoft.EntityFrameworkCore;

namespace EFMaterializedPath.IntegrationTest.Checks;

/// <summary>
/// The schema produced by <c>MaterializedEntityMapping</c> is created correctly by the database.
/// </summary>
public abstract class SchemaChecks : RelationalCheckBase
{
    [Fact]
    public async Task PathDefaultsToEmptyString()
    {
        await AddRootsAsync(1);
        DbContext.ChangeTracker.Clear();

        var stored = await ReloadAsync(1);

        stored.Path.Should().Be("");
        stored.Level.Should().Be(0);
        stored.ParentId.Should().BeNull();
    }

    [Fact]
    public async Task PathAndParentIdAreIndexed()
    {
        var indexedColumns = await DbContext.Database.SqlQueryRaw<string>(Database.IndexedColumnsSql).ToListAsync();

        indexedColumns.Should().Contain(["Path", "ParentId"]);
    }
}
