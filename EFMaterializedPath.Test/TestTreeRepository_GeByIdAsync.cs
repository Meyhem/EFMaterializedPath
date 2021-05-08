using System.Linq;
using System.Threading.Tasks;
using EFMaterializedPath.Test.TestUtils;
using FluentAssertions;
using Xunit;

namespace EFMaterializedPath.Test
{
    // ReSharper disable once InconsistentNaming
    public class TestTreeRepository_GetByIdAsync
    {
        private readonly TreeRepository<TestDbContext, Category, int> repository;

        public TestTreeRepository_GetByIdAsync()
        {
            var dbContext = TestHelpers.CreateTestDb();
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
        public async Task GetByIdAsync()
        {
            var five = await repository.GetByIdAsync(5);
            five.Id.Should().Be(5);
            
            var one = await repository.GetByIdAsync(1);
            one.Id.Should().Be(1);
        }
        
        [Fact]
        public async Task GetByIdAsyncWhenNotExist()
        {
            var zero = await repository.GetByIdAsync(0);
            zero.Should().BeNull();
            
            var node123 = await repository.GetByIdAsync(123);
            node123.Should().BeNull();
        }
    }
}