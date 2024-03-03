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
    public async Task ThrowsOnNonStoredEntity()
    {
        Func<Task> nullEntity = async () => await Repository.DetachNodeAsync(null!);
        await nullEntity.Should().ThrowAsync<ArgumentNullException>();

        Func<Task> nonStored = async () => await Repository.DetachNodeAsync(new Category());
        await nonStored.Should().ThrowAsync<InvalidOperationException>();
    }
}
