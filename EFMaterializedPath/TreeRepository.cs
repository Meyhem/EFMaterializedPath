using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EFMaterializedPath.Entity;
using Microsoft.EntityFrameworkCore;

namespace EFMaterializedPath
{
    public class TreeRepository<TDbContext, TEntity> : ITreeRepository<TEntity>
        where TEntity : class, IMaterializedPathEntity
        where TDbContext : DbContext
    {
        private readonly TDbContext dbContext;

        public TreeRepository(TDbContext dbContext)
        {
            this.dbContext = dbContext;
        }

        public IQueryable<TEntity> QueryAncestors(TEntity entity)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));

            var path = ParsePath(entity.Path);
            if (path.Count == 0)
            {
                return Enumerable.Empty<TEntity>().AsQueryable();
            }

            return dbContext.Set<TEntity>().Where(e => path.Contains(e.Id));
        }

        public IQueryable<TEntity> QueryDescendants(TEntity entity)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));

            var descendantPathPrefix = FormatPath(ParsePath(entity.Path).Append(entity.Id));

            var set = dbContext.Set<TEntity>().Local.ToList();

            return dbContext.Set<TEntity>().Where(e => e.Path.StartsWith(descendantPathPrefix));
        }

        public IQueryable<TEntity> QueryChildren(TEntity entity)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));

            var childrenPath = FormatPath(ParsePath(entity.Path).Append(entity.Id));

            return dbContext.Set<TEntity>().Where(e => e.Path == childrenPath);
        }

        public async Task<IEnumerable<TEntity>> GetPathFromRootAsync(TEntity entity)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));

            var path = ParsePath(entity.Path);

            return (await dbContext.Set<TEntity>()
                    .Where(e => path.Contains(e.Id))
                    .ToListAsync())
                .OrderBy(o => path.IndexOf(o.Id));
        }

        public async Task<TEntity?> GetParentAsync(TEntity entity)
        {
            if (entity.ParentId == null)
            {
                return null;
            }

            return await dbContext.Set<TEntity>().FindAsync(entity.ParentId);
        }

        public async Task SetParent(TEntity entity, IMaterializedPathEntity? parent)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));

            if (parent is not null && parent.Id == 0)
                throw new InvalidOperationException(
                    $"{nameof(parent)} has {nameof(IMaterializedPathEntity.Id)}==0. " +
                    $"Entity must be saved first before it can be used as parent");

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

        private static string FormatPath(IEnumerable<int> path)
        {
            var joined = string.Join("|", path);

            if (joined.Length > 0)
            {
                joined = "|" + joined + "|";
            }

            return joined;
        }

        private static List<int> ParsePath(string? path)
        {
            return path switch
            {
                null => new List<int>(),
                _ => path.Split('|', StringSplitOptions.RemoveEmptyEntries).Select(int.Parse).ToList()
            };
        }
    }
}