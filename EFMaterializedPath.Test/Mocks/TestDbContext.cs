using Microsoft.EntityFrameworkCore;

namespace EFMaterializedPath.Test.Mocks
{
    public class TestDbContext : DbContext
    {
        public TestDbContext(DbContextOptions options) : base(options)
        {
        }

        public DbSet<Category> Categories { get; set; }
    }
}