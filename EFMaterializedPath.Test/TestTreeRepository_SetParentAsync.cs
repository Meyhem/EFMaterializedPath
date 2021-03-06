using System;
using System.Threading.Tasks;
using EFMaterializedPath.Test.TestUtils;
using FluentAssertions;
using Xunit;

namespace EFMaterializedPath.Test
{
    // ReSharper disable once InconsistentNaming
    public class TestTreeRepository_SetParentAsync
    {
        private readonly TestDbContext dbContext;
        private readonly TreeRepository<TestDbContext, Category, int> repository;

        public TestTreeRepository_SetParentAsync()
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
        public async Task SetParentUpdatesDescendants()
        {
            var five = await dbContext.Set<Category>().FindAsync(5);
            var four = await dbContext.Set<Category>().FindAsync(4);

            await repository.SetParentAsync(five, four);
            await dbContext.SaveChangesAsync();

            five.ParentId.Should().Be(4);
            five.Level.Should().Be(2);
            five.Path.Should().Be("|1|4|");

            var nine = await dbContext.Set<Category>().FindAsync(9);
            nine.Path.Should().Be("|1|4|5|");

            var seven = await dbContext.Set<Category>().FindAsync(7);
            seven.Path.Should().Be("|1|4|5|9|");
        }

        [Fact]
        public async Task SetParentNull()
        {
            var five = await dbContext.Set<Category>().FindAsync(5);

            await repository.SetParentAsync(five, null);
            await dbContext.SaveChangesAsync();

            five.ParentId.Should().BeNull();
            five.Level.Should().Be(0);
            five.Path.Should().Be("");

            var nine = await dbContext.Set<Category>().FindAsync(9);
            nine.Path.Should().Be("|5|");

            var seven = await dbContext.Set<Category>().FindAsync(7);
            seven.Path.Should().Be("|5|9|");
        }

        [Fact]
        public async Task SetParentOnInterleavedSubtrees()
        {
            var two = await dbContext.Set<Category>().FindAsync(2);
            var five = await dbContext.Set<Category>().FindAsync(5);
            var nine = await dbContext.Set<Category>().FindAsync(9);
            var four = await dbContext.Set<Category>().FindAsync(4);

            await repository.SetParentAsync(two, null);
            two.Path.Should().Be("");
            five.Path.Should().Be("|2|");
            nine.Path.Should().Be("|2|5|");

            await repository.SetParentAsync(five, four);
            five.Path.Should().Be("|1|4|");
            nine.Path.Should().Be("|1|4|5|");
        }

        [Fact]
        public async Task ThrowsOnNonStoredEntity()
        {
            Func<Task> nullEntity = async () => await repository.SetParentAsync(null!, null);
            await nullEntity.Should().ThrowAsync<ArgumentNullException>();

            Func<Task> nonStored = async () => await repository.SetParentAsync(new Category(), null);
            await nonStored.Should().ThrowAsync<InvalidOperationException>();

            Func<Task> nonStoredParent = async () =>
                await repository.SetParentAsync(new Category() {Id = 1}, new Category());
            await nonStoredParent.Should().ThrowAsync<InvalidOperationException>();
        }
    }
}