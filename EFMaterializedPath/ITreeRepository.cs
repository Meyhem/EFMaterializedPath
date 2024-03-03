using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EFMaterializedPath.Entity;

namespace EFMaterializedPath;

/// <summary>
/// Tree repository for manipulation with <see cref="IMaterializedPathEntity{TId}"/>
/// </summary>
/// <typeparam name="TEntity">Entity implementing <see cref="IMaterializedPathEntity{TId}"/></typeparam>
/// <typeparam name="TId">Type of primary identifier</typeparam>
public interface ITreeRepository<TEntity, TId>
    where TEntity : class, IMaterializedPathEntity<TId>
    where TId : struct
{
    /// <summary>
    /// Queries all root nodes
    /// </summary>
    /// <returns><see cref="IQueryable&lt;TEntity&gt;"/> of all roots</returns>
    IQueryable<TEntity?> QueryRoots();

    /// <summary>
    /// Arbitrary query over DbSet.
    /// </summary>
    /// <returns></returns>
    IQueryable<TEntity?> Query();

    /// <summary>
    /// Get single node by its primary key
    /// </summary>
    /// <param name="id">Id of node to retrieve</param>
    /// <returns><see cref="TEntity"/> or null</returns>
    Task<TEntity?> GetByIdAsync(TId id);

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
    /// Queries all nodes that are children of <see cref="entity"/>'s parent except self
    /// </summary>
    /// <param name="entity">Entity to get siblings of</param>
    /// <returns>Unordered <see cref="IQueryable&lt;TEntity&gt;"/> of sibling nodes</returns>
    IQueryable<TEntity> QuerySiblings(TEntity entity);

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
    Task SetParentAsync(TEntity entity, IMaterializedPathEntity<TId>? parent);

    /// <summary>
    /// Detaches <paramref name="entity"/> from subtree, removing it from path of descendants. This makes the node
    /// a root node with no ancestors or descendants. Descendants are parented to parent of detached node.
    /// </summary>
    /// <remarks>
    /// Method does not delete detached node.
    /// Method saves underlying context.
    /// Method has to update paths of all descendants. This can be performance heavy. 
    /// </remarks>
    /// <param name="entity">Entity to detach from tree</param>
    Task DetachNodeAsync(TEntity entity);

    /// <summary>
    /// Deletes <paramref name="entity"/> from tree, removing it from path of descendants.
    /// Descendants are parented to parent of detached node.
    /// </summary>
    /// <remarks>
    /// Method deletes whole entity
    /// Method saves underlying context.
    /// Method has to update paths of all descendants. This can be performance heavy. 
    /// </remarks>
    /// <param name="entity">Entity to delete from tree</param>
    Task RemoveNodeAsync(TEntity entity);
}
