using EFMaterializedPath.Test.TestUtils;

namespace EFMaterializedPath.Test;

public class TreeRepositoryTestBase : IAsyncLifetime
{
    protected TestDbContext DbContext { get; private set; }
    protected TreeRepository<TestDbContext, Category, int> Repository { get; private set; }

    public virtual async Task InitializeAsync()
    {
        DbContext = TestHelpers.CreateTestDb();
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

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }
}
