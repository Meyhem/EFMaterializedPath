using EFMaterializedPath.Entity;
using Microsoft.EntityFrameworkCore;

namespace EFMaterializedPath.Test;

/// <summary>
/// <see cref="TreeRepository{TDbContext,TEntity,TId}.ValidateAsync"/> with a strongly typed id that is not comparable.
/// </summary>
public class TestValidateAsyncWithCustomId
{
    [Fact]
    public async Task ReportsProblemsForNonComparableId()
    {
        await using var context = new StrongIdContext();
        var repository = new TreeRepository<StrongIdContext, StrongNode, StrongId>(context, new StrongIdSerializer());

        context.Nodes.AddRange(
            new StrongNode { Id = new StrongId(1), Path = "" },
            new StrongNode { Id = new StrongId(2), Path = "|9|", Level = 1, ParentId = new StrongId(1) },
            new StrongNode { Id = new StrongId(3), Path = "", ParentId = new StrongId(99) }
        );
        await context.SaveChangesAsync();

        var problems = await repository.ValidateAsync();

        problems.Select(p => (p.Id.Value, p.Kind)).Should().BeEquivalentTo(new[]
        {
            (2, TreeProblemKind.WrongPath),
            (3, TreeProblemKind.MissingParent),
        });
    }

    public readonly record struct StrongId(int Value);

    public class StrongNode : IMaterializedPathEntity<StrongId>
    {
        public StrongId Id { get; set; }
        public string Path { get; set; }
        public int Level { get; set; }
        public StrongId? ParentId { get; set; }
    }

    private class StrongIdSerializer : IIdentifierSerializer<StrongId>
    {
        public string SerializeIdentifier(StrongId id) => id.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
        public StrongId DeserializeIdentifier(string id) => new(int.Parse(id, System.Globalization.CultureInfo.InvariantCulture));
    }

    public class StrongIdContext : DbContext
    {
        public DbSet<StrongNode> Nodes { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder options) =>
            options.UseInMemoryDatabase(Guid.NewGuid().ToString());

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfiguration(new MaterializedEntityMapping<StrongNode, StrongId>());
            modelBuilder.Entity<StrongNode>().Property(n => n.Id).HasConversion(v => v.Value, v => new StrongId(v));
            modelBuilder.Entity<StrongNode>().Property(n => n.ParentId)
                .HasConversion(v => v!.Value.Value, v => new StrongId(v));
        }
    }
}
