using System.Linq;
using System.Threading.Tasks;
using EFMaterializedPath.Entity;

namespace EFMaterializedPath
{
    public interface ITreeRepository<TEntity> where TEntity : class, IMaterializedPathEntity
    {
        IQueryable<TEntity> QueryAncestors(TEntity entity);
        IQueryable<TEntity> QueryDescendants(TEntity entity);
        IQueryable<TEntity> QueryChildren(TEntity entity);
        Task SetParent(TEntity entity, IMaterializedPathEntity? parent);
    }
}