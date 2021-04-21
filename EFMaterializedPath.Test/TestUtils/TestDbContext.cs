using Microsoft.EntityFrameworkCore;

namespace EFMaterializedPath.Test.TestUtils
{
    public class TestDbContext : DbContext
    {
        public TestDbContext(DbContextOptions options) : base(options)
        {
        }

        public DbSet<Category> Categories { get; set; }
    }
}