using System.Collections.Generic;
using EFMaterializedPath.Test.TestUtils;

namespace EFMaterializedPath.Test;

public class TestTreeRepository_ValidateAsync : TreeRepositoryTestBase
{
    [Fact]
    public async Task ValidTreeHasNoProblems()
    {
        // On InMemory the never-moved root 1 has a null Path; that is a valid root.
        (await Repository.ValidateAsync()).Should().BeEmpty();
    }

    [Fact]
    public async Task ReportsMissingParent()
    {
        var problems = await ValidateCorruptedAsync(9, c => c.ParentId = 999);

        // Node 7 below it cannot be checked and is not reported.
        problems.Select(p => (p.Id, p.Kind)).Should().Equal((9, TreeProblemKind.MissingParent));
    }

    [Fact]
    public async Task ReportsWrongPath()
    {
        var problems = await ValidateCorruptedAsync(9, c => c.Path = "|1|9|");

        problems.Select(p => (p.Id, p.Kind)).Should().Equal((9, TreeProblemKind.WrongPath));
    }

    [Fact]
    public async Task ReportsWrongLevel()
    {
        var problems = await ValidateCorruptedAsync(5, c => c.Level = 7);

        problems.Select(p => (p.Id, p.Kind)).Should().Equal((5, TreeProblemKind.WrongLevel));
    }

    [Fact]
    public async Task ReportsNonEmptyRoot()
    {
        var problems = await ValidateCorruptedAsync(1, c => c.Path = "|3|");

        problems.Select(p => (p.Id, p.Kind)).Should().Equal((1, TreeProblemKind.WrongPath));
    }

    [Fact]
    public async Task ReportsSelfParent()
    {
        var problems = await ValidateCorruptedAsync(3, c => c.ParentId = 3);

        problems.Select(p => (p.Id, p.Kind)).Should().Equal((3, TreeProblemKind.Cycle));
    }

    [Fact]
    public async Task ReportsCycle()
    {
        // 2 -> 5 -> 2; the nodes hanging off the loop (6, 7, 9, 10) are not reported.
        var problems = await ValidateCorruptedAsync(2, c => c.ParentId = 5);

        problems.Select(p => (p.Id, p.Kind)).Should().Equal((2, TreeProblemKind.Cycle), (5, TreeProblemKind.Cycle));
    }

    [Fact]
    public async Task ReportsWholeSubtreeOfManuallyReparentedNode()
    {
        // ParentId is the truth, so after moving 5 under 3 by hand, 5 and everything below it have outdated paths.
        var problems = await ValidateCorruptedAsync(5, c => c.ParentId = 3);

        problems.Select(p => (p.Id, p.Kind)).Should().Equal(
            (5, TreeProblemKind.WrongPath), (7, TreeProblemKind.WrongPath), (9, TreeProblemKind.WrongPath));
    }

    [Fact]
    public async Task MessageNamesExpectedAndStoredValue()
    {
        var problems = await ValidateCorruptedAsync(5, c => c.Level = 7);

        problems.Single().Message.Should().Be("Node 5 has level 7, expected 2.");
    }

    /// <summary>
    /// Stores a corrupted version of node <paramref name="id"/>, validates, then restores the node so the base
    /// class's tree check still passes.
    /// </summary>
    private async Task<IReadOnlyList<TreeProblem<int>>> ValidateCorruptedAsync(int id, Action<Category> corrupt)
    {
        var node = await DbContext.Categories.FindAsync(id);
        var (path, level, parentId) = (node.Path, node.Level, node.ParentId);

        corrupt(node);
        await DbContext.SaveChangesAsync();
        try
        {
            return await Repository.ValidateAsync();
        }
        finally
        {
            (node.Path, node.Level, node.ParentId) = (path, level, parentId);
            await DbContext.SaveChangesAsync();
        }
    }
}
