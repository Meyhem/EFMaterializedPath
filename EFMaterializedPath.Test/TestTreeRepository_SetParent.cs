using System.Linq;
using EFMaterializedPath.Test.Mocks;
using FluentAssertions;
using Xunit;

namespace EFMaterializedPath.Test
{
    // ReSharper disable once InconsistentNaming
    public class TestTreeRepository_SetParent
    {
        private readonly TestDbContext dbContext;
        private readonly TreeRepository<TestDbContext, Category> repository;

        public TestTreeRepository_SetParent()
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
        public void SetParentUpdatesDescendants()
        {
            var five = dbContext.Set<Category>().Find(5);
            var four = dbContext.Set<Category>().Find(4);

            repository.SetParent(five, four);
            dbContext.SaveChanges();
            
            five.ParentId.Should().Be(4);
            five.Level.Should().Be(2);
            five.Path.Should().Be("|1|4|");

            var nine = dbContext.Set<Category>().Find(9);
            nine.Path.Should().Be("|1|4|5|");

            var seven = dbContext.Set<Category>().Find(7);
            seven.Path.Should().Be("|1|4|5|9|");
        }

        [Fact]
        public void SetParentNull()
        {
            var five = dbContext.Set<Category>().Find(5);

            repository.SetParent(five, null);
            dbContext.SaveChanges();

            five.ParentId.Should().BeNull();
            five.Level.Should().Be(0);
            five.Path.Should().Be("");

            var nine = dbContext.Set<Category>().Find(9);
            nine.Path.Should().Be("|5|");

            var seven = dbContext.Set<Category>().Find(7);
            seven.Path.Should().Be("|5|9|");
        }
        
        [Fact]
        public void SetParentOnInterleavedSubtrees()
        {
            var two = dbContext.Set<Category>().Find(2);
            var five = dbContext.Set<Category>().Find(5);
            var nine = dbContext.Set<Category>().Find(9);
            var four = dbContext.Set<Category>().Find(4);

            repository.SetParent(two, null);
            two.Path.Should().Be("");
            five.Path.Should().Be("|2|");
            nine.Path.Should().Be("|2|5|");

            repository.SetParent(five, four);
            five.Path.Should().Be("|1|4|");
            nine.Path.Should().Be("|1|4|5|");
        }
    }
}