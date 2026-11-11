using System.Collections.Generic;
using System.Text;
using EFMaterializedPath.Test.TestUtils;
using Microsoft.EntityFrameworkCore;

namespace EFMaterializedPath.Test;

/// <summary>
/// Applies a seeded random sequence of tree operations and, after every step, compares the stored tree with a plain
/// parent-dictionary model. A failure names the seed, the step and the operations so far, so it can be replayed.
/// </summary>
// ReSharper disable once InconsistentNaming
public class TestTreeRepository_RandomOperations : TreeRepositoryTestBase
{
    private const int Steps = 60;
    private const int MinNodes = 5;
    private const int MaxNodes = 25;

    /// <summary>Node id → parent id, the reference the database is compared with.</summary>
    private readonly Dictionary<int, int?> model = new()
    {
        [1] = null, [2] = 1, [3] = 1, [4] = 1, [5] = 2, [6] = 2, [8] = 4, [9] = 5, [10] = 6, [7] = 9,
    };

    private readonly StringBuilder log = new();
    private Random random = new(0);
    private int nextId = 100;

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    public async Task RandomOperationsKeepTreeConsistentWithModel(int seed)
    {
        random = new Random(seed);
        await GiveInMemoryRootsAnEmptyPathAsync();

        for (var step = 1; step <= Steps; step++)
        {
            try
            {
                await ApplyRandomOperationAsync();
                await AssertDatabaseMatchesModelAsync();
                await AssertQueriesMatchModelAsync(Pick(model.Keys));
            }
            catch (Exception e)
            {
                throw new InvalidOperationException($"Seed {seed} failed at step {step}. Operations so far:\n{log}", e);
            }
        }
    }

    private async Task ApplyRandomOperationAsync()
    {
        var roll = random.Next(10);

        if (roll >= 8 && model.Count < MaxNodes)
        {
            var id = nextId++;
            log.AppendLine($"add root {id}");
            DbContext.Add(new Category { Id = id });
            await DbContext.SaveChangesAsync();
            await GiveInMemoryRootsAnEmptyPathAsync();
            model[id] = null;
        }
        else if (roll == 7 && model.Count > MinNodes)
        {
            var id = Pick(model.Keys);
            log.AppendLine($"remove {id}");
            await Repository.RemoveNodeAsync(await LoadAsync(id));
            ReparentChildrenToGrandparent(id);
            model.Remove(id);
        }
        else if (roll == 6)
        {
            var id = Pick(model.Keys);
            log.AppendLine($"detach {id}");
            await Repository.DetachNodeAsync(await LoadAsync(id));
            ReparentChildrenToGrandparent(id);
            model[id] = null;
        }
        else if (roll == 5)
        {
            var id = Pick(model.Keys);
            var parentId = Pick(SubtreeOf(id));
            log.AppendLine($"move {id} under {parentId} (cycle, must be rejected)");
            var entity = await LoadAsync(id);
            var parent = await LoadAsync(parentId);

            Func<Task> cycle = async () => await Repository.SetParentAsync(entity, parent);
            await cycle.Should().ThrowAsync<InvalidOperationException>();
        }
        else
        {
            var id = Pick(model.Keys);
            var subtree = SubtreeOf(id);
            var candidates = model.Keys.Where(k => !subtree.Contains(k)).ToList();
            int? parentId = candidates.Count == 0 || random.Next(5) == 0 ? null : Pick(candidates);
            log.AppendLine($"move {id} under {parentId?.ToString() ?? "nothing (make root)"}");

            var entity = await LoadAsync(id);
            var parent = parentId is null ? null : await LoadAsync(parentId.Value);
            await Repository.SetParentAsync(entity, parent);
            model[id] = parentId;
        }
    }

    /// <summary>
    /// Relational databases store "" for a new root through the column default. InMemory ignores defaults and stores
    /// null, which root queries (<c>Path == ""</c>) do not match, so mimic the default there.
    /// </summary>
    private async Task GiveInMemoryRootsAnEmptyPathAsync()
    {
        if (DbContext.Database.IsRelational())
        {
            return;
        }

        foreach (var root in await DbContext.Categories.Where(c => c.Path == null).ToListAsync())
        {
            root.Path = "";
        }

        await DbContext.SaveChangesAsync();
    }

