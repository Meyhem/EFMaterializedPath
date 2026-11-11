namespace EFMaterializedPath.Test.TestUtils;

public sealed class InMemoryTestDatabase : ITestDatabase
{
    public static readonly InMemoryTestDatabase Instance = new();

    public Task<TestDbContext> CreateContextAsync() => Task.FromResult(TestHelpers.CreateTestDb());

    public async Task DropAsync(TestDbContext context) => await context.DisposeAsync();
}
