using EFMaterializedPath.IntegrationTest.Databases;
using EFMaterializedPath.Test;
using EFMaterializedPath.Test.TestUtils;

// Re-runs every existing TreeRepository test class against SqlServer.
namespace EFMaterializedPath.IntegrationTest.Suites;

[Collection(SqlServerCollection.Name), Trait(Traits.Category, Traits.Docker)]
public class SqlServer_DetachNodeAsync(SqlServerFixture db) : TestTreeRepository_DetachNodeAsync
{
    protected override ITestDatabase Database => db;
}

[Collection(SqlServerCollection.Name), Trait(Traits.Category, Traits.Docker)]
public class SqlServer_GetByIdAsync(SqlServerFixture db) : TestTreeRepository_GetByIdAsync
{
    protected override ITestDatabase Database => db;
}

[Collection(SqlServerCollection.Name), Trait(Traits.Category, Traits.Docker)]
public class SqlServer_GetParentAsync(SqlServerFixture db) : TestTreeRepository_GetParentAsync
{
    protected override ITestDatabase Database => db;
}

[Collection(SqlServerCollection.Name), Trait(Traits.Category, Traits.Docker)]
public class SqlServer_GetPathFromRootAsync(SqlServerFixture db) : TestTreeRepository_GetPathFromRootAsync
{
    protected override ITestDatabase Database => db;
}

[Collection(SqlServerCollection.Name), Trait(Traits.Category, Traits.Docker)]
public class SqlServer_Query(SqlServerFixture db) : TestTreeRepository_Query
{
    protected override ITestDatabase Database => db;
}

[Collection(SqlServerCollection.Name), Trait(Traits.Category, Traits.Docker)]
public class SqlServer_QueryAncestors(SqlServerFixture db) : TestTreeRepository_QueryAncestors
{
    protected override ITestDatabase Database => db;
}

[Collection(SqlServerCollection.Name), Trait(Traits.Category, Traits.Docker)]
public class SqlServer_QueryChildren(SqlServerFixture db) : TestTreeRepository_QueryChildren
{
    protected override ITestDatabase Database => db;
}

[Collection(SqlServerCollection.Name), Trait(Traits.Category, Traits.Docker)]
public class SqlServer_QueryDescendants(SqlServerFixture db) : TestTreeRepository_QueryDescendants
{
    protected override ITestDatabase Database => db;
}

[Collection(SqlServerCollection.Name), Trait(Traits.Category, Traits.Docker)]
public class SqlServer_QueryRoots(SqlServerFixture db) : TestTreeRepository_QueryRoots
{
    protected override ITestDatabase Database => db;
}

[Collection(SqlServerCollection.Name), Trait(Traits.Category, Traits.Docker)]
public class SqlServer_QuerySiblings(SqlServerFixture db) : TestTreeRepository_QuerySiblings
{
    protected override ITestDatabase Database => db;
}

[Collection(SqlServerCollection.Name), Trait(Traits.Category, Traits.Docker)]
public class SqlServer_RemoveNodeAsync(SqlServerFixture db) : TestTreeRepository_RemoveNodeAsync
{
    protected override ITestDatabase Database => db;
}

[Collection(SqlServerCollection.Name), Trait(Traits.Category, Traits.Docker)]
public class SqlServer_SetParentAsync(SqlServerFixture db) : TestTreeRepository_SetParentAsync
{
    protected override ITestDatabase Database => db;
}

[Collection(SqlServerCollection.Name), Trait(Traits.Category, Traits.Docker)]
public class SqlServer_RandomOperations(SqlServerFixture db) : TestTreeRepository_RandomOperations
{
    protected override ITestDatabase Database => db;
}

[Collection(SqlServerCollection.Name), Trait(Traits.Category, Traits.Docker)]
public class SqlServer_ValidateAsync(SqlServerFixture db) : TestTreeRepository_ValidateAsync
{
    protected override ITestDatabase Database => db;
}
