using EFMaterializedPath.IntegrationTest.Checks;
using EFMaterializedPath.IntegrationTest.Databases;

// Relational-only checks against Postgres.
namespace EFMaterializedPath.IntegrationTest.Suites;

[Collection(PostgresCollection.Name), Trait(Traits.Category, Traits.Docker)]
public class Postgres_SchemaChecks(PostgresFixture db) : SchemaChecks
{
    protected override IRelationalTestDatabase Database => db;
}

[Collection(PostgresCollection.Name), Trait(Traits.Category, Traits.Docker)]
public class Postgres_PathChecks(PostgresFixture db) : PathChecks
{
    protected override IRelationalTestDatabase Database => db;
}

[Collection(PostgresCollection.Name), Trait(Traits.Category, Traits.Docker)]
public class Postgres_GuidIdChecks(PostgresFixture db) : GuidIdChecks
{
    protected override IRelationalTestDatabase Database => db;
}

[Collection(PostgresCollection.Name), Trait(Traits.Category, Traits.Docker)]
public class Postgres_LimitChecks(PostgresFixture db) : LimitChecks
{
    protected override IRelationalTestDatabase Database => db;
}

[Collection(PostgresCollection.Name), Trait(Traits.Category, Traits.Docker)]
public class Postgres_FailureChecks(PostgresFixture db) : FailureChecks
{
    protected override IRelationalTestDatabase Database => db;
}

[Collection(PostgresCollection.Name), Trait(Traits.Category, Traits.Docker)]
public class Postgres_ConcurrencyChecks(PostgresFixture db) : ConcurrencyChecks
{
    protected override IRelationalTestDatabase Database => db;
}
