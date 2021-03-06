using System;
using System.Linq;
using System.Threading.Tasks;
using EFMaterializedPath.Test.TestUtils;
using FluentAssertions;
using Xunit;

namespace EFMaterializedPath.Test
{
    // ReSharper disable once InconsistentNaming
    public class TestTreeRepository_RemoveNodeAsync
    {
        private readonly TestDbContext dbContext;
        private readonly TreeRepository<TestDbContext, Category, int> repository;

        public TestTreeRepository_RemoveNodeAsync()
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
        public async Task DeleteNode()
        {
            var two = await dbContext.Categories.FindAsync(2);
            await repository.RemoveNodeAsync(two);
            
            two = await dbContext.Categories.FindAsync(2);
            two.Should().BeNull();
            
            var five = await dbContext.Categories.FindAsync(5);
            five.ParentId.Should().Be(1);
            
            var six = await dbContext.Categories.FindAsync(6);
            six.ParentId.Should().Be(1);
        }
        
        [Fact]
        public async Task ThrowsOnNonStoredEntity()
        {
            Func<Task> nullEntity = async () => await repository.RemoveNodeAsync(null!);
            await nullEntity.Should().ThrowAsync<ArgumentNullException>();
            
            Func<Task> nonStored = async () => await repository.RemoveNodeAsync(new Category());
            await nonStored.Should().ThrowAsync<InvalidOperationException>();
        }
    }
}