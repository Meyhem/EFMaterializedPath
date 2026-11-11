using EFMaterializedPath.Test.TestUtils;

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

    [Theory]
    [InlineData(nameof(ITreeRepository<Category, int>.Query))]
    [InlineData(nameof(ITreeRepository<Category, int>.QueryRoots))]
    public void ReturnsNonNullableElements(string method)
    {
        // Open generic definition: nullability annotations live there, not on the constructed type.
        var returnParameter = typeof(ITreeRepository<,>).GetMethod(method)!.ReturnParameter;
        var info = new System.Reflection.NullabilityInfoContext().Create(returnParameter);
        info.GenericTypeArguments.Single().ReadState.Should().Be(System.Reflection.NullabilityState.NotNull);
    }
}
