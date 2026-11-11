using EFMaterializedPath.Test.TestUtils;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Testcontainers.MsSql;

namespace EFMaterializedPath.IntegrationTest.Databases;

/// <summary>
/// One SQL Server container per test run; every test gets its own freshly created database in it.
/// </summary>
public sealed class SqlServerFixture : IRelationalTestDatabase, IAsyncLifetime
{
    private readonly MsSqlContainer container =
        new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest").Build();

    public string IndexedColumnsSql =>
        """
        SELECT c.name AS Value
        FROM sys.indexes i
        JOIN sys.index_columns ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id
        JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
        WHERE i.object_id = OBJECT_ID('Categories') AND i.is_primary_key = 0
        """;

    // EF Core maps indexed string columns to nvarchar(450); 12 Guids are 445 characters.
    public string PathColumnType => "nvarchar(450)";
    public int? MaxGuidTreeDepth => 12;

    // The default collation is case-insensitive.
    public bool PrefixMatchIgnoresCase => true;
    public bool EqualityIgnoresCase => true;
    public bool DescendantsQueryUsesPathIndex => true;

    public async Task<string> ExplainAsync(TestDbContext context, IQueryable<Category> query)
    {
        await context.Database.ExecuteSqlRawAsync("UPDATE STATISTICS Categories");
        await context.Database.OpenConnectionAsync();
        await using var command = query.CreateDbCommand();

        // Runs the query and returns its actual plan as an extra single-column result set.
        await context.Database.ExecuteSqlRawAsync("SET STATISTICS XML ON");
        var plan = new System.Text.StringBuilder();
        await using (var reader = await command.ExecuteReaderAsync())
        {
            do
            {
                while (await reader.ReadAsync())
                {
                    if (reader.FieldCount == 1)
                    {
                        plan.AppendLine(reader.GetString(0));
                    }
                }
            } while (await reader.NextResultAsync());
        }

        await context.Database.ExecuteSqlRawAsync("SET STATISTICS XML OFF");
        return plan.ToString();
    }

    public Task InitializeAsync() => container.StartAsync();

    public Task DisposeAsync() => container.DisposeAsync().AsTask();

    public async Task<TestDbContext> CreateContextAsync(params IInterceptor[] interceptors)
    {
        var connectionString = new SqlConnectionStringBuilder(container.GetConnectionString())
        {
            InitialCatalog = $"t_{Guid.NewGuid():N}",
            // Keeps the password readable on the connection so CreateSecondContext can reuse the string.
            PersistSecurityInfo = true,
        }.ConnectionString;

        var options = new DbContextOptionsBuilder<TestDbContext>().UseSqlServer(connectionString)
            .AddInterceptors(interceptors).Options;
        var context = new TestDbContext(options);
        await context.Database.EnsureCreatedAsync();
        return context;
    }

    public bool SupportsParallelConnections => true;

    public TestDbContext CreateSecondContext(TestDbContext first) =>
        new(new DbContextOptionsBuilder<TestDbContext>().UseSqlServer(first.Database.GetConnectionString()).Options);

    public async Task DropAsync(TestDbContext context)
    {
        await context.Database.EnsureDeletedAsync();
        await context.DisposeAsync();
    }
}
