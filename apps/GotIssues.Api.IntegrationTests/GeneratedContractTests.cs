using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using GotIssues.Api.IntegrationTests.Infrastructure;

namespace GotIssues.Api.IntegrationTests;

/// <summary>
/// The projects resource, exercised through the real HTTP pipeline against the
/// generated contract (T-0002 AC5, and T-0004 AC6).
///
/// These tests were written against the disposable placeholder resource and moved to
/// projects when T-0004 deleted it. Each one still encodes a real defect from T-0002's
/// history — the comments say which — because a regression test that outlives the
/// resource it was written for is worth more than the resource was.
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

    /// <summary>An admin: the only role that may create a project (T-0004 AC2).</summary>
    private HttpClient AdminClient() => ClientAs("contract-tests-admin", "admin");

    /// <summary>A member: may list, may not create.</summary>
    private HttpClient MemberClient() => ClientAs("contract-tests-member", "member");

    private HttpClient ClientAs(string subject, string role)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.HeaderName, subject);
        client.DefaultRequestHeaders.Add(TestAuthHandler.RoleHeaderName, role);
        return client;
    }

    private static StringContent Json(string body) =>
        new(body, Encoding.UTF8, "application/json");

    public Task DisposeAsync()
    {
        _factory.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task A_created_project_comes_back_in_the_listing()
    {
        using var client = AdminClient();

        var created = await client.PostAsJsonAsync(
            new Uri("/projects", UriKind.Relative), new { key = "GOTI", name = "Got Issues" });

        Assert.Equal(HttpStatusCode.Created, created.StatusCode);

        var listed = await client.GetAsync(new Uri("/projects", UriKind.Relative));
        var body = await listed.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, listed.StatusCode);
        using var document = JsonDocument.Parse(body);
        var root = document.RootElement;
        Assert.Equal(1, root.GetProperty("totalCount").GetInt32());
        Assert.Equal("GOTI", root.GetProperty("items")[0].GetProperty("key").GetString());
        Assert.Equal("Got Issues", root.GetProperty("items")[0].GetProperty("name").GetString());
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
        using var client = AdminClient();

        var response = await client.GetAsync(
            new Uri("/projects?pageSize=10000", UriKind.Relative));

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
        using var client = AdminClient();

        var response = await client.GetAsync(new Uri($"/projects?{query}", UriKind.Relative));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Every_declared_required_property_is_present_on_a_listed_project()
    {
        // Replaces the placeholder's nullable-label test, which encoded T-0002's
        // defect 4: the document declared a bare string while the API returned null,
        // which OpenAPI 3.1 forbids. Project has no nullable property, so that exact
        // reproduction no longer exists — but the *class* does: the document promising
        // something the API does not do. This asserts the other direction of the same
        // claim, that every property the schema marks required actually arrives.
        //
        // Recorded for T-0017, whose AC6 names defect 4 by its placeholder
        // reproduction: that criterion needs re-expressing against this resource.
        using var client = AdminClient();
        await client.PostAsJsonAsync(
            new Uri("/projects", UriKind.Relative), new { key = "REQ", name = "Required fields" });

        var response = await client.GetAsync(new Uri("/projects", UriKind.Relative));
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var project = document.RootElement.GetProperty("items")[0];

        foreach (var required in new[] { "id", "key", "name", "createdAt" })
        {
            Assert.True(
                project.TryGetProperty(required, out var value) && value.ValueKind != JsonValueKind.Null,
                $"The schema marks '{required}' required, and the response omitted it or sent null.");
        }
    }

    [Fact]
    public async Task Page_size_defaults_when_omitted()
    {
        using var client = AdminClient();

        var response = await client.GetAsync(new Uri("/projects", UriKind.Relative));
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        Assert.Equal(20, document.RootElement.GetProperty("pageSize").GetInt32());
    }

    [Fact]
    public async Task Paging_returns_every_record_exactly_once()
    {
        using var client = AdminClient();
        for (var i = 0; i < 5; i++)
        {
            await client.PostAsJsonAsync(
                new Uri("/projects", UriKind.Relative), new { key = $"PAGE{i}", name = $"item-{i}" });
        }

        var seen = new List<string>();
        for (var page = 1; page <= 3; page++)
        {
            var response = await client.GetAsync(
                new Uri($"/projects?page={page}&pageSize=2", UriKind.Relative));
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

        var response = await anonymous.GetAsync(new Uri("/projects", UriKind.Relative));

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
        // The spec declares name as minLength 1; the generated model carries the
        // annotation, so this is the contract rejecting the request, not the
        // controller. RFC 9457 shape, not a bare 400.
        using var client = AdminClient();

        using var content = Json("""{"key":"OK","name":""}""");
        var response = await client.PostAsync(new Uri("/projects", UriKind.Relative), content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains(
            "application/problem+json",
            response.Content.Headers.ContentType?.MediaType ?? string.Empty,
            StringComparison.Ordinal);
    }
}
