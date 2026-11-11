using EFMaterializedPath.Test.TestUtils;
using Microsoft.EntityFrameworkCore;
using Xunit.Sdk;

namespace EFMaterializedPath.IntegrationTest.Checks;

/// <summary>
/// Several contexts working on one tree, as separate requests or processes do.
/// </summary>
public abstract class ConcurrencyChecks : RelationalCheckBase
{
    private TreeRepository<TestDbContext, Category, int> RepositoryFor(TestDbContext context) =>
        new(context, new IntIdentifierSerializer());

    /// <summary>
    /// Known limitation: the repository trusts the <c>Path</c> of the parent it is given. A parent loaded before
    /// another context moved it carries an outdated path, and the child is stored with that outdated path.
    /// </summary>
    [Fact]
    public async Task ParentThatAnotherContextMovedCorruptsTree_KnownLimitation()
    {
        // 1 → 3, and roots 2 and 4.
        var nodes = await AddTreeAsync((3, 1));
        var extra = (await AddRootsAsync(2, 4)).ToDictionary(c => c.Id);
        var staleThree = nodes[3]; // Path "|1|"

        await using (var other = Database.CreateSecondContext(DbContext))
        {
            // Meanwhile someone else moves 1 (and with it 3) under 2: 3 is now at "|2|1|".
            await RepositoryFor(other).SetParentAsync(
                await other.Categories.SingleAsync(c => c.Id == 1),
                await other.Categories.SingleAsync(c => c.Id == 2)
            );
        }

        await Repository.SetParentAsync(extra[4], staleThree);

        (await ReloadAsync(4)).Path.Should().Be("|1|3|", "the outdated path of 3 is used; the correct one is |2|1|3|");
        var stored = await StoredTreeAsync();
        var check = () => TreeInvariants.AssertValid(stored);
        check.Should().Throw<XunitException>();
    }

    /// <summary>
    /// Known limitation: descendants are found through the <c>Path</c> of the entity that is given. If another
    /// context moved that entity in the meantime, its descendants are not found and keep their previous paths.
    /// </summary>
    [Fact]
    public async Task EntityThatAnotherContextMovedCorruptsTree_KnownLimitation()
    {
        // 1 → 2 → 3, and roots 4 and 5.
        var nodes = await AddTreeAsync((2, 1), (3, 2));
        await AddRootsAsync(4, 5);
        var staleTwo = nodes[2]; // Path "|1|"
        var five = await DbContext.Categories.SingleAsync(c => c.Id == 5);

        await using (var other = Database.CreateSecondContext(DbContext))
        {
            // Meanwhile someone else moves 2 under 4: 3 is now at "|4|2|".
            await RepositoryFor(other).SetParentAsync(
                await other.Categories.SingleAsync(c => c.Id == 2),
                await other.Categories.SingleAsync(c => c.Id == 4)
            );
        }

        await Repository.SetParentAsync(staleTwo, five);

        (await ReloadAsync(2)).Path.Should().Be("|5|");
        (await ReloadAsync(3)).Path.Should().Be("|4|2|", "3 was looked for under the outdated |1|2| and not updated");
        var stored = await StoredTreeAsync();
        var check = () => TreeInvariants.AssertValid(stored);
        check.Should().Throw<XunitException>();
    }

    /// <summary>
    /// The safe pattern: load the entities in the context (and transaction) that changes them.
    /// </summary>
    [Fact]
    public async Task FreshlyLoadedEntitiesSeeChangesFromAnotherContext()
    {
        var nodes = await AddTreeAsync((2, 1), (3, 2));
        await AddRootsAsync(4, 5);

        await using (var other = Database.CreateSecondContext(DbContext))
        {
            await RepositoryFor(other).SetParentAsync(
                await other.Categories.SingleAsync(c => c.Id == 2),
                await other.Categories.SingleAsync(c => c.Id == 4)
            );
        }

        await DbContext.Entry(nodes[2]).ReloadAsync();
        await Repository.SetParentAsync(nodes[2], await DbContext.Categories.SingleAsync(c => c.Id == 5));

        var stored = await StoredTreeAsync();
        TreeInvariants.AssertValid(stored);
        stored.Single(c => c.Id == 3).Path.Should().Be("|5|2|");
    }

    [Fact]
    public async Task ParallelMovesOfSeparateSubtreesDoNotInterfere()
    {
        if (!Database.SupportsParallelConnections)
        {
            return;
        }

        const int workers = 8;
        const int movesPerWorker = 15;

        // Worker w owns subtree w01 → w02 → w03 and moves it back and forth between its own roots w10 and w11.
        for (var w = 1; w <= workers; w++)
        {
            await AddTreeAsync((w * 100 + 2, w * 100 + 1), (w * 100 + 3, w * 100 + 2));
            await AddRootsAsync(w * 100 + 10, w * 100 + 11);
        }

        await Task.WhenAll(Enumerable.Range(1, workers).Select(w => Task.Run(async () =>
        {
            await using var context = Database.CreateSecondContext(DbContext);
            var repository = RepositoryFor(context);
            var subtree = await context.Categories.SingleAsync(c => c.Id == w * 100 + 1);

            for (var move = 0; move < movesPerWorker; move++)
            {
                var targetId = w * 100 + 10 + move % 2;
                await repository.SetParentAsync(subtree, await context.Categories.SingleAsync(c => c.Id == targetId));
            }
        })));

        var stored = await StoredTreeAsync();
        TreeInvariants.AssertValid(stored);
        for (var w = 1; w <= workers; w++)
        {
            var lastTarget = w * 100 + 10 + (movesPerWorker - 1) % 2;
            stored.Single(c => c.Id == w * 100 + 3).Path.Should().Be($"|{lastTarget}|{w * 100 + 1}|{w * 100 + 2}|");
        }
    }
}
