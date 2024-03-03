using EFMaterializedPath.Test.TestUtils;

namespace EFMaterializedPath.Test;

public class TestTreeRepository_QueryChildren : TreeRepositoryTestBase
{
    [Fact]
    public async Task TestOnRootNode()
    {
        var root = await DbContext.Categories.FindAsync(1);
        var children = Repository.QueryChildren(root);

        children.Should().HaveCount(3);
    }

    [Fact]
    public async Task TestOnIntermediateNode()
    {
        var root = await DbContext.Categories.FindAsync(2);
        var children = Repository.QueryChildren(root);

        children.Should().HaveCount(2);
    }

    [Fact]
    public async Task TestOnLeafNode()
    {
        var root = await DbContext.Categories.FindAsync(8);
        var children = Repository.QueryChildren(root);

        children.Should().BeEmpty();
    }

    [Fact]
    public void ThrowsOnNonStoredEntity()
    {
        Action nullEntity = () => Repository.QueryChildren(null!);
        nullEntity.Should().Throw<ArgumentNullException>();

        Action nonStored = () => Repository.QueryChildren(new Category());
        nonStored.Should().Throw<InvalidOperationException>();
    }
}
