using EFMaterializedPath.Test.TestUtils;

namespace EFMaterializedPath.Test;

public class TestTreeRepository_GetPathFromRootAsync : TreeRepositoryTestBase
{
    [Fact]
    public async Task GetPathFromRootOnRoot()
    {
        var root = await DbContext.Categories.FindAsync(1);

        (await Repository.GetPathFromRootAsync(root)).Should().BeEmpty();
    }

    [Fact]
    public async Task GetPathFromRootOnDeepNode()
    {
        var seven = await DbContext.Categories.FindAsync(7);
        var path = (await Repository.GetPathFromRootAsync(seven)).ToArray();

        path.Should().HaveCount(4);
        path.Select(p => p.Id).Should().Equal(1, 2, 5, 9);
    }

    [Fact]
    public async Task ThrowsOnNonStoredEntity()
    {
        Func<Task> nullEntity = async () => await Repository.GetPathFromRootAsync(null!);
        await nullEntity.Should().ThrowAsync<ArgumentNullException>();

        Func<Task> nonStored = async () => await Repository.GetPathFromRootAsync(new Category());
        await nonStored.Should().ThrowAsync<InvalidOperationException>();
    }
}
