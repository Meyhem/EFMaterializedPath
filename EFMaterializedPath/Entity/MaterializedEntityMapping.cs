using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EFMaterializedPath.Entity
{
    /// <summary>
    /// Class to provide basic entity configuration for tree entities
    /// </summary>
    /// <typeparam name="TEntity">Entity implementing <see cref="IMaterializedPathEntity{TId}"/></typeparam>
    /// <typeparam name="TId">Type of primary identifier</typeparam>
    public class MaterializedEntityMapping<TEntity, TId> : IEntityTypeConfiguration<TEntity>
        where TEntity : class, IMaterializedPathEntity<TId>
        where TId: struct
    {
        public void Configure(EntityTypeBuilder<TEntity> builder)
        {
            builder.HasKey(ent => ent.Id);
            builder.Property(ent => ent.Path).IsRequired().HasDefaultValue("");
            builder.Property(ent => ent.Level).IsRequired();
            builder.Property(ent => ent.ParentId).IsRequired(false);

            builder.HasIndex(ent => ent.Path).IsUnique(false);
            builder.HasIndex(ent => ent.ParentId).IsUnique(false);
        }
    }
}