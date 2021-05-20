using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EFMaterializedPath.Entity;
using Microsoft.EntityFrameworkCore;

namespace EFMaterializedPath
{
    public class TreeRepository<TDbContext, TEntity, TId> : ITreeRepository<TEntity, TId>
        where TEntity : class, IMaterializedPathEntity<TId>
        where TDbContext : DbContext
        where TId : struct
    {
        private readonly TDbContext dbContext;
        private readonly IIdentifierSerializer<TId> identifierSerializer;

        public TreeRepository(TDbContext dbContext, IIdentifierSerializer<TId> identifierSerializer)
        {
            this.dbContext = dbContext;
            this.identifierSerializer = identifierSerializer;
        }

        public IQueryable<TEntity> QueryRoots()
        {
            return dbContext.Set<TEntity>().Where(ent => ent.ParentId == null);
        }

        public IQueryable<TEntity> Query()
        {
            return dbContext.Set<TEntity>();
        }

        public async Task<TEntity> GetByIdAsync(TId id)
        {
            return await dbContext.Set<TEntity>().FindAsync(id);
        }

        public IQueryable<TEntity> QueryAncestors(TEntity entity)
        {
            AssertIsStoredEntity(entity);

            var path = ParsePath(entity.Path);
            
            if (path.Count == 0)
            {
                return Enumerable.Empty<TEntity>().AsQueryable();
            }

            return dbContext.Set<TEntity>().Where(e => path.Contains(e.Id));
        }

        public IQueryable<TEntity> QueryDescendants(TEntity entity)
        {
            AssertIsStoredEntity(entity);

            var descendantPathPrefix = FormatPath(ParsePath(entity.Path).Append(entity.Id));

            return dbContext.Set<TEntity>().Where(e => e.Path.StartsWith(descendantPathPrefix));
        }

        public IQueryable<TEntity> QueryChildren(TEntity entity)
        {
            AssertIsStoredEntity(entity);

            var childrenPath = FormatPath(ParsePath(entity.Path).Append(entity.Id));

            return dbContext.Set<TEntity>().Where(e => e.Path == childrenPath);
        }

        public IQueryable<TEntity> QuerySiblings(TEntity entity)
        {
            AssertIsStoredEntity(entity);
            var siblingPath = FormatPath(ParsePath(entity.Path));
            
            return dbContext.Set<TEntity>()
                .Where(e => e.Path == siblingPath && !e.Id.Equals(entity.Id));
        }

        public async Task<IEnumerable<TEntity>> GetPathFromRootAsync(TEntity entity)
        {
            AssertIsStoredEntity(entity);

            var path = ParsePath(entity.Path);

            return (await dbContext.Set<TEntity>()
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

            return await dbContext.Set<TEntity>().FindAsync(entity.ParentId);
        }

        public async Task SetParentAsync(TEntity entity, IMaterializedPathEntity<TId>? parent)
        {
            AssertIsStoredEntity(entity);

            if (parent is not null)
            {
                AssertIsStoredEntity(parent!);
            }

            var path = ParsePath(parent?.Path);

            if (parent is not null)
            {
                path.Add(parent.Id);
            }

            var oldPath = entity.Path;
            var newPath = FormatPath(path);

            foreach (var descendant in await QueryDescendants(entity).ToListAsync())
            {
                var newDescendantPath = ParsePath(descendant.Path.Replace(oldPath, newPath));
                descendant.Path = FormatPath(newDescendantPath);
                descendant.Level = newDescendantPath.Count;
            }

            entity.Level = path.Count;
            entity.Path = newPath;

            entity.ParentId = parent?.Id;

            await dbContext.SaveChangesAsync();
        }

        public async Task DetachNodeAsync(TEntity entity)
        {
            AssertIsStoredEntity(entity);

            var children = QueryChildren(entity);
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
            
            dbContext.Set<TEntity>().Remove(entity);
            await dbContext.SaveChangesAsync();
        }

        private static void AssertIsStoredEntity(IMaterializedPathEntity<TId> entity)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));
            if (entity.Id.Equals(default(TId)))
                throw new InvalidOperationException($"{nameof(entity)} does not have valid Id. Save this entity first.");
        }
        
        private string FormatPath(IEnumerable<TId> path)
        {
            var joined = string.Join(
                "|", 
                path.Select(item => 
                    identifierSerializer.SerializeIdentifier(item)));

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
            return split.Select(item => identifierSerializer.DeserializeIdentifier(item)).ToList();
        }
    }
}