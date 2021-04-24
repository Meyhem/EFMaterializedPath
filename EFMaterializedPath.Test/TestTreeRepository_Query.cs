using System.Linq;
using System.Threading.Tasks;
using EFMaterializedPath.Test.TestUtils;
using FluentAssertions;
using Xunit;
// ReSharper disable MergeIntoPattern

namespace EFMaterializedPath.Test
{
    // ReSharper disable once InconsistentNaming
    public class TestTreeRepository_Query
    {
        private readonly TreeRepository<TestDbContext, Category> repository;

        public TestTreeRepository_Query()
        {
            var dbContext = TestHelpers.CreateTestDb();
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
        public void Query()
        {
            var roots = repository.Query()
                .Where(c => c.Id > 2 && c.Id < 5)
                .Select(r => r.Id);

            roots.Should().HaveCount(2);
            roots.Should().OnlyContain(item => item > 2 && item < 5);
        }
    }
}