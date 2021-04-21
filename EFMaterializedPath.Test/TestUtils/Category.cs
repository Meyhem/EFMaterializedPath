using EFMaterializedPath.Entity;

namespace EFMaterializedPath.Test.TestUtils
{
    public class Category : IMaterializedPathEntity
    {
        public int Id { get; set; }
        public string Path { get; set; }
        public int Level { get; set; }
        public int? ParentId { get; set; }
    }
}