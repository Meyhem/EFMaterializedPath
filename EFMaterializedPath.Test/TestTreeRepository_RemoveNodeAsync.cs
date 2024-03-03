using EFMaterializedPath.Test.TestUtils;

namespace EFMaterializedPath.Test;

public class TestTreeRepository_RemoveNodeAsync : TreeRepositoryTestBase
{
    [Fact]
    public async Task DeleteNode()
    {
        var two = await DbContext.Categories.FindAsync(2);
        await Repository.RemoveNodeAsync(two);

        two = await DbContext.Categories.FindAsync(2);
        two.Should().BeNull();

        var five = await DbContext.Categories.FindAsync(5);
        five.ParentId.Should().Be(1);

        var six = await DbContext.Categories.FindAsync(6);
        six.ParentId.Should().Be(1);
    }

    [Fact]
    public async Task ThrowsOnNonStoredEntity()
    {
        Func<Task> nullEntity = async () => await Repository.RemoveNodeAsync(null!);
        await nullEntity.Should().ThrowAsync<ArgumentNullException>();

        Func<Task> nonStored = async () => await Repository.RemoveNodeAsync(new Category());
        await nonStored.Should().ThrowAsync<InvalidOperationException>();
    }
}
