using Microsoft.EntityFrameworkCore;

namespace EFMaterializedPath.Test.TestUtils;

public static class TestHelpers
{
    public static TestDbContext CreateTestDb()
    {
        var builder = new DbContextOptionsBuilder<TestDbContext>().UseInMemoryDatabase(
            databaseName: Guid.NewGuid().ToString()
        );
        return new TestDbContext(builder.Options);
    }

    public static async Task CreateTestCategoryTree(
        TestDbContext dbContext,
        TreeRepository<TestDbContext, Category, int> repository
    )
    {
        var cats = Enumerable.Range(1, 10)
            .Select(i => new Category {Id = i})
            .ToList();

        dbContext.AddRange(cats);
        await dbContext.SaveChangesAsync();

        await repository.SetParentAsync(cats[1], cats[0]);
        await repository.SetParentAsync(cats[2], cats[0]);
        await repository.SetParentAsync(cats[3], cats[0]);

        await repository.SetParentAsync(cats[4], cats[1]);
        await repository.SetParentAsync(cats[5], cats[1]);
        await repository.SetParentAsync(cats[7], cats[3]);

        await repository.SetParentAsync(cats[8], cats[4]);
        await repository.SetParentAsync(cats[9], cats[5]);
        await repository.SetParentAsync(cats[6], cats[8]);

        await dbContext.SaveChangesAsync();

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
}
