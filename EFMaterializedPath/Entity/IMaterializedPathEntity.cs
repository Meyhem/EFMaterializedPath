namespace EFMaterializedPath.Entity
{
    public interface IMaterializedPathEntity
    {
        public int Id { get; set; }
        public string Path { get; set; }
        public int Level { get; set; }
        public int? ParentId { get; set; }
    }
}