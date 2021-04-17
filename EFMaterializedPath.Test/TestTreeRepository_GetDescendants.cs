using System.Linq;
using EFMaterializedPath.Core;
using EFMaterializedPath.Test.Mocks;
using FluentAssertions;
using Xunit;

namespace EFMaterializedPath.Test
{
    // ReSharper disable once InconsistentNaming
    public class TestTreeRepository_GetDescendants
    {
        private readonly TestDbContext dbContext;
        private readonly TreeRepository<Category> repository;

        public TestTreeRepository_GetDescendants()
        {
            dbContext = TestHelpers.CreateTestDb();
            repository = new TreeRepository<Category>(dbContext);

            TestHelpers.CreateTestCategoryTree(dbContext, repository);

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

        [Fact]
        public void TestOnRootNode()
        {
            var root = dbContext.Categories.Find(1);
            var descendants = repository.GetDescendants(root);

            descendants.Should().HaveCount(9);
        }

        [Fact]
        public void TestOnIntermediateNode()
        {
            var intermediate = dbContext.Categories.Find(5);
            var descendants = repository.GetDescendants(intermediate);

            descendants.Should().HaveCount(2);
        }

        [Fact]
        public void TestOnLeafNode()
        {
            var leaf = dbContext.Categories.Find(7);
            var descendants = repository.GetDescendants(leaf);

            descendants.Should().BeEmpty();
        }
    }
}