using System;
using System.Linq;
using System.Threading.Tasks;
using EFMaterializedPath.Test.TestUtils;
using FluentAssertions;
using Xunit;

namespace EFMaterializedPath.Test
{
    // ReSharper disable once InconsistentNaming
    public class TestTreeRepository_GetPathFromRootAsync
    {
        private readonly TestDbContext dbContext;
        private readonly TreeRepository<TestDbContext, Category, int> repository;

        public TestTreeRepository_GetPathFromRootAsync()
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
        public async Task GetPathFromRootOnRoot()
        {
            var root = await dbContext.Categories.FindAsync(1);
            
            (await repository.GetPathFromRootAsync(root)).Should().BeEmpty();
        }
        
        [Fact]
        public async Task GetPathFromRootOnDeepNode()
        {
            var seven = await dbContext.Categories.FindAsync(7);
            var path = (await repository.GetPathFromRootAsync(seven)).ToArray();

            path.Should().HaveCount(4);
            path.Select(p => p.Id).Should().Equal(1, 2, 5, 9);
        }
        
        [Fact]
        public async Task ThrowsOnNonStoredEntity()
        {
            Func<Task> nullEntity = async () => await repository.GetPathFromRootAsync(null!);
            await nullEntity.Should().ThrowAsync<ArgumentNullException>();
            
            Func<Task> nonStored = async () => await repository.GetPathFromRootAsync(new Category());
            await nonStored.Should().ThrowAsync<InvalidOperationException>();
        }
    }
}