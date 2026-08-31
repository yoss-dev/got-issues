using Duende.IdentityServer.EntityFramework.DbContexts;
using Duende.IdentityServer.EntityFramework.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace GotIssues.IdentityHost.Configuration;

/// <summary>
/// Wiring for Duende's EF stores, which live in their own <c>identity</c> schema of
/// the shared PostgreSQL instance. The API owns <c>public</c>; neither reads the
/// other's tables (ARCHITECTURE.md).
/// </summary>
public static class IdentityStore
{
    public const string Schema = "identity";

    /// <summary>
    /// Puts Duende's entities in the identity schema. Setting only the migrations
    /// history table's schema is not enough — the tables themselves default to
    /// <c>public</c>, which is the API's, and the two would share a schema in
    /// violation of the ownership boundary in ARCHITECTURE.md.
    /// </summary>
    public static ConfigurationStoreOptions ConfigurationStore() =>
        new() { DefaultSchema = Schema };

    public static OperationalStoreOptions OperationalStore() =>
        new() { DefaultSchema = Schema };

    public static Action<DbContextOptionsBuilder> Npgsql(string connectionString, string? migrationsAssembly) =>
        builder => builder.UseNpgsql(
            connectionString,
            sql => sql.MigrationsAssembly(migrationsAssembly)
                      .MigrationsHistoryTable("__EFMigrationsHistory", Schema));
}

/// <summary>
/// Design-time factories for <c>dotnet ef</c>.
///
/// The non-obvious part, and the one that cost a first attempt: Duende's contexts
/// resolve their store options (which carry the schema and table names) from the
/// <em>application</em> service provider attached to <see cref="DbContextOptions"/>,
/// not from the context's constructor. A factory that only supplies a provider and a
/// connection string fails with "Unable to resolve service for type
/// ConfigurationStoreOptions". Attaching a minimal service provider that contains the
/// options is what makes scaffolding work.
///
/// EF never opens this connection when scaffolding, so the fallback carries no
/// credentials — nothing password-shaped is committed (T-0010 AC7).
/// </summary>
internal static class DesignTime
{
    public static string ConnectionString =>
        Environment.GetEnvironmentVariable("ConnectionStrings__Identity")
        ?? "Host=localhost;Database=gotissues_design_time";

    public static DbContextOptions<TContext> Options<TContext>(object storeOptions)
        where TContext : DbContext
    {
        var services = new ServiceCollection();
        services.AddSingleton(storeOptions.GetType(), storeOptions);

        var builder = new DbContextOptionsBuilder<TContext>()
            .UseApplicationServiceProvider(services.BuildServiceProvider());

        IdentityStore.Npgsql(ConnectionString, typeof(DesignTime).Assembly.GetName().Name)(builder);

        return builder.Options;
    }
}

public sealed class ConfigurationDbContextFactory : IDesignTimeDbContextFactory<ConfigurationDbContext>
{
    public ConfigurationDbContext CreateDbContext(string[] args) =>
        new(DesignTime.Options<ConfigurationDbContext>(IdentityStore.ConfigurationStore()));
}

public sealed class PersistedGrantDbContextFactory : IDesignTimeDbContextFactory<PersistedGrantDbContext>
{
    public PersistedGrantDbContext CreateDbContext(string[] args) =>
        new(DesignTime.Options<PersistedGrantDbContext>(IdentityStore.OperationalStore()));
}
