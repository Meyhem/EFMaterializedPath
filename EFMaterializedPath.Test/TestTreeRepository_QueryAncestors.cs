using EFMaterializedPath.Test.TestUtils;

namespace EFMaterializedPath.Test;

// ReSharper disable once InconsistentNaming
public class TestTreeRepository_QueryAncestors : TreeRepositoryTestBase
{
    [Fact]
    public async Task TestOnRootNode()
    {
        var root = await DbContext.Categories.FindAsync(1);
        var ancestors = Repository.QueryAncestors(root);

        ancestors.Should().BeEmpty();
    }

    [Fact]
    public async Task TestOnIntermediateNode()
    {
        var node = await DbContext.Categories.FindAsync(2);
        var ancestors = Repository.QueryAncestors(node).ToList();

        ancestors.Should().HaveCount(1);
        ancestors.Should().OnlyContain(c => c.Id == 1);
    }

    [Fact]
    public async Task TestOnLeafNode()
    {
        var node = await DbContext.Categories.FindAsync(7);
        var ancestors = Repository.QueryAncestors(node).ToList();

        var expectedAncestorIds = new[] {9, 5, 2, 1};

        ancestors.Should().HaveCount(4);
        ancestors.Should().OnlyContain(c => expectedAncestorIds.Contains(c.Id));
    }

    [Fact]
    public void ThrowsOnNonStoredEntity()
    {
        Action nullEntity = () => Repository.QueryAncestors(null!);
        nullEntity.Should().Throw<ArgumentNullException>();

        Action nonStored = () => Repository.QueryAncestors(new Category());
        nonStored.Should().Throw<InvalidOperationException>();
    }
}
