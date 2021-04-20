using System.Threading.Tasks;
using EFMaterializedPath.Test.Mocks;
using FluentAssertions;
using Xunit;

namespace EFMaterializedPath.Test
{
    // ReSharper disable once InconsistentNaming
    public class TestTreeRepository_QueryChildren
    {
        private readonly TestDbContext dbContext;
        private readonly TreeRepository<TestDbContext, Category> repository;

        public TestTreeRepository_QueryChildren()
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
            var children = repository.QueryChildren(root);

            children.Should().HaveCount(3);
        }

        [Fact]
        public async Task TestOnIntermediateNode()
        {
            var root = await dbContext.Categories.FindAsync(2);
            var children = repository.QueryChildren(root);

            children.Should().HaveCount(2);
        }

        [Fact]
        public async Task TestOnLeafNode()
        {
            var root = await dbContext.Categories.FindAsync(8);
            var children = repository.QueryChildren(root);

            children.Should().BeEmpty();
        }
    }
}