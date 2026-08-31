using GotIssues.Api.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GotIssues.Api.IntegrationTests.Infrastructure;

/// <summary>
/// Runs the real API through its real HTTP pipeline against a real PostgreSQL
/// database — never the EF in-memory provider, which enforces no constraints and
/// translates no real SQL (TESTING.md).
/// </summary>
public sealed class ApiFactory(string connectionString, bool withTestAuthentication = false)
    : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:GotIssues", connectionString);
        builder.UseEnvironment("Testing");

        if (!withTestAuthentication)
        {
            return;
        }

        builder.ConfigureTestServices(services =>
        {
            services.AddAuthentication(TestAuthHandler.SchemeName)
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                    TestAuthHandler.SchemeName, _ => { });
            services.AddAuthorization();
            services.AddSingleton<IStartupFilter, GuardedEndpointStartupFilter>();
        });
    }

    /// <summary>
    /// Applies the API project's own migrations — the same ones the compose stack's
    /// migration step runs. Tests therefore exercise the real schema, and a broken
    /// migration fails the suite (T-0003 AC3, and AC8's coverage of T-0001).
    /// </summary>
    public async Task ApplyMigrationsAsync()
    {
        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<GotIssuesDbContext>();
        await context.Database.MigrateAsync().ConfigureAwait(false);
    }
}
