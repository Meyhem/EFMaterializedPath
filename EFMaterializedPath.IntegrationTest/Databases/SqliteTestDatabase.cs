using EFMaterializedPath.Test.TestUtils;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace EFMaterializedPath.IntegrationTest.Databases;

/// <summary>
/// In-process SQLite: each context gets its own private in-memory database, alive while its connection is open.
/// </summary>
public sealed class SqliteTestDatabase : IRelationalTestDatabase
{
    public static readonly SqliteTestDatabase Instance = new();

    public string IndexedColumnsSql =>
        """
        SELECT ii.name AS Value
        FROM sqlite_master m, pragma_index_info(m.name) ii
        WHERE m.type = 'index' AND m.tbl_name = 'Categories'
        """;

    public async Task<TestDbContext> CreateContextAsync()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<TestDbContext>().UseSqlite(connection).Options;
        var context = new TestDbContext(options);
        await context.Database.EnsureCreatedAsync();
        return context;
    }

    public async Task DropAsync(TestDbContext context)
    {
        var connection = context.Database.GetDbConnection();
        await context.DisposeAsync();
        await connection.DisposeAsync();
    }
}
