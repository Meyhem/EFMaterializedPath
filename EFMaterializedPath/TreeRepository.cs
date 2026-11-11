using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EFMaterializedPath.Entity;
using Microsoft.EntityFrameworkCore;

namespace EFMaterializedPath;

public class TreeRepository<TDbContext, TEntity, TId>(
    TDbContext dbContext,
    IIdentifierSerializer<TId> identifierSerializer
)
    : ITreeRepository<TEntity, TId>
    where TEntity : class, IMaterializedPathEntity<TId>
    where TDbContext : DbContext
    where TId : struct
{
    private DbSet<TEntity> Set => dbContext.Set<TEntity>() ??
                                  throw new InvalidOperationException(
                                      $"Failed to retrieve DbSet for {typeof(TEntity).FullName}"
                                  );

    public IQueryable<TEntity> QueryRoots()
    {
        return Set.Where(ent => ent.ParentId == null);
    }

    public IQueryable<TEntity> Query()
    {
        return Set;
    }

    public async Task<TEntity?> GetByIdAsync(TId id)
    {
        return await Set.FindAsync(id);
    }

    public IQueryable<TEntity> QueryAncestors(TEntity entity)
    {
        AssertIsStoredEntity(entity);

        var path = ParsePath(entity.Path);

        if (path.Count == 0)
        {
            // Still an EF query (not Enumerable.Empty), so async operators and Include keep working on roots.
            return Set.Where(e => false);
        }

        return Set.Where(e => path.Contains(e.Id));
    }

    public IQueryable<TEntity> QueryDescendants(TEntity entity)
    {
        AssertIsStoredEntity(entity);

        var descendantPathPrefix = FormatPath(ParsePath(entity.Path).Append(entity.Id));

        return Set.Where(e => e.Path.StartsWith(descendantPathPrefix));
    }

    public IQueryable<TEntity> QueryChildren(TEntity entity)
    {
        AssertIsStoredEntity(entity);

        var childrenPath = FormatPath(ParsePath(entity.Path).Append(entity.Id));

        return Set.Where(e => e.Path == childrenPath);
    }

    public IQueryable<TEntity> QuerySiblings(TEntity entity)
    {
        AssertIsStoredEntity(entity);
        var siblingPath = FormatPath(ParsePath(entity.Path));

        return Set
            .Where(e => e.Path == siblingPath && !e.Id.Equals(entity.Id));
    }

    public async Task<IEnumerable<TEntity>> GetPathFromRootAsync(TEntity entity)
    {
        AssertIsStoredEntity(entity);

        var path = ParsePath(entity.Path);

        return (await Set
                .Where(e => path.Contains(e.Id))
                .ToListAsync())
            .OrderBy(o => path.IndexOf(o.Id));
    }

    public bool IsAncestorOf(TEntity ancestor, TEntity node)
    {
        AssertIsStoredEntity(ancestor);
        AssertIsStoredEntity(node);

        return ParsePath(node.Path).Contains(ancestor.Id);
    }

    public bool IsDescendantOf(TEntity node, TEntity ancestor) => IsAncestorOf(ancestor, node);

    public async Task<IReadOnlyList<TreeProblem<TId>>> ValidateAsync()
    {
        var rows = (await Set
                .AsNoTracking()
                .Select(e => new { e.Id, e.ParentId, e.Path, e.Level })
                .ToListAsync())
            .ToDictionary(r => r.Id);

        var problems = new List<TreeProblem<TId>>();

        // Ancestors of each node as given by ParentId, root first; null when they cannot be determined (the chain
        // up from the node ends in a missing parent or a cycle).
        var ancestors = new Dictionary<TId, List<TId>?>();

        foreach (var id in rows.Keys)
        {
            if (ancestors.ContainsKey(id))
            {
                continue;
            }

            // Walk up until reaching a root, a node already resolved, a missing parent, or a node seen on this walk.
            // Iterative, as trees can be deeper than a safe recursion depth on databases without a path limit.
            var chain = new List<TId>();
            var positionInChain = new Dictionary<TId, int>();
            var current = id;
            while (true)
            {
                positionInChain[current] = chain.Count;
                chain.Add(current);

                if (rows[current].ParentId is not { } parentId)
                {
                    ancestors[current] = [];
                    break;
                }

                if (!rows.ContainsKey(parentId))
                {
                    problems.Add(new TreeProblem<TId>(current, TreeProblemKind.MissingParent,
                        $"Node {current} points to parent {parentId}, which does not exist."));
                    ancestors[current] = null;
                    break;
                }

                if (positionInChain.TryGetValue(parentId, out var cycleStart))
                {
                    foreach (var inCycle in chain.Skip(cycleStart))
                    {
                        problems.Add(new TreeProblem<TId>(inCycle, TreeProblemKind.Cycle,
                            $"Node {inCycle} is part of a ParentId cycle and has no root."));
                        ancestors[inCycle] = null;
                    }

                    break;
                }

                if (ancestors.TryGetValue(parentId, out var parentAncestors))
                {
                    ancestors[current] = parentAncestors is null ? null : [..parentAncestors, parentId];
                    break;
                }

                current = parentId;
            }

            // Each chain element's parent is the next element, which is resolved by now.
            for (var i = chain.Count - 2; i >= 0; i--)
            {
                if (ancestors.ContainsKey(chain[i]))
                {
                    continue;
                }

                var parentAncestors = ancestors[chain[i + 1]];
                ancestors[chain[i]] = parentAncestors is null ? null : [..parentAncestors, chain[i + 1]];
            }
        }

        foreach (var row in rows.Values)
        {
            if (ancestors[row.Id] is not { } expected)
            {
                continue;
            }

            // InMemory ignores the column default, so a never-moved root can have a null Path.
            var storedPath = row.Path ?? "";
            var expectedPath = FormatPath(expected);
            if (storedPath != expectedPath)
            {
                problems.Add(new TreeProblem<TId>(row.Id, TreeProblemKind.WrongPath,
                    $"Node {row.Id} has path '{storedPath}', expected '{expectedPath}'."));
            }

            if (row.Level != expected.Count)
            {
                problems.Add(new TreeProblem<TId>(row.Id, TreeProblemKind.WrongLevel,
                    $"Node {row.Id} has level {row.Level}, expected {expected.Count}."));
            }
        }

        // Strongly typed ids are often not comparable; their problems keep the order they were found in.
        var idsAreComparable = typeof(IComparable<TId>).IsAssignableFrom(typeof(TId)) ||
                               typeof(IComparable).IsAssignableFrom(typeof(TId));

        return idsAreComparable
            ? problems.OrderBy(p => p.Id).ThenBy(p => p.Kind).ToList()
            : problems;
    }

    public async Task<TEntity?> GetParentAsync(TEntity entity)
    {
        AssertIsStoredEntity(entity);

        if (entity.ParentId == null)
        {
            return null;
        }

        return await Set.FindAsync(entity.ParentId);
    }

    public async Task SetParentAsync(TEntity entity, IMaterializedPathEntity<TId>? parent)
    {
        AssertIsStoredEntity(entity);

        if (parent is not null)
        {
            AssertIsStoredEntity(parent);
        }

        var path = ParsePath(parent?.Path);

        if (parent is not null)
        {
            if (parent.Id.Equals(entity.Id))
            {
                throw new InvalidOperationException("Entity cannot be its own parent.");
            }

            if (path.Contains(entity.Id))
            {
                throw new InvalidOperationException(
                    "Entity cannot be moved under one of its own descendants."
                );
            }

            path.Add(parent.Id);
        }

        // The entity's own changes are only saved if the context tracks it. An untracked entity is attached, or,
        // if the context already tracks another instance of the same row, that instance is updated as well.
        var tracked = TrackedInstanceOf(entity);

        var oldPathLength = ParsePath(entity.Path).Count;
        var newPath = FormatPath(path);

        foreach (var descendant in await QueryDescendants(entity).ToListAsync())
        {
            // Swap the entity's old ancestors for the new ones, keep the rest (entity id and below).
            var newDescendantPath = path.Concat(ParsePath(descendant.Path).Skip(oldPathLength)).ToList();
            descendant.Path = FormatPath(newDescendantPath);
            descendant.Level = newDescendantPath.Count;
        }

        foreach (var target in new[] { entity, tracked }.Distinct())
        {
            target.Level = path.Count;
            target.Path = newPath;
            target.ParentId = parent?.Id;
        }

        await dbContext.SaveChangesAsync();
    }

    public async Task DetachNodeAsync(TEntity entity)
    {
        var undo = new List<Action>();
        await DetachWithoutSavingAsync(entity, undo);
        await SaveOrUndoAsync(undo);
    }

    public async Task RemoveNodeAsync(TEntity entity)
    {
        var undo = new List<Action>();
        var tracked = await DetachWithoutSavingAsync(entity, undo);

        Set.Remove(tracked);
        await SaveOrUndoAsync(undo);
    }

    /// <summary>
    /// Saves the context; if the save fails, runs <paramref name="undo"/> (newest first) so the entities and the change
    /// tracker are as they were before the operation, then rethrows.
    /// </summary>
    private async Task SaveOrUndoAsync(List<Action> undo)
    {
        try
        {
            await dbContext.SaveChangesAsync();
        }
        catch
        {
            for (var i = undo.Count - 1; i >= 0; i--)
            {
                undo[i]();
            }

            throw;
        }
    }

    /// <summary>
    /// Records how to put <paramref name="instance"/>'s tree properties and its change tracking state back to what they
    /// are now.
    /// </summary>
    private Action Snapshot(TEntity instance)
    {
        var (path, level, parentId) = (instance.Path, instance.Level, instance.ParentId);
        var entry = dbContext.Entry(instance);
        var state = entry.State;
        string[] treeProperties =
            [nameof(IMaterializedPathEntity<TId>.Path), nameof(IMaterializedPathEntity<TId>.Level),
                nameof(IMaterializedPathEntity<TId>.ParentId)];
        var modified = state is EntityState.Unchanged or EntityState.Modified
            ? treeProperties.Select(p => entry.Property(p).IsModified).ToArray()
            : null;

        return () =>
        {
            (instance.Path, instance.Level, instance.ParentId) = (path, level, parentId);

            if (state is EntityState.Detached or EntityState.Added or EntityState.Deleted)
            {
                entry.State = state;
                return;
            }

            if (entry.State is EntityState.Deleted or EntityState.Detached)
            {
                entry.State = EntityState.Unchanged;
            }

            entry.DetectChanges();
            for (var i = 0; i < treeProperties.Length; i++)
            {
                entry.Property(treeProperties[i]).IsModified = modified![i];
            }
        };
    }

    /// <summary>
    /// Makes <paramref name="entity"/> a root and removes it from the paths of its descendants, whose children are
    /// re-parented to its parent. Does not save; adds to <paramref name="undo"/> what reverts every change it makes.
    /// </summary>
    /// <returns>The instance of <paramref name="entity"/>'s row that the context tracks.</returns>
    private async Task<TEntity> DetachWithoutSavingAsync(TEntity entity, List<Action> undo)
    {
        AssertIsStoredEntity(entity);

        undo.Add(Snapshot(entity));
        var tracked = TrackedInstanceOf(entity);
        if (!ReferenceEquals(tracked, entity))
        {
            undo.Add(Snapshot(tracked));
        }

        // The entity's id sits at this index in every descendant's path, right after the entity's own ancestors.
        var entityIndex = ParsePath(entity.Path).Count;

        foreach (var descendant in await QueryDescendants(entity).ToListAsync())
        {
            undo.Add(Snapshot(descendant));

            var path = ParsePath(descendant.Path);
            path.RemoveAt(entityIndex);
            descendant.Path = FormatPath(path);
            descendant.Level = path.Count;

            if (descendant.ParentId.Equals(entity.Id))
            {
                descendant.ParentId = entity.ParentId;
            }
        }

        foreach (var target in new[] { entity, tracked }.Distinct())
        {
            target.Path = "";
            target.Level = 0;
            target.ParentId = null;
        }

        return tracked;
    }

    /// <summary>
    /// Returns the instance of <paramref name="entity"/>'s row that the context tracks: the entity itself if it is
    /// tracked, another tracked instance with the same id if there is one, otherwise the entity after attaching it.
    /// </summary>
    private TEntity TrackedInstanceOf(TEntity entity)
    {
        if (dbContext.Entry(entity).State != EntityState.Detached)
        {
            return entity;
        }

        var trackedEntry = Set.Local.FindEntry(entity.Id);
        if (trackedEntry is not null)
        {
            return trackedEntry.Entity;
        }

        Set.Attach(entity);
        return entity;
    }

    private static void AssertIsStoredEntity(IMaterializedPathEntity<TId> entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        if (entity.Id.Equals(default(TId)))
        {
            throw new InvalidOperationException(
                $"{nameof(entity)} does not have valid Id. Save this entity first."
            );
        }
    }

    private string FormatPath(IEnumerable<TId> path)
    {
        var joined = string.Join(
            "|",
            path.Select(
                identifierSerializer.SerializeIdentifier
            )
        );

        if (joined.Length > 0)
        {
            joined = "|" + joined + "|";
        }

        return joined;
    }

    private List<TId> ParsePath(string? path)
    {
        if (string.IsNullOrEmpty(path)) return new List<TId>();

        var split = path.Split('|', StringSplitOptions.RemoveEmptyEntries);
        return split.Select(identifierSerializer.DeserializeIdentifier).ToList();
    }
}
