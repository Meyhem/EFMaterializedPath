using Microsoft.EntityFrameworkCore;

namespace EFMaterializedPath.IntegrationTest.Checks;

/// <summary>
/// Path matching and path updates as translated to SQL by the provider.
/// </summary>
public abstract class PathChecks : RelationalCheckBase
{
    [Fact]
    public async Task DescendantsDoNotMatchIdsWithSamePrefix()
    {
        //   1       11      12
        //   │       │       │
        //   2       21      3
        var n = (await AddRootsAsync(1, 2, 3, 11, 12, 21)).ToDictionary(c => c.Id);
        await Repository.SetParentAsync(n[2], n[1]);
        await Repository.SetParentAsync(n[21], n[11]);
        await Repository.SetParentAsync(n[3], n[12]);

        (await Repository.QueryDescendants(n[1]).Select(c => c.Id).ToListAsync()).Should().Equal(2);
        (await Repository.QueryDescendants(n[11]).Select(c => c.Id).ToListAsync()).Should().Equal(21);
        (await Repository.QueryChildren(n[1]).Select(c => c.Id).ToListAsync()).Should().Equal(2);
        (await Repository.QueryAncestors(n[21]).Select(c => c.Id).ToListAsync()).Should().Equal(11);
        (await Repository.QuerySiblings(n[2]).Select(c => c.Id).ToListAsync()).Should().BeEmpty();
    }

    [Fact]
    public async Task PathFromRootIsRootFirst()
    {
        // Ids descend along the path, so primary-key order is the opposite of path order.
        var n = (await AddRootsAsync(10, 20, 30)).ToDictionary(c => c.Id);
        await Repository.SetParentAsync(n[20], n[30]);
        await Repository.SetParentAsync(n[10], n[20]);

        var path = await Repository.GetPathFromRootAsync(n[10]);

        path.Select(c => c.Id).Should().Equal(30, 20);
    }

    [Fact]
    public async Task MovingDeepSubtreeUpdatesAllDescendants()
    {
        const int depth = 50;
        var roots = await AddRootsAsync(1, 2);
        var chain = await AddRootsAsync(Enumerable.Range(100, depth).ToArray());

        // Hang the chain under 1: 1 → 100 → 101 → … → 149, built top-down.
        await Repository.SetParentAsync(chain[0], roots[0]);
        for (var i = 1; i < depth; i++)
        {
            await Repository.SetParentAsync(chain[i], chain[i - 1]);
        }

        // Move the whole 50-level subtree in one call.
        await Repository.SetParentAsync(chain[0], roots[1]);
        DbContext.ChangeTracker.Clear();

        for (var k = 0; k < depth; k++)
        {
            var node = await ReloadAsync(100 + k);
            var expectedPath = "|" + string.Join("|", new[] { 2 }.Concat(Enumerable.Range(100, k))) + "|";

            node.Path.Should().Be(expectedPath, $"node {100 + k} is {k + 1} levels below root 2");
            node.Level.Should().Be(k + 1);
        }

        var two = await ReloadAsync(2);
        (await Repository.QueryDescendants(two).CountAsync()).Should().Be(depth);
        (await Repository.QueryDescendants(await ReloadAsync(1)).CountAsync()).Should().Be(0);
    }
}
