using EFMaterializedPath.Test.TestUtils;
using Microsoft.EntityFrameworkCore;
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

    public Task InitializeAsync() => container.StartAsync();

    public Task DisposeAsync() => container.DisposeAsync().AsTask();

    public async Task<TestDbContext> CreateContextAsync()
    {
        var connectionString = new NpgsqlConnectionStringBuilder(container.GetConnectionString())
        {
            Database = $"t_{Guid.NewGuid():N}",
        }.ConnectionString;

        var options = new DbContextOptionsBuilder<TestDbContext>().UseNpgsql(connectionString).Options;
        var context = new TestDbContext(options);
        await context.Database.EnsureCreatedAsync();
        return context;
    }

    public async Task DropAsync(TestDbContext context)
    {
        await context.Database.EnsureDeletedAsync();
        await context.DisposeAsync();
    }
}
