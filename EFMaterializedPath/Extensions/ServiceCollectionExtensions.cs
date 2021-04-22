using EFMaterializedPath.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EFMaterializedPath.Extensions
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds new ITreeRepository for entity of type <typeparamref name="TEntity"/>
        /// and associated <typeparamref name="TDbContext"/> to be used 
        /// </summary>
        /// <typeparam name="TDbContext">EF DbContext</typeparam>
        /// <typeparam name="TEntity">Entity type implementing <see cref="IMaterializedPathEntity"/></typeparam>
        /// <returns></returns>
        public static IServiceCollection AddTreeRepository<TDbContext, TEntity>(this IServiceCollection self)
            where TDbContext : DbContext
            where TEntity : class, IMaterializedPathEntity
        {
            return self.AddTransient<ITreeRepository<TEntity>, TreeRepository<TDbContext, TEntity>>();
        }
    }
}