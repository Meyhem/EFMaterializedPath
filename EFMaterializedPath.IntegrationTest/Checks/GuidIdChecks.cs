using EFMaterializedPath.Test.TestUtils;
using Microsoft.EntityFrameworkCore;

namespace EFMaterializedPath.IntegrationTest.Checks;

/// <summary>
/// Trees keyed by <see cref="Guid"/> with <see cref="GuidIdentifierSerializer"/>.
/// </summary>
public abstract class GuidIdChecks : RelationalCheckBase
{
    [Fact]
    public async Task QueriesWorkWithGuidIds()
    {
        var repository = new TreeRepository<TestDbContext, GuidCategory, Guid>(DbContext, new GuidIdentifierSerializer());
        var root = new GuidCategory { Id = Guid.NewGuid() };
        var a = new GuidCategory { Id = Guid.NewGuid() };
        var b = new GuidCategory { Id = Guid.NewGuid() };
        DbContext.AddRange(root, a, b);
        await DbContext.SaveChangesAsync();

        // root → a → b
        await repository.SetParentAsync(a, root);
        await repository.SetParentAsync(b, a);

        (await repository.QueryDescendants(root).CountAsync()).Should().Be(2);
        (await repository.QueryChildren(root).Select(c => c.Id).ToListAsync()).Should().Equal(a.Id);
        (await repository.QueryAncestors(b).Select(c => c.Id).ToListAsync()).Should().BeEquivalentTo([root.Id, a.Id]);
        (await repository.GetPathFromRootAsync(b)).Select(c => c.Id).Should().Equal(root.Id, a.Id);

        await repository.SetParentAsync(b, root);
        DbContext.ChangeTracker.Clear();

        var stored = await DbContext.GuidCategories.AsNoTracking().SingleAsync(c => c.Id == b.Id);
        stored.Path.Should().Be($"|{root.Id}|");
        stored.Level.Should().Be(1);
        stored.ParentId.Should().Be(root.Id);
    }
}
