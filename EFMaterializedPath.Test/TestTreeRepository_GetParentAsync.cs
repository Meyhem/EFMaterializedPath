using System;
using System.Linq;
using System.Threading.Tasks;
using EFMaterializedPath.Test.TestUtils;
using FluentAssertions;
using Xunit;

namespace EFMaterializedPath.Test
{
    // ReSharper disable once InconsistentNaming
    public class TestTreeRepository_GetParentAsync
    {
        private readonly TestDbContext dbContext;
        private readonly TreeRepository<TestDbContext, Category, int> repository;

        public TestTreeRepository_GetParentAsync()
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
        public async Task GetParentOnRoot()
        {
            var root = await dbContext.Categories.FindAsync(1);
            (await repository.GetParentAsync(root)).Should().BeNull();
        }
        
        [Fact]
        public async Task GetParentIntermediateNode()
        {
            var five = await dbContext.Categories.FindAsync(5);
            (await repository.GetParentAsync(five))!.Id.Should().Be(2);
        }

        [Fact]
        public async Task ThrowsOnNonStoredEntity()
        {
            Func<Task> nullEntity = async () => await repository.GetParentAsync(null!);
            await nullEntity.Should().ThrowAsync<ArgumentNullException>();
            
            Func<Task> nonStored = async () => await repository.GetParentAsync(new Category());
            await nonStored.Should().ThrowAsync<InvalidOperationException>();
        }

    }
}