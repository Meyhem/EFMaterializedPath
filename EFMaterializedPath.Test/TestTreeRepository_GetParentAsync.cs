using EFMaterializedPath.Test.TestUtils;

namespace EFMaterializedPath.Test;

public class TestTreeRepository_GetParentAsync : TreeRepositoryTestBase
{
    [Fact]
    public async Task GetParentOnRoot()
    {
        var root = await DbContext.Categories.FindAsync(1);
        (await Repository.GetParentAsync(root)).Should().BeNull();
    }

    [Fact]
    public async Task GetParentIntermediateNode()
    {
        var five = await DbContext.Categories.FindAsync(5);
        (await Repository.GetParentAsync(five))!.Id.Should().Be(2);
    }

    [Fact]
    public async Task ThrowsOnNonStoredEntity()
    {
        Func<Task> nullEntity = async () => await Repository.GetParentAsync(null!);
        await nullEntity.Should().ThrowAsync<ArgumentNullException>();

        Func<Task> nonStored = async () => await Repository.GetParentAsync(new Category());
        await nonStored.Should().ThrowAsync<InvalidOperationException>();
    }
}
