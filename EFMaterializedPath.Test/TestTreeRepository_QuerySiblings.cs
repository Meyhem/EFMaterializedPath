using EFMaterializedPath.Test.TestUtils;

namespace EFMaterializedPath.Test;

public class TestTreeRepository_QuerySiblings : TreeRepositoryTestBase
{
    [Fact]
    public async Task TestOnRootNode()
    {
        var root = await DbContext.Categories.FindAsync(1);
        var siblings = Repository.QuerySiblings(root);
        siblings.Should().BeEmpty();
    }

    [Fact]
    public async Task TestOnIntermediateNode()
    {
        var root = await DbContext.Categories.FindAsync(2);
        var siblings = Repository.QuerySiblings(root).ToList();

        siblings.Should().HaveCount(2);
        siblings.Select(s => s.Id).Should().BeEquivalentTo(new int[] {3, 4});
    }

    [Fact]
    public void ThrowsOnNonStoredEntity()
    {
        Action nullEntity = () => Repository.QuerySiblings(null!);
        nullEntity.Should().Throw<ArgumentNullException>();

        Action nonStored = () => Repository.QuerySiblings(new Category());
        nonStored.Should().Throw<InvalidOperationException>();
    }
}
