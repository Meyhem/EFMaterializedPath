using System;
using System.Linq;
using System.Threading.Tasks;
using EFMaterializedPath.Test.TestUtils;
using FluentAssertions;
using Xunit;

namespace EFMaterializedPath.Test
{
    // ReSharper disable once InconsistentNaming
    public class TestTreeRepository_QuerySiblings
    {
        private readonly TestDbContext dbContext;
        private readonly TreeRepository<TestDbContext, Category, int> repository;

        public TestTreeRepository_QuerySiblings()
        {
            dbContext = TestHelpers.CreateTestDb();
            repository = new TreeRepository<TestDbContext, Category, int>(dbContext, new IntIdentifierSerializer());

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
            var siblings = repository.QuerySiblings(root);
            siblings.Should().BeEmpty();
        }

        [Fact]
        public async Task TestOnIntermediateNode()
        {
            var root = await dbContext.Categories.FindAsync(2);
            var siblings = repository.QuerySiblings(root).ToList();

            siblings.Should().HaveCount(2);
            siblings.Select(s => s.Id).Should().BeEquivalentTo(new int[] { 3, 4 });
        }
        
        [Fact]
        public void ThrowsOnNonStoredEntity()
        {
            Action nullEntity = () => repository.QuerySiblings(null!);
            nullEntity.Should().Throw<ArgumentNullException>();
            
            Action nonStored = () => repository.QuerySiblings(new Category());
            nonStored.Should().Throw<InvalidOperationException>();
        }
    }
}