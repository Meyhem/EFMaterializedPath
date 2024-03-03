namespace EFMaterializedPath.Test;

public class TestTreeRepository_Query : TreeRepositoryTestBase
{
    [Fact]
    public void Query()
    {
        var roots = Repository.Query()
            .Where(c => c.Id > 3 && c.Id < 5)
            .Select(r => r.Id);

        roots.Should().HaveCount(1);
        roots.Should().OnlyContain(item => item > 3 && item < 5);
    }
}
