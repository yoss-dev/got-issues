using System.Net;
using GotIssues.Api.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace GotIssues.Api.IntegrationTests;

/// <summary>
/// The API's resource-server wiring (T-0010). What is verified here is what a test
/// harness can reach without an identity host: that configuring an authority turns
/// the protected operational endpoint on and that it refuses anonymous callers, and
/// that without an authority the endpoint does not exist at all.
///
/// Token *validation* against a real issuer — a valid token accepted, and refusals
/// for expired, wrong-audience and unknown-key tokens — needs the identity host
/// running, so it is verified against the live Compose stack and recorded in the
/// ticket. Automating it is owned by T-0015, whose scope was widened during
/// T-0010's review to accept exactly this: behaviour whose verification requires
/// the real stack, whether it belongs to the stack or the API behind it.
/// </summary>
[Collection(PostgresFixtureDefinition.Name)]
public sealed class ResourceServerTests(PostgresContainerFixture postgres) : IAsyncLifetime
{
    private string _connectionString = string.Empty;

    public async Task InitializeAsync() =>
        _connectionString = await postgres.CreateDatabaseAsync().ConfigureAwait(false);

    public Task DisposeAsync() => Task.CompletedTask;

    private sealed class AuthenticatedApiFactory(string connectionString)
        : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseSetting("ConnectionStrings:GotIssues", connectionString);
            builder.UseSetting("Authentication:Authority", "http://identity.invalid");
            builder.UseSetting("Authentication:Audience", "gotissues-api");
            builder.UseSetting("Authentication:RequireHttpsMetadata", "false");
            builder.UseEnvironment("Testing");
        }
    }

    [Fact]
    public async Task The_protected_operational_endpoint_refuses_an_anonymous_caller()
    {
        using var factory = new AuthenticatedApiFactory(_connectionString);
        using var client = factory.CreateClient();

        var response = await client.GetAsync(new Uri("/health/authenticated", UriKind.Relative));

        // 401 without ever contacting the authority: there is no token to validate,
        // so the challenge happens before any metadata fetch.
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task The_protected_endpoint_does_not_exist_when_no_authority_is_configured()
    {
        // The API still runs standalone: authentication is wired only when an authority
        // is configured, so T-0001's stack keeps working without the identity host.
        using var factory = new ApiFactory(_connectionString);
        using var client = factory.CreateClient();

        var response = await client.GetAsync(new Uri("/health/authenticated", UriKind.Relative));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task The_unprotected_health_endpoint_stays_open_when_authentication_is_configured()
    {
        // Adding authentication must not accidentally guard the liveness probe the
        // compose stack depends on (T-0001 AC2).
        using var factory = new AuthenticatedApiFactory(_connectionString);
        using var client = factory.CreateClient();

        var response = await client.GetAsync(new Uri("/health", UriKind.Relative));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
