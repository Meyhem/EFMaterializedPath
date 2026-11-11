using EFMaterializedPath.IntegrationTest.Checks;
using EFMaterializedPath.IntegrationTest.Databases;

// Relational-only checks against Sqlite.
namespace EFMaterializedPath.IntegrationTest.Suites;

public class Sqlite_SchemaChecks : SchemaChecks
{
    protected override IRelationalTestDatabase Database => SqliteTestDatabase.Instance;
}

public class Sqlite_PathChecks : PathChecks
{
    protected override IRelationalTestDatabase Database => SqliteTestDatabase.Instance;
}

public class Sqlite_GuidIdChecks : GuidIdChecks
{
    protected override IRelationalTestDatabase Database => SqliteTestDatabase.Instance;
}

public class Sqlite_LimitChecks : LimitChecks
{
    protected override IRelationalTestDatabase Database => SqliteTestDatabase.Instance;
}

public class Sqlite_FailureChecks : FailureChecks
{
    protected override IRelationalTestDatabase Database => SqliteTestDatabase.Instance;
}

public class Sqlite_ConcurrencyChecks : ConcurrencyChecks
{
    protected override IRelationalTestDatabase Database => SqliteTestDatabase.Instance;
}
