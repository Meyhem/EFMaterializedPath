using EFMaterializedPath.Core;
using EFMaterializedPath.Test.Mocks;
using FluentAssertions;
using Xunit;

namespace EFMaterializedPath.Test
{
    // ReSharper disable once InconsistentNaming
    public class TestTreeRepository_GetChildren
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
                var children = repository.GetChildren(root);

                children.Should().HaveCount(3);
            }
            
            [Fact]
            public void TestOnIntermediateNode()
            {
                var root = dbContext.Categories.Find(2);
                var children = repository.GetChildren(root);

                children.Should().HaveCount(2);
            }
            
            [Fact]
            public void TestOnLeafNode()
            {
                var root = dbContext.Categories.Find(8);
                var children = repository.GetChildren(root);

                children.Should().BeEmpty();
            }
        }
    }
}