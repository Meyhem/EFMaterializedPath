using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EFMaterializedPath.Entity;

namespace EFMaterializedPath
{
    /// <summary>
    /// Tree repository for manipulation with <see cref="IMaterializedPathEntity"/>
    /// </summary>
    /// <typeparam name="TEntity">Entity implementing <see cref="IMaterializedPathEntity"/></typeparam>
    public interface ITreeRepository<TEntity> where TEntity : class, IMaterializedPathEntity
    {
        /// <summary>
        /// Queries all ancestor nodes of current node. If you need to preserve order from to to bottom,
        /// then see <see cref="GetPathFromRootAsync"/> 
        ///  
        /// </summary>
        /// <param name="entity">Entity to get ancestors of</param>
        /// <returns>Unordered <see cref="IQueryable&lt;TEntity&gt;"/> of ancestor nodes</returns>
        IQueryable<TEntity> QueryAncestors(TEntity entity);
        
        /// <summary>
        /// Queries all descendant nodes of <paramref name="entity"/>
        /// </summary>
        /// <param name="entity">Entity to get descendants of</param>
        /// <returns>Unordered <see cref="IQueryable&lt;TEntity&gt;"/> of descendant nodes</returns>
        IQueryable<TEntity> QueryDescendants(TEntity entity);
        
        /// <summary>
        /// Queries all direct children nodes of <paramref name="entity"/>.
        /// </summary>
        /// <param name="entity">Entity to get children of</param>
        /// <returns>Unordered <see cref="IQueryable&lt;TEntity&gt;"/> of children nodes</returns>
        IQueryable<TEntity> QueryChildren(TEntity entity);

        /// <summary>
        /// Gets parent of <paramref name="entity"/>
        /// </summary>
        /// <param name="entity">Entity to get parent of</param>
        /// <returns>Parent <see cref="TEntity"/> or null </returns>
        Task<TEntity?> GetParentAsync(TEntity entity);
        
        /// <summary>
        /// Returns enumerated collection of nodes from root to <paramref name="entity"/>
        /// preserving order
        /// </summary>
        /// <remarks>
        /// This method internally enumerates the query to ensure correct ordering of path. This can have
        /// performance impact if the tree is large
        /// </remarks>
        /// <param name="entity">Entity to get path from root</param>
        /// <returns></returns>
        Task<IEnumerable<TEntity>> GetPathFromRootAsync(TEntity entity);
        
        /// <summary>
        /// Sets <paramref name="parent"/> node of <paramref name="entity"/>
        /// </summary>
        /// <remarks>
        /// Method saves underlying context.
        /// Method has to update paths of all descendants. This can be performance heavy. 
        /// </remarks>
        /// <param name="entity">Entity to set parent of</param>
        /// <param name="parent">Parent entity or null</param>
        Task SetParentAsync(TEntity entity, IMaterializedPathEntity? parent);

        /// <summary>
        /// Detaches <paramref name="entity"/> from subtree, removing it from path of descendants. This makes the node
        /// a root node with no ancestors or descendants.
        /// </summary>
        /// <remarks>
        /// Method saves underlying context.
        /// Method has to update paths of all descendants. This can be performance heavy. 
        /// Detaching is necessary before deleting the node from the tree as the descendant paths need to be adjusted.
        /// Deleting node before detaching it will leave descendants in inconsistent state.
        /// </remarks>
        /// <param name="entity">Entity to detach from tree</param>
        Task DetachNodeAsync(TEntity entity);
    }
}