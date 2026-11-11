using EFMaterializedPath.Entity;
using Microsoft.EntityFrameworkCore;

namespace EFMaterializedPath.Test.TestUtils;

public class TestDbContext : DbContext
{
    public TestDbContext(DbContextOptions options) : base(options)
    {
    }

    public DbSet<Category> Categories { get; set; }
    public DbSet<GuidCategory> GuidCategories { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // InMemory ignores database defaults, so the library mapping (required Path, default "") only applies on
        // relational providers, which is where consumers use it. The InMemory unit tests keep the convention model.
        if (!Database.IsRelational())
        {
            return;
        }

        modelBuilder.ApplyConfiguration(new MaterializedEntityMapping<Category, int>());
        modelBuilder.ApplyConfiguration(new MaterializedEntityMapping<GuidCategory, Guid>());

        // Tests assign ids explicitly; without this SQL Server rejects inserts into identity columns.
        modelBuilder.Entity<Category>().Property(c => c.Id).ValueGeneratedNever();
        modelBuilder.Entity<GuidCategory>().Property(c => c.Id).ValueGeneratedNever();
    }
}
