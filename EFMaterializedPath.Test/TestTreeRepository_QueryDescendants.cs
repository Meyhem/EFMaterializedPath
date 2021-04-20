using System.Linq;
using System.Threading.Tasks;
using EFMaterializedPath.Test.Mocks;
using FluentAssertions;
using Xunit;

namespace EFMaterializedPath.Test
{
    // ReSharper disable once InconsistentNaming
    public class TestTreeRepository_QueryDescendants
    {
        private readonly TestDbContext dbContext;
        private readonly TreeRepository<TestDbContext, Category> repository;

        public TestTreeRepository_QueryDescendants()
        {
            dbContext = TestHelpers.CreateTestDb();
            repository = new TreeRepository<TestDbContext, Category>(dbContext);

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
        public async Task TestOnRootNode()
        {
            var root = await dbContext.Categories.FindAsync(1);
            var descendants = repository.QueryDescendants(root);

            descendants.Should().HaveCount(9);
        }

        [Fact]
        public async Task TestOnIntermediateNode()
        {
            var intermediate = await dbContext.Categories.FindAsync(5);
            var descendants = repository.QueryDescendants(intermediate);

            descendants.Should().HaveCount(2);
        }

        [Fact]
        public async Task TestOnLeafNode()
        {
            var leaf = await dbContext.Categories.FindAsync(7);
            var descendants = repository.QueryDescendants(leaf);

            descendants.Should().BeEmpty();
        }
    }
}