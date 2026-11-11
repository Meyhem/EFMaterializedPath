using Microsoft.EntityFrameworkCore.Diagnostics;

namespace EFMaterializedPath.Test.TestUtils;

/// <summary>
/// Creates and drops an isolated database for a single test.
/// The default is <see cref="InMemoryTestDatabase"/>; integration tests plug in real providers.
/// </summary>
public interface ITestDatabase
{
    Task<TestDbContext> CreateContextAsync(params IInterceptor[] interceptors);
    Task DropAsync(TestDbContext context);
}
