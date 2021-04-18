using EFMaterializedPath.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EFMaterializedPath.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddTreeRepository<TDbContext, TEntity>(this IServiceCollection self)
            where TDbContext : DbContext
            where TEntity : class, IMaterializedPathEntity
        {
            return self.AddTransient<ITreeRepository<TEntity>, TreeRepository<TDbContext, TEntity>>();
        }
    }
}