    /// <summary>The stored rows, and the instances the context still tracks, both agree with the model.</summary>
    private async Task AssertDatabaseMatchesModelAsync()
    {
        var rows = await DbContext.Categories.AsNoTracking().ToListAsync();

        TreeInvariants.AssertValid(rows);
        rows.ToDictionary(r => r.Id, r => r.ParentId).Should().BeEquivalentTo(model);

        var stored = rows.ToDictionary(r => r.Id);
        foreach (var tracked in DbContext.ChangeTracker.Entries<Category>().Select(e => e.Entity))
        {
            stored.Should().ContainKey(tracked.Id, "a tracked node must still exist in the database");
            var row = stored[tracked.Id];
            TreeInvariants.NormalizePath(tracked).Should()
                .Be(TreeInvariants.NormalizePath(row), $"tracked node {tracked.Id} must not be stale");
            tracked.Level.Should().Be(row.Level, $"tracked node {tracked.Id} must not be stale");
            tracked.ParentId.Should().Be(row.ParentId, $"tracked node {tracked.Id} must not be stale");
        }
    }

    private async Task AssertQueriesMatchModelAsync(int id)
    {
        var node = await LoadAsync(id);
        var because = $"of the model's view of node {id}";

        var ancestors = AncestorsRootFirst(id);
        var children = model.Where(kv => kv.Value == id).Select(kv => kv.Key);
        var siblings = model.Where(kv => kv.Value == model[id] && kv.Key != id).Select(kv => kv.Key);
        var descendants = SubtreeOf(id).Where(k => k != id);

        (await Repository.QueryAncestors(node).Select(c => c.Id).ToListAsync())
            .Should().BeEquivalentTo(ancestors, because);
        (await Repository.QueryChildren(node).Select(c => c.Id).ToListAsync())
            .Should().BeEquivalentTo(children, because);
        (await Repository.QuerySiblings(node).Select(c => c.Id).ToListAsync())
            .Should().BeEquivalentTo(siblings, because);
        (await Repository.QueryDescendants(node).Select(c => c.Id).ToListAsync())
            .Should().BeEquivalentTo(descendants, because);
        (await Repository.GetPathFromRootAsync(node)).Select(c => c.Id)
            .Should().Equal(ancestors, because);
        (await Repository.GetParentAsync(node))?.Id.Should().Be(model[id], because);
        (await Repository.QueryRoots().Select(c => c!.Id).ToListAsync())
            .Should().BeEquivalentTo(model.Where(kv => kv.Value is null).Select(kv => kv.Key));
    }

    /// <summary>
    /// Loads a node the way consumers do: usually tracked, sometimes with <c>AsNoTracking</c>.
    /// </summary>
    private async Task<Category> LoadAsync(int id)
    {
        if (random.Next(3) == 0)
        {
            return await DbContext.Categories.AsNoTracking().SingleAsync(c => c.Id == id);
        }

        return (await DbContext.Categories.FindAsync(id))!;
    }

    private void ReparentChildrenToGrandparent(int id)
    {
        foreach (var child in model.Where(kv => kv.Value == id).Select(kv => kv.Key).ToList())
        {
            model[child] = model[id];
        }
    }

    /// <summary>The node itself and everything below it.</summary>
    private HashSet<int> SubtreeOf(int id)
    {
        var subtree = new HashSet<int> { id };
        var added = true;
        while (added)
        {
            added = false;
            foreach (var (node, parent) in model)
            {
                if (parent is not null && subtree.Contains(parent.Value) && subtree.Add(node))
                {
                    added = true;
                }
            }
        }

        return subtree;
    }

    private List<int> AncestorsRootFirst(int id)
    {
        var ancestors = new List<int>();
        for (var parent = model[id]; parent is not null; parent = model[parent.Value])
        {
            ancestors.Insert(0, parent.Value);
        }

        return ancestors;
    }

    /// <summary>Picks from a sorted copy so a seed gives the same sequence regardless of dictionary order.</summary>
    private int Pick(IEnumerable<int> ids)
    {
        var sorted = ids.OrderBy(i => i).ToList();
        return sorted[random.Next(sorted.Count)];
    }
}
