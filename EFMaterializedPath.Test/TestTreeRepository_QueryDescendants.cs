using EFMaterializedPath.Test.TestUtils;

namespace EFMaterializedPath.Test;

public class TestTreeRepository_QueryDescendants : TreeRepositoryTestBase
{
    [Fact]
    public async Task TestOnRootNode()
    {
        var root = await DbContext.Categories.FindAsync(1);
        var descendants = Repository.QueryDescendants(root);

        descendants.Should().HaveCount(9);
    }

    [Fact]
    public async Task TestOnIntermediateNode()
    {
        var intermediate = await DbContext.Categories.FindAsync(5);
        var descendants = Repository.QueryDescendants(intermediate);

        descendants.Should().HaveCount(2);
    }

    [Fact]
    public async Task TestOnLeafNode()
    {
        var leaf = await DbContext.Categories.FindAsync(7);
        var descendants = Repository.QueryDescendants(leaf);

        descendants.Should().BeEmpty();
    }

    [Fact]
    public void ThrowsOnNonStoredEntity()
    {
        Action nullEntity = () => Repository.QueryDescendants(null!);
        nullEntity.Should().Throw<ArgumentNullException>();

        Action nonStored = () => Repository.QueryDescendants(new Category());
        nonStored.Should().Throw<InvalidOperationException>();
    }
}
