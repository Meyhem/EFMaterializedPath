using System.Collections.Generic;
using EFMaterializedPath.Test.TestUtils;
using Microsoft.EntityFrameworkCore;

namespace EFMaterializedPath.IntegrationTest.Checks;

/// <summary>
/// Where each database stops: path length, index use and text comparison rules. Several of these pin a known
/// limitation (documented in the README under "Database limits") rather than ideal behaviour, so that a change in the
/// mapping, EF Core or a provider that moves a limit is noticed.
/// </summary>
public abstract class LimitChecks : RelationalCheckBase
{
    [Fact]
    public void PathColumnTypeIsUnchanged()
    {
        var path = DbContext.Model.FindEntityType(typeof(Category))!.FindProperty(nameof(Category.Path))!;

        path.GetColumnType().Should().Be(Database.PathColumnType, "the column type is part of the published schema");
    }

    [Fact]
    public async Task GuidTreeDepthLimit()
    {
        var repository = new TreeRepository<TestDbContext, GuidCategory, Guid>(DbContext, new GuidIdentifierSerializer());
        var depth = Database.MaxGuidTreeDepth ?? 100;

        var deepest = await AddGuidRootAsync();
        for (var level = 1; level <= depth; level++)
        {
            var node = await AddGuidRootAsync();
            await repository.SetParentAsync(node, deepest);
            deepest = node;
        }

        var stored = await DbContext.GuidCategories.AsNoTracking().SingleAsync(c => c.Id == deepest.Id);
        stored.Level.Should().Be(depth);
        (await repository.QueryAncestors(stored).CountAsync()).Should().Be(depth);

        if (Database.MaxGuidTreeDepth is null)
        {
            return;
        }

        // One level deeper no longer fits the indexed Path column: the database rejects it and nothing is stored.
        var tooDeep = await AddGuidRootAsync();
        Func<Task> move = async () => await repository.SetParentAsync(tooDeep, deepest);

        await move.Should().ThrowAsync<DbUpdateException>();
        var rejected = await DbContext.GuidCategories.AsNoTracking().SingleAsync(c => c.Id == tooDeep.Id);
        rejected.ParentId.Should().BeNull();
        rejected.Path.Should().Be("");
        rejected.Level.Should().Be(0);
    }

    [Theory]
    [InlineData("a_c", "abc")]
    [InlineData("a%c", "abzc")]
    [InlineData("[ab]c", "ac")]
    [InlineData(@"a\c", "ac")]
    public async Task LikeWildcardsInSerializedIdsAreMatchedLiterally(string wildcardId, string lookalikeId)
    {
        var tree = await CreateTwoLookalikeFamiliesAsync(wildcardId, lookalikeId);

        (await tree.Repository.QueryDescendants(tree.First).Select(c => c.Id).ToListAsync())
            .Should().Equal(tree.FirstChild.Id);
        (await tree.Repository.QueryChildren(tree.First).Select(c => c.Id).ToListAsync())
            .Should().Equal(tree.FirstChild.Id);
        (await tree.Repository.QuerySiblings(tree.FirstChild).ToListAsync()).Should().BeEmpty();
    }

    [Fact]
    public async Task SerializedIdsDifferingOnlyByCase()
    {
        var tree = await CreateTwoLookalikeFamiliesAsync("ab", "AB");

        var descendants = await tree.Repository.QueryDescendants(tree.First).Select(c => c.Id).ToListAsync();
        var children = await tree.Repository.QueryChildren(tree.First).Select(c => c.Id).ToListAsync();
        var siblings = await tree.Repository.QuerySiblings(tree.FirstChild).Select(c => c.Id).ToListAsync();

        // Known limitation where the database compares text ignoring case: the other family is matched as well.
        descendants.Should().BeEquivalentTo(
            Database.PrefixMatchIgnoresCase ? [tree.FirstChild.Id, tree.SecondChild.Id] : new[] { tree.FirstChild.Id }
        );
        children.Should().BeEquivalentTo(
            Database.EqualityIgnoresCase ? [tree.FirstChild.Id, tree.SecondChild.Id] : new[] { tree.FirstChild.Id }
        );
        siblings.Should().BeEquivalentTo(Database.EqualityIgnoresCase ? [tree.SecondChild.Id] : Array.Empty<Guid>());
    }

    [Fact]
    public async Task PathIndexUsage()
    {
        // 2000 roots with 20 children each: large and selective enough for planners to prefer an index if they can.
        var rows = new List<Category>();
        for (var root = 1; root <= 2000; root++)
        {
            rows.Add(new Category { Id = root, Path = "" });
            for (var child = 1; child <= 20; child++)
            {
                rows.Add(new Category { Id = root * 100_000 + child, Path = $"|{root}|", Level = 1, ParentId = root });
            }
        }

        DbContext.AddRange(rows);
        await DbContext.SaveChangesAsync();
        DbContext.ChangeTracker.Clear();
        var node = await ReloadAsync(7);

        var childrenPlan = await Database.ExplainAsync(DbContext, Repository.QueryChildren(node));
        var descendantsPlan = await Database.ExplainAsync(DbContext, Repository.QueryDescendants(node));

        childrenPlan.Should().Contain("IX_Categories_Path");
        if (Database.DescendantsQueryUsesPathIndex)
        {
            descendantsPlan.Should().Contain("IX_Categories_Path");
        }
        else
        {
            // Known limitation: with the default collation / LIKE rules this database scans the whole table.
            descendantsPlan.Should().NotContain("IX_Categories_Path");
        }
    }

    private async Task<GuidCategory> AddGuidRootAsync()
    {
        var node = new GuidCategory { Id = Guid.NewGuid() };
        DbContext.Add(node);
        await DbContext.SaveChangesAsync();
        return node;
    }

    /// <summary>
    /// Two roots whose serialized ids are <paramref name="firstId"/> and <paramref name="secondId"/>, each with one
    /// child.
    /// </summary>
    private async Task<LookalikeFamilies> CreateTwoLookalikeFamiliesAsync(string firstId, string secondId)
    {
        var nodes = Enumerable.Range(0, 4).Select(_ => new GuidCategory { Id = Guid.NewGuid() }).ToArray();
        var serializer = new FixedSerializer(new Dictionary<Guid, string>
        {
            [nodes[0].Id] = firstId, [nodes[1].Id] = secondId, [nodes[2].Id] = "child1", [nodes[3].Id] = "child2",
        });
        var repository = new TreeRepository<TestDbContext, GuidCategory, Guid>(DbContext, serializer);

        DbContext.AddRange(nodes);
        await DbContext.SaveChangesAsync();
        await repository.SetParentAsync(nodes[2], nodes[0]);
        await repository.SetParentAsync(nodes[3], nodes[1]);

        return new LookalikeFamilies(repository, nodes[0], nodes[2], nodes[3]);
    }

    private sealed record LookalikeFamilies(
        TreeRepository<TestDbContext, GuidCategory, Guid> Repository,
        GuidCategory First,
        GuidCategory FirstChild,
        GuidCategory SecondChild
    );

    /// <summary>A custom serializer that writes a chosen text for each id.</summary>
    private sealed class FixedSerializer(Dictionary<Guid, string> texts) : IIdentifierSerializer<Guid>
    {
        public string SerializeIdentifier(Guid id) => texts[id];
        public Guid DeserializeIdentifier(string id) => texts.Single(t => t.Value == id).Key;
    }
}
