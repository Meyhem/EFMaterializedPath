using EFMaterializedPath.Test.TestUtils;

namespace EFMaterializedPath.Test;

public class TestTreeRepository_IsAncestorOf : TreeRepositoryTestBase
{
    //         ┌───────1───────┐
    //         │       │       │
    //     ┌───2───┐   3       4
    //     │       │           │
    //     5       6           8
    //     │       │
    //     9       10
    //     │
    //     7

    [Theory]
    [InlineData(1, 7)]
    [InlineData(2, 7)]
    [InlineData(9, 7)]
    public async Task IsAncestor(int ancestor, int node)
    {
        Repository.IsAncestorOf(await NodeAsync(ancestor), await NodeAsync(node)).Should().BeTrue();
    }

    [Theory]
    [InlineData(7, 2)] // reverse direction
    [InlineData(3, 7)] // other branch
    [InlineData(6, 5)] // sibling
    [InlineData(1, 1)] // a node is not its own ancestor
    [InlineData(7, 1)] // root node, empty (or on InMemory null) path
    public async Task IsNotAncestor(int ancestor, int node)
    {
        Repository.IsAncestorOf(await NodeAsync(ancestor), await NodeAsync(node)).Should().BeFalse();
    }

    [Fact]
    public async Task IsDescendantOf()
    {
        var one = await NodeAsync(1);
        var seven = await NodeAsync(7);

        Repository.IsDescendantOf(seven, one).Should().BeTrue();
        Repository.IsDescendantOf(one, seven).Should().BeFalse();
    }

    [Fact]
    public async Task ThrowsOnNonStoredEntity()
    {
        var stored = await NodeAsync(1);

        Action[] nullArguments =
        [
            () => Repository.IsAncestorOf(null!, stored),
            () => Repository.IsAncestorOf(stored, null!),
            () => Repository.IsDescendantOf(null!, stored),
            () => Repository.IsDescendantOf(stored, null!),
        ];
        foreach (var call in nullArguments)
        {
            call.Should().Throw<ArgumentNullException>();
        }

        Action[] nonStoredArguments =
        [
            () => Repository.IsAncestorOf(new Category(), stored),
            () => Repository.IsAncestorOf(stored, new Category()),
            () => Repository.IsDescendantOf(new Category(), stored),
            () => Repository.IsDescendantOf(stored, new Category()),
        ];
        foreach (var call in nonStoredArguments)
        {
            call.Should().Throw<InvalidOperationException>();
        }
    }

    private async Task<Category> NodeAsync(int id) => await DbContext.Categories.FindAsync(id);
}
