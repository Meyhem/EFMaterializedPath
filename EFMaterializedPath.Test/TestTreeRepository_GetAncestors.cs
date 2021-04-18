using System;
using System.Linq;
using EFMaterializedPath.Test.Mocks;
using FluentAssertions;
using Xunit;

namespace EFMaterializedPath.Test
{
    // ReSharper disable once InconsistentNaming
    public class TestTreeRepository_GetAncestors : IDisposable
    {
        private readonly TestDbContext dbContext;
        private readonly TreeRepository<TestDbContext, Category> repository;

        public TestTreeRepository_GetAncestors()
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
        public void TestOnRootNode()
        {
            var root = dbContext.Categories.Find(1);
            var ancestors = repository.QueryAncestors(root);

            ancestors.Should().BeEmpty();
        }

        [Fact]
        public void TestOnIntermediateNode()
        {
            var node = dbContext.Categories.Find(2);
            var ancestors = repository.QueryAncestors(node).ToList();

            ancestors.Should().HaveCount(1);
            ancestors.Should().OnlyContain(c => c.Id == 1);
        }

        [Fact]
        public void TestOnLeafNode()
        {
            var node = dbContext.Categories.Find(7);
            var ancestors = repository.QueryAncestors(node).ToList();

            var expectedAncestorIds = new[] {9, 5, 2, 1};

            ancestors.Should().HaveCount(4);
            ancestors.Should().OnlyContain(c => expectedAncestorIds.Contains(c.Id));
        }

        public void Dispose()
        {
            dbContext.Dispose();
        }
    }
}