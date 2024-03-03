using System;
using EFMaterializedPath.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EFMaterializedPath.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds new ITreeRepository for entity of type <typeparamref name="TEntity"/>
    /// and associated <typeparamref name="TDbContext"/> to be used. 
    /// </summary>
    /// <typeparam name="TDbContext">EF DbContext</typeparam>
    /// <typeparam name="TEntity">Entity type implementing <see cref="IMaterializedPathEntity{TId}"/></typeparam>
    /// <typeparam name="TId">Type of primary identifier</typeparam>
    /// <returns></returns>
    public static IServiceCollection AddTreeRepository<TDbContext, TEntity, TId>(
        this IServiceCollection self
    )
        where TDbContext : DbContext
        where TEntity : class, IMaterializedPathEntity<TId>
        where TId : struct
    {
        return self.AddTransient<ITreeRepository<TEntity, TId>, TreeRepository<TDbContext, TEntity, TId>>();
    }

    /// <summary>
    /// Also adds default <see cref="IIdentifierSerializer{TId}"/>'s for int, string & Guid to be used by tree
    /// repositories
    /// </summary>
    public static IServiceCollection AddDefaultIdentifierSerializers(this IServiceCollection self)
    {
        return self.AddTransient<IIdentifierSerializer<int>, IntIdentifierSerializer>()
            .AddTransient<IIdentifierSerializer<string>, StringIdentifierSerializer>()
            .AddTransient<IIdentifierSerializer<Guid>, GuidIdentifierSerializer>();
    }
}
