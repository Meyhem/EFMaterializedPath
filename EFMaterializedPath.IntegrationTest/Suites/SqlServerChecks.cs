using EFMaterializedPath.IntegrationTest.Checks;
using EFMaterializedPath.IntegrationTest.Databases;

// Relational-only checks against SqlServer.
namespace EFMaterializedPath.IntegrationTest.Suites;

[Collection(SqlServerCollection.Name), Trait(Traits.Category, Traits.Docker)]
public class SqlServer_SchemaChecks(SqlServerFixture db) : SchemaChecks
{
    protected override IRelationalTestDatabase Database => db;
}

[Collection(SqlServerCollection.Name), Trait(Traits.Category, Traits.Docker)]
public class SqlServer_PathChecks(SqlServerFixture db) : PathChecks
{
    protected override IRelationalTestDatabase Database => db;
}

[Collection(SqlServerCollection.Name), Trait(Traits.Category, Traits.Docker)]
public class SqlServer_GuidIdChecks(SqlServerFixture db) : GuidIdChecks
{
    protected override IRelationalTestDatabase Database => db;
}

[Collection(SqlServerCollection.Name), Trait(Traits.Category, Traits.Docker)]
public class SqlServer_LimitChecks(SqlServerFixture db) : LimitChecks
{
    protected override IRelationalTestDatabase Database => db;
}

[Collection(SqlServerCollection.Name), Trait(Traits.Category, Traits.Docker)]
public class SqlServer_FailureChecks(SqlServerFixture db) : FailureChecks
{
    protected override IRelationalTestDatabase Database => db;
}

[Collection(SqlServerCollection.Name), Trait(Traits.Category, Traits.Docker)]
public class SqlServer_ConcurrencyChecks(SqlServerFixture db) : ConcurrencyChecks
{
    protected override IRelationalTestDatabase Database => db;
}
