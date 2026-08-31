using Duende.IdentityServer.EntityFramework.DbContexts;
using Duende.IdentityServer.EntityFramework.Mappers;
using Microsoft.EntityFrameworkCore;

namespace GotIssues.IdentityHost.Configuration;

/// <summary>
/// Applies migrations and seeds the development identities. Runs only under the
/// explicit <c>--migrate</c> step, never at ordinary startup (ADR-0003) — the identity
/// host must not silently mutate its schema either.
///
/// Seeding is idempotent by design, not by luck: each item is inserted only when it is
/// absent, so a restart against an existing database neither duplicates nor overwrites
/// what is already there (T-0010 AC9).
/// </summary>
public static class DatabaseSeeder
{
    public static async Task MigrateAndSeedAsync(IServiceProvider services, SeedConfiguration seed)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(seed);

        using var scope = services.CreateScope();

        await scope.ServiceProvider.GetRequiredService<PersistedGrantDbContext>()
            .Database.MigrateAsync().ConfigureAwait(false);

        var context = scope.ServiceProvider.GetRequiredService<ConfigurationDbContext>();
        await context.Database.MigrateAsync().ConfigureAwait(false);

        foreach (var scopeDefinition in IdentityResources.Scopes)
        {
            if (!await context.ApiScopes.AnyAsync(s => s.Name == scopeDefinition.Name).ConfigureAwait(false))
            {
                context.ApiScopes.Add(scopeDefinition.ToEntity());
            }
        }

        foreach (var resource in IdentityResources.Resources)
        {
            if (!await context.ApiResources.AnyAsync(r => r.Name == resource.Name).ConfigureAwait(false))
            {
                context.ApiResources.Add(resource.ToEntity());
            }
        }

        foreach (var definition in seed.Clients)
        {
            if (!await context.Clients.AnyAsync(c => c.ClientId == definition.ClientId).ConfigureAwait(false))
            {
                context.Clients.Add(ClientFactory.Build(definition).ToEntity());
            }
        }

        await context.SaveChangesAsync().ConfigureAwait(false);
    }
}
