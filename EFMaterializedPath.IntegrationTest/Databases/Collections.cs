namespace EFMaterializedPath.IntegrationTest.Databases;

[CollectionDefinition(Name)]
public class PostgresCollection : ICollectionFixture<PostgresFixture>
{
    public const string Name = "Postgres";
}

[CollectionDefinition(Name)]
public class SqlServerCollection : ICollectionFixture<SqlServerFixture>
{
    public const string Name = "SqlServer";
}

/// <summary>Tests tagged with this trait need a running Docker daemon.</summary>
public static class Traits
{
    public const string Category = "Category";
    public const string Docker = "Docker";
}
