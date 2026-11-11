using EFMaterializedPath.Test.TestUtils;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

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

    public string PathColumnType => "TEXT";
    public int? MaxGuidTreeDepth => null;

    // SQLite's LIKE ignores ASCII case, "=" does not.
    public bool PrefixMatchIgnoresCase => true;
    public bool EqualityIgnoresCase => false;

    // A case-insensitive LIKE cannot use the default (BINARY) index.
    public bool DescendantsQueryUsesPathIndex => false;

    public async Task<string> ExplainAsync(TestDbContext context, IQueryable<Category> query)
    {
        await context.Database.ExecuteSqlRawAsync("ANALYZE");
        await context.Database.OpenConnectionAsync();
        await using var command = query.CreateDbCommand();
        command.CommandText = "EXPLAIN QUERY PLAN " + command.CommandText;

        var plan = new System.Text.StringBuilder();
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            plan.AppendLine(reader.GetValue(reader.FieldCount - 1).ToString());
        }

        return plan.ToString();
    }

    public async Task<TestDbContext> CreateContextAsync(params IInterceptor[] interceptors)
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<TestDbContext>().UseSqlite(connection)
            .AddInterceptors(interceptors).Options;
        var context = new TestDbContext(options);
        await context.Database.EnsureCreatedAsync();
        return context;
    }

    public bool SupportsParallelConnections => false;

    public TestDbContext CreateSecondContext(TestDbContext first) =>
        new(new DbContextOptionsBuilder<TestDbContext>().UseSqlite(first.Database.GetDbConnection()).Options);

    public async Task DropAsync(TestDbContext context)
    {
        var connection = context.Database.GetDbConnection();
        await context.DisposeAsync();
        await connection.DisposeAsync();
    }
}
