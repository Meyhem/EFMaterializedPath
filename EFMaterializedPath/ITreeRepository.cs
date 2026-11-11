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
    IQueryable<TEntity> QueryRoots();

    /// <summary>
    /// Arbitrary query over DbSet.
    /// </summary>
    /// <returns>Unordered <see cref="IQueryable&lt;TEntity&gt;"/> of all nodes</returns>
    IQueryable<TEntity> Query();

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
    /// <returns>
    /// Unordered <see cref="IQueryable&lt;TEntity&gt;"/> of ancestor nodes, empty for a root node.
    /// It is always a database query, so it can be composed and enumerated asynchronously.
    /// </returns>
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
    /// Tells whether <paramref name="ancestor"/> is an ancestor of <paramref name="node"/>. A node is not its own
    /// ancestor.
    /// </summary>
    /// <remarks>
    /// Method does not query the database: it reads <paramref name="node"/>'s <c>Path</c> as loaded, so the result
    /// is outdated if the tree changed after <paramref name="node"/> was loaded.
    /// </remarks>
    /// <param name="ancestor">Possible ancestor</param>
    /// <param name="node">Node whose ancestors are checked</param>
    /// <returns><c>true</c> if <paramref name="ancestor"/> is on the path from the root to <paramref name="node"/></returns>
    /// <exception cref="System.ArgumentNullException">Either entity is null.</exception>
    /// <exception cref="System.InvalidOperationException">Either entity has no id yet.</exception>
    bool IsAncestorOf(TEntity ancestor, TEntity node);

    /// <summary>
    /// Tells whether <paramref name="node"/> is a descendant of <paramref name="ancestor"/>; the same as
    /// <see cref="IsAncestorOf"/> with the arguments swapped. A node is not its own descendant.
    /// </summary>
    /// <remarks>
    /// Method does not query the database: it reads <paramref name="node"/>'s <c>Path</c> as loaded.
    /// </remarks>
    /// <param name="node">Node whose ancestors are checked</param>
    /// <param name="ancestor">Possible ancestor</param>
    /// <returns><c>true</c> if <paramref name="ancestor"/> is on the path from the root to <paramref name="node"/></returns>
    /// <exception cref="System.ArgumentNullException">Either entity is null.</exception>
    /// <exception cref="System.InvalidOperationException">Either entity has no id yet.</exception>
    bool IsDescendantOf(TEntity node, TEntity ancestor);

    /// <summary>
    /// Checks the whole stored tree for inconsistent nodes: a <c>ParentId</c> pointing to a missing row, a cycle of
    /// <c>ParentId</c>s, or a <c>Path</c> / <c>Level</c> that does not match the ancestors given by <c>ParentId</c>.
    /// </summary>
    /// <remarks>
    /// <c>ParentId</c> is taken as the truth: each node's <c>Path</c> and <c>Level</c> are compared with the ancestors
    /// its <c>ParentId</c> chain gives. A wrong <c>Path</c> or <c>Level</c> on one node is reported for that node only,
    /// but if a node's <c>ParentId</c> was changed outside the repository, that node and every node below it are
    /// reported. Nodes below a missing parent or a cycle cannot be checked and are not reported.
    /// Method reads <c>Id</c>, <c>ParentId</c>, <c>Path</c> and <c>Level</c> of every row into memory in one query,
    /// bypassing the change tracker, and never modifies data.
    /// </remarks>
    /// <returns>
    /// Problems ordered by node id (if <typeparamref name="TId"/> is comparable, otherwise in no particular order), empty
    /// if the tree is consistent
    /// </returns>
    Task<IReadOnlyList<TreeProblem<TId>>> ValidateAsync();

    /// <summary>
    /// Sets <paramref name="parent"/> node of <paramref name="entity"/>
    /// </summary>
    /// <remarks>
    /// Method saves underlying context.
    /// Method has to update paths of all descendants. This can be performance heavy.
    /// If <paramref name="entity"/> is not tracked by the context (e.g. loaded with <c>AsNoTracking</c>), it is
    /// attached so its new position is saved; if the context already tracks another instance of the same row,
    /// both instances are updated.
    /// </remarks>
    /// <param name="entity">Entity to set parent of</param>
    /// <param name="parent">Parent entity or null</param>
    /// <exception cref="System.InvalidOperationException">
    /// <paramref name="parent"/> is <paramref name="entity"/> itself or one of its descendants (the move would
    /// create a cycle), or either entity has no id yet.
    /// </exception>
    Task SetParentAsync(TEntity entity, IMaterializedPathEntity<TId>? parent);

    /// <summary>
    /// Detaches <paramref name="entity"/> from subtree, removing it from path of descendants. This makes the node
    /// a root node with no ancestors or descendants. Descendants are parented to parent of detached node.
    /// </summary>
    /// <remarks>
    /// Method does not delete detached node.
    /// Method saves underlying context once, so the whole detach is stored or, if the save fails, nothing is.
    /// If the save fails, the entities it changed (including <paramref name="entity"/>) and the change tracker are
    /// put back as they were before the call, so the operation can be retried.
    /// If <paramref name="entity"/> is not tracked by the context, it is attached so its new position is saved.
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
    /// Method saves underlying context once: re-parenting the children and deleting the node are stored together,
    /// or, if the save fails, nothing is.
    /// If the save fails, the entities it changed (including <paramref name="entity"/>) and the change tracker are
    /// put back as they were before the call, so the operation can be retried.
    /// Method has to update paths of all descendants. This can be performance heavy.
    /// </remarks>
    /// <param name="entity">Entity to delete from tree</param>
    Task RemoveNodeAsync(TEntity entity);
}
