namespace EFMaterializedPath.Entity
{
    public interface IMaterializedPathEntity
    {
        /// <summary>
        /// Entity identifier to be used as PK
        /// </summary>
        public int Id { get; set; }
        
        /// <summary>
        /// Path representing materialized path of entity, empty if root
        /// </summary>
        /// <remarks>
        /// This property is fully managed by <see cref="ITreeRepository{TEntity}"/>.
        /// Manual modifications will result in inconsistent tree.
        /// </remarks>
        public string Path { get; set; }
        
        /// <summary>
        /// Level of this entity in tree, 0 if root
        /// </summary>
        /// <remarks>
        /// This property is fully managed by <see cref="ITreeRepository{TEntity}"/>.
        /// Manual modifications will result in inconsistent tree.
        /// </remarks>
        public int Level { get; set; }
        
        /// <summary>
        /// Reference to parent node, null if root
        /// </summary>
        /// <remarks>
        /// This property is fully managed by <see cref="ITreeRepository{TEntity}"/>.
        /// Manual modifications will result in inconsistent tree.
        /// </remarks>
        public int? ParentId { get; set; }
    }
}