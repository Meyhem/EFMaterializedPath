using Microsoft.EntityFrameworkCore;
using EFMaterializedPath.Test.TestUtils;

namespace EFMaterializedPath.Test;

public class TestTreeRepository_DetachNodeAsync : TreeRepositoryTestBase
{
    [Fact]
    public async Task DetachRoot()
    {
        // detach node
        var root = await DbContext.Categories.FindAsync(1);
        await Repository.DetachNodeAsync(root);

        var newRoots = DbContext.Categories.Where(p => p.ParentId == null);
        // check if children has been made root
        newRoots.Select(r => r.Id).Should().BeEquivalentTo(new int[] {1, 2, 3, 4});

        var two = await DbContext.Categories.FindAsync(2);
        two.Level.Should().Be(0);
        two.ParentId.Should().BeNull();
        two.Path.Should().BeEmpty();

        // check of descendant paths has been updated
        var five = await DbContext.Categories.FindAsync(5);
        five.Level.Should().Be(1);
        five.ParentId.Should().Be(2);
        five.Path.Should().Be("|2|");
    }

    [Fact]
    public async Task DetachLeaf()
    {
        var seven = await DbContext.Categories.FindAsync(7);

        await Repository.DetachNodeAsync(seven);

        seven = await DbContext.Categories.FindAsync(7);
        seven.Level.Should().Be(0);
        seven.ParentId.Should().BeNull();
        seven.Path.Should().BeEmpty();
    }

    [Fact]
    public async Task SavesOnce()
    {
        var saves = 0;
        DbContext.SavedChanges += (_, _) => saves++;

        await Repository.DetachNodeAsync(await DbContext.Categories.FindAsync(2));

        saves.Should().Be(1);
    }

    [Fact]
    public async Task DetachInnerNode()
    {
        await Repository.DetachNodeAsync(await DbContext.Categories.FindAsync(2));

        await AssertDetachedTwoAsync();
        var rows = await DbContext.Categories.AsNoTracking().ToDictionaryAsync(c => c.Id);
        rows[9].Should().BeEquivalentTo(new { ParentId = (int?)5, Level = 2, Path = "|1|5|" });
        rows[6].Should().BeEquivalentTo(new { ParentId = (int?)1, Level = 1, Path = "|1|" });
        rows[10].Should().BeEquivalentTo(new { ParentId = (int?)6, Level = 2, Path = "|1|6|" });
    }

    [Fact]
    public async Task DetachUntrackedNode()
    {
        var two = await DbContext.Categories.AsNoTracking().SingleAsync(c => c.Id == 2);

        await Repository.DetachNodeAsync(two);

        await AssertDetachedTwoAsync();
    }

    [Fact]
    public async Task DetachUpdatesTrackedDuplicate()
    {
        var tracked = await DbContext.Categories.FindAsync(2);
        var untracked = await DbContext.Categories.AsNoTracking().SingleAsync(c => c.Id == 2);

        await Repository.DetachNodeAsync(untracked);

        foreach (var instance in new[] { tracked, untracked })
        {
            instance.Should().BeEquivalentTo(new { ParentId = (int?)null, Level = 0, Path = "" });
        }

        await AssertDetachedTwoAsync();
    }

    /// <summary>Checks the stored rows after node 2 was detached from the standard tree.</summary>
    private async Task AssertDetachedTwoAsync()
    {
        DbContext.ChangeTracker.Clear();
        var rows = await DbContext.Categories.AsNoTracking().ToDictionaryAsync(c => c.Id);
        rows[2].Should().BeEquivalentTo(new { ParentId = (int?)null, Level = 0, Path = "" });
        rows[5].Should().BeEquivalentTo(new { ParentId = (int?)1, Level = 1, Path = "|1|" });
        rows[7].Should().BeEquivalentTo(new { ParentId = (int?)9, Level = 3, Path = "|1|5|9|" });
    }

    [Fact]
    public async Task ThrowsOnNonStoredEntity()
    {
        Func<Task> nullEntity = async () => await Repository.DetachNodeAsync(null!);
        await nullEntity.Should().ThrowAsync<ArgumentNullException>();

        Func<Task> nonStored = async () => await Repository.DetachNodeAsync(new Category());
        await nonStored.Should().ThrowAsync<InvalidOperationException>();
    }
}
