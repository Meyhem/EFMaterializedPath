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
        AssertIsStoredEntity(entity);

        // Materialize first: SetParentAsync queries and saves, which providers without multiple active result sets
        // (PostgreSQL, SQL Server) reject while the children query is still streaming.
        var children = await QueryChildren(entity).ToListAsync();
        var parent = await GetParentAsync(entity);

        foreach (var child in children)
        {
            await SetParentAsync(child, parent);
        }

        await SetParentAsync(entity, null);
    }

    public async Task RemoveNodeAsync(TEntity entity)
    {
        AssertIsStoredEntity(entity);

        await DetachNodeAsync(entity);

        Set.Remove(TrackedInstanceOf(entity));
        await dbContext.SaveChangesAsync();
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
