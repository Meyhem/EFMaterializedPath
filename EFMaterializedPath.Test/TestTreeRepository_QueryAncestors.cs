using System;
using System.Linq;
using System.Threading.Tasks;
using EFMaterializedPath.Test.TestUtils;
using FluentAssertions;
using Xunit;

namespace EFMaterializedPath.Test
{
    // ReSharper disable once InconsistentNaming
    public class TestTreeRepository_QueryAncestors
    {
        private readonly TestDbContext dbContext;
        private readonly TreeRepository<TestDbContext, Category, int> repository;

        public TestTreeRepository_QueryAncestors()
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
            var ancestors = repository.QueryAncestors(root);

            ancestors.Should().BeEmpty();
        }

        [Fact]
        public async Task TestOnIntermediateNode()
        {
            var node = await dbContext.Categories.FindAsync(2);
            var ancestors = repository.QueryAncestors(node).ToList();

            ancestors.Should().HaveCount(1);
            ancestors.Should().OnlyContain(c => c.Id == 1);
        }

        [Fact]
        public async Task TestOnLeafNode()
        {
            var node = await dbContext.Categories.FindAsync(7);
            var ancestors = repository.QueryAncestors(node).ToList();

            var expectedAncestorIds = new[] {9, 5, 2, 1};

            ancestors.Should().HaveCount(4);
            ancestors.Should().OnlyContain(c => expectedAncestorIds.Contains(c.Id));
        }

        [Fact]
        public void ThrowsOnNonStoredEntity()
        {
            Action nullEntity = () => repository.QueryAncestors(null!);
            nullEntity.Should().Throw<ArgumentNullException>();
            
            Action nonStored = () => repository.QueryAncestors(new Category());
            nonStored.Should().Throw<InvalidOperationException>();
        }
    }
}