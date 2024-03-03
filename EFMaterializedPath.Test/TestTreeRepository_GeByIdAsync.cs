namespace EFMaterializedPath.Test;

public class TestTreeRepository_GetByIdAsync : TreeRepositoryTestBase
{
    [Fact]
    public async Task GetByIdAsync()
    {
        var five = await Repository.GetByIdAsync(5);
        five.Id.Should().Be(5);

        var one = await Repository.GetByIdAsync(1);
        one.Id.Should().Be(1);
    }

    [Fact]
    public async Task GetByIdAsyncWhenNotExist()
    {
        var zero = await Repository.GetByIdAsync(0);
        zero.Should().BeNull();

        var node123 = await Repository.GetByIdAsync(123);
        node123.Should().BeNull();
    }
}
