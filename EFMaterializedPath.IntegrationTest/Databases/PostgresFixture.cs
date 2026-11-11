using EFMaterializedPath.Test.TestUtils;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Npgsql;
using Testcontainers.PostgreSql;

namespace EFMaterializedPath.IntegrationTest.Databases;

/// <summary>
/// One PostgreSQL container per test run; every test gets its own freshly created database in it.
/// </summary>
public sealed class PostgresFixture : IRelationalTestDatabase, IAsyncLifetime
{
    private readonly PostgreSqlContainer container = new PostgreSqlBuilder("postgres:17-alpine").Build();

    public string IndexedColumnsSql =>
        """
        SELECT a.attname AS "Value"
        FROM pg_index i
        JOIN pg_class t ON t.oid = i.indrelid
        JOIN pg_attribute a ON a.attrelid = t.oid AND a.attnum = ANY (i.indkey)
        WHERE t.relname = 'Categories' AND NOT i.indisprimary
        """;

    public string PathColumnType => "text";

    // A btree index entry is limited to 2704 bytes; 72 Guids are 2665 characters.
    public int? MaxGuidTreeDepth => 72;
    public bool PrefixMatchIgnoresCase => false;
    public bool EqualityIgnoresCase => false;

    // With a non-"C" collation a plain btree index cannot serve LIKE 'prefix%'.
    public bool DescendantsQueryUsesPathIndex => false;

    public async Task<string> ExplainAsync(TestDbContext context, IQueryable<Category> query)
    {
        await context.Database.ExecuteSqlRawAsync("ANALYZE");
        await context.Database.OpenConnectionAsync();
        await using var command = query.CreateDbCommand();
        command.CommandText = "EXPLAIN " + command.CommandText;

        var plan = new System.Text.StringBuilder();
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            plan.AppendLine(reader.GetValue(reader.FieldCount - 1).ToString());
        }

        return plan.ToString();
    }

    public Task InitializeAsync() => container.StartAsync();

    public Task DisposeAsync() => container.DisposeAsync().AsTask();

    public async Task<TestDbContext> CreateContextAsync(params IInterceptor[] interceptors)
    {
        var connectionString = new NpgsqlConnectionStringBuilder(container.GetConnectionString())
        {
            Database = $"t_{Guid.NewGuid():N}",
            // Keeps the password readable on the connection so CreateSecondContext can reuse the string.
            PersistSecurityInfo = true,
        }.ConnectionString;

        var options = new DbContextOptionsBuilder<TestDbContext>().UseNpgsql(connectionString)
            .AddInterceptors(interceptors).Options;
        var context = new TestDbContext(options);
        await context.Database.EnsureCreatedAsync();
        return context;
    }

    public bool SupportsParallelConnections => true;

    public TestDbContext CreateSecondContext(TestDbContext first) =>
        new(new DbContextOptionsBuilder<TestDbContext>().UseNpgsql(first.Database.GetConnectionString()).Options);

    public async Task DropAsync(TestDbContext context)
    {
        await context.Database.EnsureDeletedAsync();
        await context.DisposeAsync();
    }
}
