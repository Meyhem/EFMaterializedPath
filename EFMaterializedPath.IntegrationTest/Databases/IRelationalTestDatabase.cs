using EFMaterializedPath.Test.TestUtils;
using Microsoft.EntityFrameworkCore;

namespace EFMaterializedPath.IntegrationTest.Databases;

public interface IRelationalTestDatabase : ITestDatabase
{
    /// <summary>
    /// SQL returning one column named <c>Value</c>: every column covered by a non-primary-key index on
    /// the <c>Categories</c> table.
    /// </summary>
    string IndexedColumnsSql { get; }

    /// <summary>Column type the mapping produces for <c>Path</c> on this database.</summary>
    string PathColumnType { get; }

    /// <summary>
    /// Deepest level a node keyed by <see cref="Guid"/> (37 characters per level) can have before its path no
    /// longer fits the indexed <c>Path</c> column; null if there is no practical limit.
    /// </summary>
    int? MaxGuidTreeDepth { get; }

    /// <summary>Prefix matching (<c>LIKE</c>, used for descendants) ignores case by default.</summary>
    bool PrefixMatchIgnoresCase { get; }

    /// <summary>Equality (used for children and siblings) ignores case by default.</summary>
    bool EqualityIgnoresCase { get; }

    /// <summary>The descendants query can use the <c>Path</c> index with the default schema and settings.</summary>
    bool DescendantsQueryUsesPathIndex { get; }

    /// <summary>
    /// Whether several contexts can work on the same database at the same time. False for in-memory SQLite, where
    /// the database lives in a single connection that a second context has to share.
    /// </summary>
    bool SupportsParallelConnections { get; }

    /// <summary>
    /// A second, independent context (own change tracker) on the database of <paramref name="first"/>, as another
    /// request or process would have. The caller disposes it.
    /// </summary>
    TestDbContext CreateSecondContext(TestDbContext first);

    /// <summary>Returns the database's execution plan for <paramref name="query"/> as text.</summary>
    Task<string> ExplainAsync(TestDbContext context, IQueryable<Category> query);
}
