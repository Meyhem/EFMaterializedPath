using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace EFMaterializedPath.Test.TestUtils;

/// <summary>
/// Checks that a whole stored tree is consistent, not just the nodes a test happened to touch.
/// </summary>
public static class TreeInvariants
{
    /// <summary>Reads every category from the database (bypassing the change tracker) and checks it.</summary>
    public static async Task AssertValidAsync(TestDbContext dbContext)
    {
        AssertValid(await dbContext.Categories.AsNoTracking().ToListAsync());
    }

    /// <summary>
    /// Every root has an empty path and level 0; every other node has an existing parent, the parent's path plus the
    /// parent's id as its path, and the parent's level plus one as its level.
    /// </summary>
    /// <remarks>
    /// Together these rule out cycles too: levels would have to keep increasing all the way around one.
    /// </remarks>
    public static void AssertValid(IReadOnlyCollection<Category> rows)
    {
        var byId = rows.ToDictionary(r => r.Id);

        foreach (var row in rows)
        {
            if (row.ParentId is null)
            {
                NormalizePath(row).Should().Be("", $"node {row.Id} is a root");
                row.Level.Should().Be(0, $"node {row.Id} is a root");
                continue;
            }

            byId.Should().ContainKey(row.ParentId.Value, $"node {row.Id} points to parent {row.ParentId}");
            var parent = byId[row.ParentId.Value];
            var parentPath = NormalizePath(parent);
            var expectedPath = (parentPath.Length == 0 ? "|" : parentPath) + parent.Id + "|";

            NormalizePath(row).Should().Be(expectedPath, $"node {row.Id} is a child of {parent.Id}");
            row.Level.Should().Be(parent.Level + 1, $"node {row.Id} is a child of {parent.Id}");
        }
    }

    /// <summary>
    /// InMemory ignores the database default, so roots that were never moved have a null path there.
    /// </summary>
    public static string NormalizePath(Category category) => category.Path ?? "";
}
