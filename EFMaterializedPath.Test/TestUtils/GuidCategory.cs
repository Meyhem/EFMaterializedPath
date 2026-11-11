using EFMaterializedPath.Entity;

namespace EFMaterializedPath.Test.TestUtils;

public class GuidCategory : IMaterializedPathEntity<Guid>
{
    public Guid Id { get; set; }
    public string Path { get; set; }
    public int Level { get; set; }
    public Guid? ParentId { get; set; }
}
