using Microsoft.EntityFrameworkCore;
using EFMaterializedPath.Test.TestUtils;

namespace EFMaterializedPath.Test;

public class TestTreeRepository_SetParentAsync : TreeRepositoryTestBase
{
    [Fact]
    public async Task SetParentUpdatesDescendants()
    {
        var five = await DbContext.Set<Category>().FindAsync(5);
        var four = await DbContext.Set<Category>().FindAsync(4);

        await Repository.SetParentAsync(five, four);
        await DbContext.SaveChangesAsync();

        five.ParentId.Should().Be(4);
        five.Level.Should().Be(2);
        five.Path.Should().Be("|1|4|");

        var nine = await DbContext.Set<Category>().FindAsync(9);
        nine.Path.Should().Be("|1|4|5|");

        var seven = await DbContext.Set<Category>().FindAsync(7);
        seven.Path.Should().Be("|1|4|5|9|");
    }

    [Fact]
    public async Task SetParentNull()
    {
        var five = await DbContext.Set<Category>().FindAsync(5);

        await Repository.SetParentAsync(five, null);
        await DbContext.SaveChangesAsync();

        five.ParentId.Should().BeNull();
        five.Level.Should().Be(0);
        five.Path.Should().Be("");

        var nine = await DbContext.Set<Category>().FindAsync(9);
        nine.Path.Should().Be("|5|");

        var seven = await DbContext.Set<Category>().FindAsync(7);
        seven.Path.Should().Be("|5|9|");
    }

    [Fact]
    public async Task SetParentOnInterleavedSubtrees()
    {
        var two = await DbContext.Set<Category>().FindAsync(2);
        var five = await DbContext.Set<Category>().FindAsync(5);
        var nine = await DbContext.Set<Category>().FindAsync(9);
        var four = await DbContext.Set<Category>().FindAsync(4);

        await Repository.SetParentAsync(two, null);
        two.Path.Should().Be("");
        five.Path.Should().Be("|2|");
        nine.Path.Should().Be("|2|5|");

        await Repository.SetParentAsync(five, four);
        five.Path.Should().Be("|1|4|");
        nine.Path.Should().Be("|1|4|5|");
    }

    [Fact]
    public async Task MovingRootWithDescendantsUnderAnotherNode()
    {
        var two = await DbContext.Set<Category>().FindAsync(2);
        var four = await DbContext.Set<Category>().FindAsync(4);

        // Make 2 a root (it keeps its subtree 5, 6, 9, 10, 7), then attach it again somewhere else.
        await Repository.SetParentAsync(two, null);
        await Repository.SetParentAsync(two, four);

        two.Path.Should().Be("|1|4|");
        two.Level.Should().Be(2);
        two.ParentId.Should().Be(4);

        var five = await DbContext.Set<Category>().FindAsync(5);
        five.Path.Should().Be("|1|4|2|");
        five.Level.Should().Be(3);

        var seven = await DbContext.Set<Category>().FindAsync(7);
        seven.Path.Should().Be("|1|4|2|5|9|");
        seven.Level.Should().Be(5);
    }

    [Fact]
    public async Task ThrowsWhenParentIsTheEntityItself()
    {
        var two = await DbContext.Set<Category>().FindAsync(2);

        Func<Task> selfParent = async () => await Repository.SetParentAsync(two, two);

        await selfParent.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task ThrowsWhenParentIsDescendantOfEntity()
    {
        var two = await DbContext.Set<Category>().FindAsync(2);
        var seven = await DbContext.Set<Category>().FindAsync(7);

        Func<Task> cycle = async () => await Repository.SetParentAsync(two, seven);

        await cycle.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task SetParentPersistsUntrackedEntity()
    {
        var five = await DbContext.Set<Category>().AsNoTracking().SingleAsync(c => c.Id == 5);
        var four = await DbContext.Set<Category>().AsNoTracking().SingleAsync(c => c.Id == 4);

        await Repository.SetParentAsync(five, four);
        DbContext.ChangeTracker.Clear();

        var storedFive = await DbContext.Set<Category>().AsNoTracking().SingleAsync(c => c.Id == 5);
        storedFive.Path.Should().Be("|1|4|");
        storedFive.Level.Should().Be(2);
        storedFive.ParentId.Should().Be(4);

        var storedNine = await DbContext.Set<Category>().AsNoTracking().SingleAsync(c => c.Id == 9);
        storedNine.Path.Should().Be("|1|4|5|");
    }

    [Fact]
    public async Task SetParentPersistsUntrackedEntityWhenContextTracksNothing()
    {
        DbContext.ChangeTracker.Clear();
        var five = await DbContext.Set<Category>().AsNoTracking().SingleAsync(c => c.Id == 5);
        var four = await DbContext.Set<Category>().AsNoTracking().SingleAsync(c => c.Id == 4);

        await Repository.SetParentAsync(five, four);
        DbContext.ChangeTracker.Clear();

        var storedFive = await DbContext.Set<Category>().AsNoTracking().SingleAsync(c => c.Id == 5);
        storedFive.Path.Should().Be("|1|4|");
        storedFive.ParentId.Should().Be(4);

        var storedSeven = await DbContext.Set<Category>().AsNoTracking().SingleAsync(c => c.Id == 7);
        storedSeven.Path.Should().Be("|1|4|5|9|");
    }

    [Fact]
    public async Task CycleAttemptLeavesTreeUnchanged()
    {
        var two = await DbContext.Set<Category>().FindAsync(2);
        var seven = await DbContext.Set<Category>().FindAsync(7);

        Func<Task> cycle = async () => await Repository.SetParentAsync(two, seven);
        await cycle.Should().ThrowAsync<InvalidOperationException>();

        two.Path.Should().Be("|1|");
        two.ParentId.Should().Be(1);
        seven.Path.Should().Be("|1|2|5|9|");
    }

    [Fact]
    public async Task ThrowsOnNonStoredEntity()
    {
        Func<Task> nullEntity = async () => await Repository.SetParentAsync(null!, null);
        await nullEntity.Should().ThrowAsync<ArgumentNullException>();

        Func<Task> nonStored = async () => await Repository.SetParentAsync(new Category(), null);
        await nonStored.Should().ThrowAsync<InvalidOperationException>();

        Func<Task> nonStoredParent = async () =>
            await Repository.SetParentAsync(new Category() {Id = 1}, new Category());
        await nonStoredParent.Should().ThrowAsync<InvalidOperationException>();
    }
}
