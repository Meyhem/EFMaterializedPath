using EFMaterializedPath.Test.TestUtils;

namespace EFMaterializedPath.Test;

public class TreeRepositoryTestBase : IAsyncLifetime
{
    /// <summary>
    /// Database the tests run against. Integration tests override this to run the same tests on real providers.
    /// </summary>
    protected virtual ITestDatabase Database => InMemoryTestDatabase.Instance;

    protected TestDbContext DbContext { get; private set; }
    protected TreeRepository<TestDbContext, Category, int> Repository { get; private set; }

    public virtual async Task InitializeAsync()
    {
        DbContext = await Database.CreateContextAsync();
        Repository = new TreeRepository<TestDbContext, Category, int>(DbContext, new IntIdentifierSerializer());

        await TestHelpers.CreateTestCategoryTree(DbContext, Repository);

        //         ┌───────1───────┐
        //         │       │       │
        //     ┌───2───┐   3       4
        //     │       │           │
        //     5       6           8
        //     │       │
        //     9       10
        //     │
        //     7
    }

    public virtual async Task DisposeAsync()
    {
        try
        {
            // Whatever a test did, it must leave a consistent tree behind.
            await TreeInvariants.AssertValidAsync(DbContext);
        }
        finally
        {
            await Database.DropAsync(DbContext);
        }
    }
}
