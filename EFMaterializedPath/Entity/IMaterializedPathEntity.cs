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
        public string Path { get; set; }
        
        /// <summary>
        /// Level of this entity in tree, 0 if root
        /// </summary>
        public int Level { get; set; }
        
        /// <summary>
        /// Reference to parent node, null if root
        /// </summary>
        public int? ParentId { get; set; }
    }
}