using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using EFMaterializedPath.Entity;
using Microsoft.EntityFrameworkCore;

namespace EFMaterializedPath.Core
{
    public class TreeRepository<TEntity>
        where TEntity : class, IMaterializedPathEntity
    {
        private readonly DbContext dbContext;

        public TreeRepository(DbContext dbContext)
        {
            this.dbContext = dbContext;
        }

        public IQueryable<TEntity> GetAncestors(TEntity entity)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));

            var path = ParsePath(entity.Path);
            if (path.Count == 0)
            {
                return Enumerable.Empty<TEntity>().AsQueryable();
            }

            return dbContext.Set<TEntity>().Where(e => path.Contains(e.Id));
        }

        public IQueryable<TEntity> GetDescendants(TEntity entity)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));

            var descendantPathPrefix = FormatPath(ParsePath(entity.Path).Append(entity.Id));

            return dbContext.Set<TEntity>().Where(e => e.Path.StartsWith(descendantPathPrefix));
        }

        public IQueryable<TEntity> GetChildren(TEntity entity)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));
            
            var childrenPath = FormatPath(ParsePath(entity.Path).Append(entity.Id));
            
            return dbContext.Set<TEntity>().Where(e => e.Path == childrenPath);
        }
        
        public void SetParent(TEntity entity, IMaterializedPathEntity? parent)
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

            foreach (var descendant in GetDescendants(entity))
            {
                var newDescendantPath = ParsePath(descendant.Path.Replace(oldPath, newPath));
                descendant.Path = FormatPath(newDescendantPath);
                descendant.Level = newDescendantPath.Count;
            }

            entity.Level = path.Count;
            entity.Path = newPath;
            entity.ParentId = parent?.Id;
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