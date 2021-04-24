using System.Linq;
using System.Threading.Tasks;
using EFMaterializedPath.Test.TestUtils;
using FluentAssertions;
using Xunit;

namespace EFMaterializedPath.Test
{
    // ReSharper disable once InconsistentNaming
    public class TestTreeRepository_DetachNodeAsync
    {
        private readonly TestDbContext dbContext;
        private readonly TreeRepository<TestDbContext, Category> repository;

        public TestTreeRepository_DetachNodeAsync()
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
        public async Task DetachRoot()
        {
            // detach node
            var root = await dbContext.Categories.FindAsync(1);
            await repository.DetachNodeAsync(root);

            var newRoots = dbContext.Categories.Where(p => p.ParentId == null);
            // check if children has been made root
            newRoots.Select(r => r.Id).Should().BeEquivalentTo(1, 2, 3, 4);
            
            var two = await dbContext.Categories.FindAsync(2);
            two.Level.Should().Be(0);
            two.ParentId.Should().BeNull();
            two.Path.Should().BeEmpty();
            
            // check of descendant paths has been updated
            var five = await dbContext.Categories.FindAsync(5);
            five.Level.Should().Be(1);
            five.ParentId.Should().Be(2);
            five.Path.Should().Be("|2|");
        }
        
        [Fact]
        public async Task DetachLeaf()
        {
            var seven = await dbContext.Categories.FindAsync(7);
            
            await repository.DetachNodeAsync(seven);
            
            seven = await dbContext.Categories.FindAsync(7);
            seven.Level.Should().Be(0);
            seven.ParentId.Should().BeNull();
            seven.Path.Should().BeEmpty();
        }
    }
}