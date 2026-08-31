using System.Net;
using GotIssues.Api.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;

namespace GotIssues.Api.IntegrationTests;

/// <summary>
/// The test-authentication scaffolding, and proof it cannot leak into a real run.
/// Nothing in the product is protected yet — the identity host is T-0010 and the
/// role policies are T-0009 — so the guarded endpoint exercised here belongs to the
/// test host. What is being verified is that refusal *works*, so the tests those
/// tickets write have a foundation that has actually been shown to refuse.
/// </summary>
[Collection(PostgresFixtureDefinition.Name)]
public sealed class AuthenticationScaffoldTests(PostgresContainerFixture postgres) : IAsyncLifetime
{
    private ApiFactory _guarded = null!;
    private ApiFactory _plain = null!;

    public async Task InitializeAsync()
    {
        var connectionString = await postgres.CreateDatabaseAsync().ConfigureAwait(false);
        _guarded = new ApiFactory(connectionString, withTestAuthentication: true);
        await _guarded.ApplyMigrationsAsync().ConfigureAwait(false);
        _plain = new ApiFactory(connectionString);
    }

    public Task DisposeAsync()
    {
        _guarded.Dispose();
        _plain.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task An_unauthenticated_caller_is_refused_by_a_guarded_endpoint()
    {
        using var client = _guarded.CreateClient();

        var response = await client.GetAsync(
            new Uri(GuardedEndpointStartupFilter.Route, UriKind.Relative));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task An_authenticated_caller_reaches_a_guarded_endpoint()
    {
        using var client = _guarded.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.HeaderName, "test-subject");

        var response = await client.GetAsync(
            new Uri(GuardedEndpointStartupFilter.Route, UriKind.Relative));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task The_test_authentication_handler_is_absent_from_the_api_normal_composition()
    {
        // AC10: the handler exists only in the test host. In a normal run the API
        // registers no authentication scheme at all, so there is no configuration
        // switch that could turn this on outside tests.
        using var scope = _plain.Services.CreateScope();
        var schemes = scope.ServiceProvider.GetService<IAuthenticationSchemeProvider>();

        var registered = schemes is null
            ? []
            : await schemes.GetAllSchemesAsync();

        Assert.DoesNotContain(registered, s => s.Name == TestAuthHandler.SchemeName);
        Assert.DoesNotContain(
            registered,
            s => s.HandlerType == typeof(TestAuthHandler));
    }

    [Fact]
    public async Task The_guarded_test_endpoint_does_not_exist_in_the_api_normal_composition()
    {
        using var client = _plain.CreateClient();

        var response = await client.GetAsync(
            new Uri(GuardedEndpointStartupFilter.Route, UriKind.Relative));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
