using EFMaterializedPath.IntegrationTest.Databases;
using EFMaterializedPath.Test;
using EFMaterializedPath.Test.TestUtils;

// Re-runs every existing TreeRepository test class against Postgres.
namespace EFMaterializedPath.IntegrationTest.Suites;

[Collection(PostgresCollection.Name), Trait(Traits.Category, Traits.Docker)]
public class Postgres_DetachNodeAsync(PostgresFixture db) : TestTreeRepository_DetachNodeAsync
{
    protected override ITestDatabase Database => db;
}

[Collection(PostgresCollection.Name), Trait(Traits.Category, Traits.Docker)]
public class Postgres_GetByIdAsync(PostgresFixture db) : TestTreeRepository_GetByIdAsync
{
    protected override ITestDatabase Database => db;
}

[Collection(PostgresCollection.Name), Trait(Traits.Category, Traits.Docker)]
public class Postgres_GetParentAsync(PostgresFixture db) : TestTreeRepository_GetParentAsync
{
    protected override ITestDatabase Database => db;
}

[Collection(PostgresCollection.Name), Trait(Traits.Category, Traits.Docker)]
public class Postgres_GetPathFromRootAsync(PostgresFixture db) : TestTreeRepository_GetPathFromRootAsync
{
    protected override ITestDatabase Database => db;
}

[Collection(PostgresCollection.Name), Trait(Traits.Category, Traits.Docker)]
public class Postgres_Query(PostgresFixture db) : TestTreeRepository_Query
{
    protected override ITestDatabase Database => db;
}

[Collection(PostgresCollection.Name), Trait(Traits.Category, Traits.Docker)]
public class Postgres_QueryAncestors(PostgresFixture db) : TestTreeRepository_QueryAncestors
{
    protected override ITestDatabase Database => db;
}

[Collection(PostgresCollection.Name), Trait(Traits.Category, Traits.Docker)]
public class Postgres_QueryChildren(PostgresFixture db) : TestTreeRepository_QueryChildren
{
    protected override ITestDatabase Database => db;
}

[Collection(PostgresCollection.Name), Trait(Traits.Category, Traits.Docker)]
public class Postgres_QueryDescendants(PostgresFixture db) : TestTreeRepository_QueryDescendants
{
    protected override ITestDatabase Database => db;
}

[Collection(PostgresCollection.Name), Trait(Traits.Category, Traits.Docker)]
public class Postgres_QueryRoots(PostgresFixture db) : TestTreeRepository_QueryRoots
{
    protected override ITestDatabase Database => db;
}

[Collection(PostgresCollection.Name), Trait(Traits.Category, Traits.Docker)]
public class Postgres_QuerySiblings(PostgresFixture db) : TestTreeRepository_QuerySiblings
{
    protected override ITestDatabase Database => db;
}

[Collection(PostgresCollection.Name), Trait(Traits.Category, Traits.Docker)]
public class Postgres_RemoveNodeAsync(PostgresFixture db) : TestTreeRepository_RemoveNodeAsync
{
    protected override ITestDatabase Database => db;
}

[Collection(PostgresCollection.Name), Trait(Traits.Category, Traits.Docker)]
public class Postgres_SetParentAsync(PostgresFixture db) : TestTreeRepository_SetParentAsync
{
    protected override ITestDatabase Database => db;
}

[Collection(PostgresCollection.Name), Trait(Traits.Category, Traits.Docker)]
public class Postgres_RandomOperations(PostgresFixture db) : TestTreeRepository_RandomOperations
{
    protected override ITestDatabase Database => db;
}

[Collection(PostgresCollection.Name), Trait(Traits.Category, Traits.Docker)]
public class Postgres_ValidateAsync(PostgresFixture db) : TestTreeRepository_ValidateAsync
{
    protected override ITestDatabase Database => db;
}
