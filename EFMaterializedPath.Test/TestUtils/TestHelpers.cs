using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace EFMaterializedPath.Test.TestUtils
{
    public static class TestHelpers
    {
        public static TestDbContext CreateTestDb()
        {
            var builder = new DbContextOptionsBuilder<TestDbContext>().UseInMemoryDatabase(
                databaseName: Guid.NewGuid().ToString());
            return new TestDbContext(builder.Options);
        }

        public static void CreateTestCategoryTree(TestDbContext dbContext,
            TreeRepository<TestDbContext, Category> repository)
        {
            var cats = Enumerable.Range(1, 10)
                .Select(i => new Category {Id = i})
                .ToList();

            dbContext.AddRange(cats);
            dbContext.SaveChanges();

            repository.SetParentAsync(cats[1], cats[0]);
            repository.SetParentAsync(cats[2], cats[0]);
            repository.SetParentAsync(cats[3], cats[0]);

            repository.SetParentAsync(cats[4], cats[1]);
            repository.SetParentAsync(cats[5], cats[1]);
            repository.SetParentAsync(cats[7], cats[3]);

            repository.SetParentAsync(cats[8], cats[4]);
            repository.SetParentAsync(cats[9], cats[5]);
            repository.SetParentAsync(cats[6], cats[8]);

            dbContext.SaveChanges();

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
}