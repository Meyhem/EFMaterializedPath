using EFMaterializedPath.Test.TestUtils;

namespace EFMaterializedPath.Test;

public class TestTreeRepository_QueryRoots : TreeRepositoryTestBase
{
    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        DbContext.Categories.Add(new Category {Id = 11});
        await DbContext.SaveChangesAsync();
    }

    [Fact]
    public void QueryRoots()
    {
        var roots = Repository.QueryRoots().Select(r => r.Id);
        roots.Should().BeEquivalentTo(new int[] {1, 11});
    }
}
