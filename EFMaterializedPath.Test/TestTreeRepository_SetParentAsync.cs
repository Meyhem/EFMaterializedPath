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
