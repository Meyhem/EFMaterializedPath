using EFMaterializedPath.IntegrationTest.Databases;
using EFMaterializedPath.Test;
using EFMaterializedPath.Test.TestUtils;

// Re-runs every existing TreeRepository test class against Sqlite.
namespace EFMaterializedPath.IntegrationTest.Suites;

public class Sqlite_DetachNodeAsync : TestTreeRepository_DetachNodeAsync
{
    protected override ITestDatabase Database => SqliteTestDatabase.Instance;
}

public class Sqlite_GetByIdAsync : TestTreeRepository_GetByIdAsync
{
    protected override ITestDatabase Database => SqliteTestDatabase.Instance;
}

public class Sqlite_GetParentAsync : TestTreeRepository_GetParentAsync
{
    protected override ITestDatabase Database => SqliteTestDatabase.Instance;
}

public class Sqlite_GetPathFromRootAsync : TestTreeRepository_GetPathFromRootAsync
{
    protected override ITestDatabase Database => SqliteTestDatabase.Instance;
}

public class Sqlite_Query : TestTreeRepository_Query
{
    protected override ITestDatabase Database => SqliteTestDatabase.Instance;
}

public class Sqlite_QueryAncestors : TestTreeRepository_QueryAncestors
{
    protected override ITestDatabase Database => SqliteTestDatabase.Instance;
}

public class Sqlite_QueryChildren : TestTreeRepository_QueryChildren
{
    protected override ITestDatabase Database => SqliteTestDatabase.Instance;
}

public class Sqlite_QueryDescendants : TestTreeRepository_QueryDescendants
{
    protected override ITestDatabase Database => SqliteTestDatabase.Instance;
}

public class Sqlite_QueryRoots : TestTreeRepository_QueryRoots
{
    protected override ITestDatabase Database => SqliteTestDatabase.Instance;
}

public class Sqlite_QuerySiblings : TestTreeRepository_QuerySiblings
{
    protected override ITestDatabase Database => SqliteTestDatabase.Instance;
}

public class Sqlite_RemoveNodeAsync : TestTreeRepository_RemoveNodeAsync
{
    protected override ITestDatabase Database => SqliteTestDatabase.Instance;
}

public class Sqlite_SetParentAsync : TestTreeRepository_SetParentAsync
{
    protected override ITestDatabase Database => SqliteTestDatabase.Instance;
}

public class Sqlite_RandomOperations : TestTreeRepository_RandomOperations
{
    protected override ITestDatabase Database => SqliteTestDatabase.Instance;
}

public class Sqlite_ValidateAsync : TestTreeRepository_ValidateAsync
{
    protected override ITestDatabase Database => SqliteTestDatabase.Instance;
}
