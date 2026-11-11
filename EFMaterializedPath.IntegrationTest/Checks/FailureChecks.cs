using System.Collections.Generic;
using System.Threading;
using EFMaterializedPath.Test.TestUtils;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace EFMaterializedPath.IntegrationTest.Checks;

/// <summary>
/// What is left in the database when a multi-step operation (detach and remove save once per child) fails half way,
/// with and without a surrounding transaction.
/// </summary>
public abstract class FailureChecks : RelationalCheckBase
{
    private readonly FailingSaveInterceptor failingSave = new();

    protected override IInterceptor[] Interceptors => [failingSave];

    //     1
    //     │
    // ┌───2───┐
    // 3   4   5
    //     │
    //     6
    private Task<Dictionary<int, Category>> AddTestTreeAsync() => AddTreeAsync((2, 1), (3, 2), (4, 2), (5, 2), (6, 4));

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public async Task FailedSaveDuringDetachLeavesConsistentTree(int failingSaveNumber)
    {
        var nodes = await AddTestTreeAsync();
        var before = await StoredTreeAsync();

        failingSave.FailOnSave(failingSaveNumber);
        Func<Task> detach = async () => await Repository.DetachNodeAsync(nodes[2]);
        await detach.Should().ThrowAsync<InjectedFailureException>();

        // Without a transaction the operation is not atomic: the children moved before the failure stay moved. Each
        // save is still a complete move, so the stored tree is a valid one.
        var after = await StoredTreeAsync();
        TreeInvariants.AssertValid(after);
        after.Count(c => c.ParentId == 1 && c.Id != 2).Should().Be(failingSaveNumber - 1, "one child moves per save");
        after.Single(c => c.Id == 2).Should().BeEquivalentTo(before.Single(c => c.Id == 2));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(5)]
    public async Task FailedSaveDuringRemoveInsideTransactionRollsBackEverything(int failingSaveNumber)
    {
        var nodes = await AddTestTreeAsync();
        var before = await StoredTreeAsync();

        await using (var transaction = await DbContext.Database.BeginTransactionAsync())
        {
            failingSave.FailOnSave(failingSaveNumber);
            Func<Task> remove = async () => await Repository.RemoveNodeAsync(nodes[2]);
            await remove.Should().ThrowAsync<InjectedFailureException>();
            await transaction.RollbackAsync();
        }

        (await StoredTreeAsync()).Should().BeEquivalentTo(before, o => o.WithStrictOrdering());
    }

    [Fact]
    public async Task RollbackUndoesSuccessfulOperations()
    {
        var nodes = await AddTestTreeAsync();
        var before = await StoredTreeAsync();

        await using (var transaction = await DbContext.Database.BeginTransactionAsync())
        {
            await Repository.SetParentAsync(nodes[4], nodes[1]);
            await Repository.DetachNodeAsync(nodes[2]);
            await Repository.RemoveNodeAsync(nodes[3]);
            await transaction.RollbackAsync();
        }

        (await StoredTreeAsync()).Should().BeEquivalentTo(before, o => o.WithStrictOrdering());
    }

    [Fact]
    public async Task CommitKeepsAllOperations()
    {
        var nodes = await AddTestTreeAsync();

        await using (var transaction = await DbContext.Database.BeginTransactionAsync())
        {
            await Repository.DetachNodeAsync(nodes[2]);
            await Repository.RemoveNodeAsync(nodes[4]);
            await transaction.CommitAsync();
        }

        var after = await StoredTreeAsync();
        TreeInvariants.AssertValid(after);
        after.Select(c => (c.Id, c.ParentId)).Should().Equal((1, null), (2, null), (3, 1), (5, 1), (6, 1));
    }

    private sealed class InjectedFailureException : Exception;

    /// <summary>Makes the n-th <c>SaveChangesAsync</c> after <see cref="FailOnSave"/> throw before reaching the database.</summary>
    private sealed class FailingSaveInterceptor : SaveChangesInterceptor
    {
        private int savesUntilFailure;

        public void FailOnSave(int number) => savesUntilFailure = number;

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default
        )
        {
            if (savesUntilFailure > 0 && --savesUntilFailure == 0)
            {
                throw new InjectedFailureException();
            }

            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }
    }
}
