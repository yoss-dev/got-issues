using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using GotIssues.Api.IntegrationTests.Infrastructure;

namespace GotIssues.Api.IntegrationTests;

/// <summary>
/// The placeholder resource, exercised through the real HTTP pipeline against the
/// generated contract (T-0002 AC5).
///
/// What these tests are really checking is that the contract-first pipeline
/// produces a working endpoint: the routes, parameter binding and status codes all
/// come from <c>spec/openapi.yaml</c> via generated code, not from anything
/// hand-written in the API.
/// </summary>
[Collection(PostgresFixtureDefinition.Name)]
public sealed class GeneratedContractTests(PostgresContainerFixture postgres) : IAsyncLifetime
{
    private ApiFactory _factory = null!;

    public async Task InitializeAsync()
    {
        var connectionString = await postgres.CreateDatabaseAsync().ConfigureAwait(false);

        // The specification declares `security: bearerAuth` globally, so the
        // generated controller carries [Authorize] — nobody wrote that attribute.
        // These tests therefore need an authentication scheme registered, which is
        // what the test host provides.
        _factory = new ApiFactory(connectionString, withTestAuthentication: true);
        await _factory.ApplyMigrationsAsync().ConfigureAwait(false);
    }

    private HttpClient AuthenticatedClient()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.HeaderName, "contract-tests");
        return client;
    }

    public Task DisposeAsync()
    {
        _factory.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task A_created_placeholder_comes_back_in_the_listing()
    {
        using var client = AuthenticatedClient();

        var created = await client.PostAsJsonAsync(
            new Uri("/placeholders", UriKind.Relative), new { label = "first" });

        Assert.Equal(HttpStatusCode.Created, created.StatusCode);

        var listed = await client.GetAsync(new Uri("/placeholders", UriKind.Relative));
        var body = await listed.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, listed.StatusCode);
        using var document = JsonDocument.Parse(body);
        var root = document.RootElement;
        Assert.Equal(1, root.GetProperty("totalCount").GetInt32());
        Assert.Equal("first", root.GetProperty("items")[0].GetProperty("label").GetString());
    }

    [Fact]
    public async Task Page_size_above_the_declared_maximum_is_rejected()
    {
        // The spec declares `maximum: 100`, which the generator carries into the
        // contract as [Range(1, 100)]. The request is rejected before it reaches
        // any hand-written code.
        //
        // This test first failed because the specification contradicted itself: its
        // prose said values would be capped while its schema declared a maximum.
        // The generator obeyed the schema, which is the normative half. Prose in a
        // contract is documentation; the schema is the contract.
        using var client = AuthenticatedClient();

        var response = await client.GetAsync(
            new Uri("/placeholders?pageSize=10000", UriKind.Relative));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData("page=0")]
    [InlineData("page=-5")]
    public async Task An_out_of_range_page_is_rejected_not_silently_adjusted(string query)
    {
        // This previously returned 200 with page=1: the specification declared
        // `minimum: 1` but the generator emits a Range attribute only when both
        // bounds are present, so the rule existed in prose and in a Math.Max in the
        // controller — a validation rule living only in code, which is the thing the
        // contract exists to prevent. Declaring an upper bound too made it
        // enforceable by the contract, and by generated clients.
        using var client = AuthenticatedClient();

        var response = await client.GetAsync(new Uri($"/placeholders?{query}", UriKind.Relative));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task An_omitted_label_comes_back_as_null()
    {
        // The spec declares label as [string, 'null']. It previously declared a bare
        // string while the API returned null, which OpenAPI 3.1 forbids — the
        // document promised something the API did not do.
        using var client = AuthenticatedClient();

        await client.PostAsJsonAsync(new Uri("/placeholders", UriKind.Relative), new { });
        var response = await client.GetAsync(new Uri("/placeholders", UriKind.Relative));
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        var label = document.RootElement.GetProperty("items")[0].GetProperty("label");
        Assert.Equal(JsonValueKind.Null, label.ValueKind);
    }

    [Fact]
    public async Task Page_size_defaults_when_omitted()
    {
        using var client = AuthenticatedClient();

        var response = await client.GetAsync(new Uri("/placeholders", UriKind.Relative));
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        Assert.Equal(20, document.RootElement.GetProperty("pageSize").GetInt32());
    }

    [Fact]
    public async Task Paging_returns_every_record_exactly_once()
    {
        using var client = AuthenticatedClient();
        for (var i = 0; i < 5; i++)
        {
            await client.PostAsJsonAsync(
                new Uri("/placeholders", UriKind.Relative), new { label = $"item-{i}" });
        }

        var seen = new List<string>();
        for (var page = 1; page <= 3; page++)
        {
            var response = await client.GetAsync(
                new Uri($"/placeholders?page={page}&pageSize=2", UriKind.Relative));
            using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            seen.AddRange(document.RootElement.GetProperty("items")
                .EnumerateArray()
                .Select(e => e.GetProperty("id").GetString()!));
        }

        Assert.Equal(5, seen.Count);
        Assert.Equal(5, seen.Distinct().Count());   // no duplicates across page boundaries
    }

    [Fact]
    public async Task The_endpoints_are_protected_because_the_specification_says_so()
    {
        // Nobody wrote [Authorize] on the controller. It is there because
        // spec/openapi.yaml declares `security: bearerAuth` globally, and the
        // generator carried that into the contract. This asserts the declaration
        // is actually enforced — the clearest single proof that the pipeline
        // transmits intent and not just shapes.
        using var anonymous = _factory.CreateClient();

        var response = await anonymous.GetAsync(new Uri("/placeholders", UriKind.Relative));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        // The 401's *body* is asserted in ResourceServerTests, against the API's own
        // pipeline. It cannot be asserted here: this test host injects its
        // authentication through an IStartupFilter, which produces the 401 before
        // the application's own middleware runs — so a body assertion here would
        // test the test host rather than the API.
    }

    [Fact]
    public async Task A_validation_failure_returns_a_problem_document()
    {
        // The spec declares label as minLength 1; the generated model carries the
        // annotation, so this is the contract rejecting the request, not the
        // controller. RFC 9457 shape, not a bare 400.
        using var client = AuthenticatedClient();

        using var content = new StringContent(
            """{"label":""}""", Encoding.UTF8, "application/json");
        var response = await client.PostAsync(new Uri("/placeholders", UriKind.Relative), content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains(
            "application/problem+json",
            response.Content.Headers.ContentType?.MediaType ?? string.Empty,
            StringComparison.Ordinal);
    }
}
