namespace EFMaterializedPath;

/// <summary>
/// Kind of inconsistency found by <see cref="ITreeRepository{TEntity,TId}.ValidateAsync"/>.
/// </summary>
public enum TreeProblemKind
{
    /// <summary>The node's <c>ParentId</c> points to a row that does not exist.</summary>
    MissingParent,

    /// <summary>Following <c>ParentId</c> from this node leads back to it, so it has no root.</summary>
    Cycle,

    /// <summary>The stored <c>Path</c> does not match the ancestors given by <c>ParentId</c>.</summary>
    WrongPath,

    /// <summary>The stored <c>Level</c> does not match the number of ancestors given by <c>ParentId</c>.</summary>
    WrongLevel,
}

/// <summary>
/// One inconsistency found by <see cref="ITreeRepository{TEntity,TId}.ValidateAsync"/>.
/// </summary>
/// <param name="Id">Id of the node at fault</param>
/// <param name="Kind">What is wrong with it</param>
/// <param name="Message">Human-readable description naming the stored and the expected value</param>
/// <typeparam name="TId">Type of primary identifier</typeparam>
public sealed record TreeProblem<TId>(TId Id, TreeProblemKind Kind, string Message)
    where TId : struct;
