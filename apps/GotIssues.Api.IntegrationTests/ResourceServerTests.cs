using System.Net;
using System.Text.Json;
using GotIssues.Api.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

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
    public async Task An_unauthenticated_refusal_carries_a_problem_document()
    {
        // The specification declares application/problem+json for 401, and the
        // Problem schema states that every failure in this API uses that shape. The
        // 401 was returning an empty body with no content type, making that sentence
        // false for the most common failure a client meets — and it survived two
        // review passes because the refusal tests asserted only the status code.
        //
        // Asserted here rather than in GeneratedContractTests because this factory
        // exercises the API's own pipeline; that one injects authentication through
        // an IStartupFilter which short-circuits before the app's middleware.
        using var factory = new AuthenticatedApiFactory(_connectionString);
        using var client = factory.CreateClient();

        var response = await client.GetAsync(new Uri("/placeholders", UriKind.Relative));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal(
            "application/problem+json",
            response.Content.Headers.ContentType?.MediaType);

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(401, document.RootElement.GetProperty("status").GetInt32());
    }

    [Fact]
    public void Inbound_claim_mapping_is_disabled_and_the_role_claim_is_named()
    {
        // Pins the actual fix. Without this, deleting `MapInboundClaims = false`
        // leaves all other tests green — the policies' fallback to ClaimTypes.Role
        // silently carries them, so the safety net hides the removal of the thing it
        // is a net for. This asserts the configuration itself, so the removal is loud.
        //
        // RoleClaimType is pinned for the same reason: with mapping off it would
        // otherwise still point at the WS-Federation URI, making User.IsInRole return
        // false for a genuine admin.
        using var factory = new AuthenticatedApiFactory(_connectionString);

        var options = factory.Services
            .GetRequiredService<IOptionsMonitor<JwtBearerOptions>>()
            .Get(JwtBearerDefaults.AuthenticationScheme);

        Assert.False(options.MapInboundClaims);
        Assert.Equal("role", options.TokenValidationParameters.RoleClaimType);
        Assert.Equal("name", options.TokenValidationParameters.NameClaimType);
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
