using Microsoft.EntityFrameworkCore.Diagnostics;

namespace EFMaterializedPath.Test.TestUtils;

public sealed class InMemoryTestDatabase : ITestDatabase
{
    public static readonly InMemoryTestDatabase Instance = new();

    public Task<TestDbContext> CreateContextAsync(params IInterceptor[] interceptors) =>
        Task.FromResult(TestHelpers.CreateTestDb(interceptors));

    public async Task DropAsync(TestDbContext context) => await context.DisposeAsync();
}
