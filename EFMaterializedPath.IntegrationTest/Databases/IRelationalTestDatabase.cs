using EFMaterializedPath.Test.TestUtils;

namespace EFMaterializedPath.IntegrationTest.Databases;

public interface IRelationalTestDatabase : ITestDatabase
{
    /// <summary>
    /// SQL returning one column named <c>Value</c>: every column covered by a non-primary-key index on
    /// the <c>Categories</c> table.
    /// </summary>
    string IndexedColumnsSql { get; }
}
