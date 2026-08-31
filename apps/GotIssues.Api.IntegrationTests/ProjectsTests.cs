using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using System.Text;
using System.Text.Json;
using GotIssues.Api.Data;
using GotIssues.Api.IntegrationTests.Infrastructure;
using GotIssues.Contracts.Controllers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GotIssues.Api.IntegrationTests;

/// <summary>
/// T-0004's acceptance criteria: creating and listing projects, the key's rules, and
/// who may do which.
/// </summary>
[Collection(PostgresFixtureDefinition.Name)]
public sealed class ProjectsTests(PostgresContainerFixture postgres) : IAsyncLifetime
{
    private static readonly string[] ExpectedOperations = ["CreateProject", "ListProjects"];

    private ApiFactory _factory = null!;

    public async Task InitializeAsync()
    {
        var connectionString = await postgres.CreateDatabaseAsync().ConfigureAwait(false);
        _factory = new ApiFactory(connectionString, withTestAuthentication: true);
        await _factory.ApplyMigrationsAsync().ConfigureAwait(false);
    }

    public Task DisposeAsync()
    {
        _factory.Dispose();
        return Task.CompletedTask;
    }

    private HttpClient ClientAs(string subject, string? role)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.HeaderName, subject);
        if (role is not null)
        {
            client.DefaultRequestHeaders.Add(TestAuthHandler.RoleHeaderName, role);
        }

        return client;
    }

    private HttpClient Admin(string subject = "admin-1") => ClientAs(subject, "admin");
    private HttpClient Member(string subject = "member-1") => ClientAs(subject, "member");

    private static StringContent Json(string body) => new(body, Encoding.UTF8, "application/json");

    [Fact]
    public async Task AC1_an_admin_creates_a_project_and_gets_it_back_with_its_key()
    {
        using var client = Admin();

        var response = await client.PostAsJsonAsync(
            new Uri("/projects", UriKind.Relative), new { key = "GOTI", name = "Got Issues" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = document.RootElement;
        Assert.Equal("GOTI", root.GetProperty("key").GetString());
        Assert.Equal("Got Issues", root.GetProperty("name").GetString());
        Assert.NotEqual(Guid.Empty, root.GetProperty("id").GetGuid());
    }

    [Theory]
    [InlineData("goti", "lowercase")]
    [InlineData("Got Issues!", "spaces and punctuation")]
    [InlineData("TOOLONGAKEY1", "longer than the declared maximum")]
    [InlineData("G", "shorter than the declared minimum")]
    [InlineData("1GOTI", "does not start with a letter")]
    public async Task AC1b_a_key_outside_the_declared_pattern_is_rejected(string key, string why)
    {
        // The pattern lives in spec/openapi.yaml and reaches the request through the
        // generated model's annotations. Nothing in the API validates it by hand,
        // which is the point: a rule enforced only in a controller is a rule the
        // contract does not carry and generated clients never see.
        using var client = Admin();

        var response = await client.PostAsync(
            new Uri("/projects", UriKind.Relative),
            Json($$"""{"key":"{{key}}","name":"Rejected because it is {{why}}"}"""));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(
            "application/problem+json",
            response.Content.Headers.ContentType?.MediaType);

        // The criterion says the problem document *names* the key, so assert that.
        // A substring search for "key" would also be satisfied by the `type` URI, by a
        // trace identifier, or by the caller's own input echoed back — three ways to
        // pass without the API having identified the offending field at all. The
        // marker only correct behaviour emits is structural: a validation error keyed
        // by the property name.
        AssertNamesField(await response.Content.ReadAsStringAsync(), "key");

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<GotIssuesDbContext>();
        Assert.Equal(0, await db.Projects.CountAsync());
    }

    [Fact]
    public async Task AC1c_a_key_already_in_use_is_rejected_with_409()
    {
        using var client = Admin();
        var body = new { key = "DUPE", name = "First" };

        Assert.Equal(
            HttpStatusCode.Created,
            (await client.PostAsJsonAsync(new Uri("/projects", UriKind.Relative), body)).StatusCode);

        var second = await client.PostAsJsonAsync(
            new Uri("/projects", UriKind.Relative), new { key = "DUPE", name = "Second" });

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
        Assert.Equal("application/problem+json", second.Content.Headers.ContentType?.MediaType);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<GotIssuesDbContext>();
        Assert.Equal(1, await db.Projects.CountAsync(p => p.Key == "DUPE"));
    }

    [Fact]
    public async Task AC1c_two_concurrent_creates_of_one_key_produce_exactly_one_project()
    {
        // The criterion this ticket's Risks section singles out: "the constraint is the
        // guarantee, the check is the error message". A read-then-insert check passes
        // the sequential test above and fails here, because both requests find nothing
        // and both insert. Only the unique index can refuse the second.
        using var a = Admin("admin-a");
        using var b = Admin("admin-b");
        var body = new { key = "RACE", name = "Concurrent" };

        var responses = await Task.WhenAll(
            a.PostAsJsonAsync(new Uri("/projects", UriKind.Relative), body),
            b.PostAsJsonAsync(new Uri("/projects", UriKind.Relative), body));

        Assert.Equal(1, responses.Count(r => r.StatusCode == HttpStatusCode.Created));
        Assert.Equal(1, responses.Count(r => r.StatusCode == HttpStatusCode.Conflict));

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<GotIssuesDbContext>();
        Assert.Equal(1, await db.Projects.CountAsync(p => p.Key == "RACE"));
    }

    [Fact]
    public void AC1d_the_contract_exposes_no_operation_that_could_change_a_key()
    {
        // AC1d is an absence, and an absence proven by "I did not write one" is not
        // proven. This asserts it of the generated contract — the artefact the
        // specification produces — so adding an update operation to spec/openapi.yaml
        // fails this test rather than silently making the key mutable.
        var operations = typeof(ProjectsApiController)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(m => m.IsAbstract)
            .Select(m => m.Name)
            .OrderBy(n => n, StringComparer.Ordinal)
            .ToList();

        Assert.Equal(ExpectedOperations, operations);

        // And the key has no setter: immutable in the type, not only in the absence
        // of an endpoint.
        var key = typeof(ProjectRecord).GetProperty(nameof(ProjectRecord.Key))!;
        Assert.True(
            key.SetMethod!.ReturnParameter.GetRequiredCustomModifiers()
                .Any(m => m.Name == "IsExternalInit"),
            "ProjectRecord.Key must be init-only: every issue reference derives from it.");
    }

    [Fact]
    public async Task AC2_a_member_may_not_create_a_project()
    {
        using var client = Member();

        var response = await client.PostAsJsonAsync(
            new Uri("/projects", UriKind.Relative), new { key = "NOPE", name = "Refused" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<GotIssuesDbContext>();
        Assert.Equal(0, await db.Projects.CountAsync());
    }

    [Fact]
    public async Task AC2b_either_role_may_list_projects()
    {
        using var admin = Admin();
        await admin.PostAsJsonAsync(
            new Uri("/projects", UriKind.Relative), new { key = "LIST", name = "Listable" });

        foreach (var client in new[] { Admin("reader-admin"), Member("reader-member") })
        {
            using (client)
            {
                var response = await client.GetAsync(new Uri("/projects", UriKind.Relative));
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            }
        }
    }

    [Fact]
    public async Task AC2b_a_caller_with_an_unrecognised_role_is_refused()
    {
        // T-0009 AC4: an unrecognised role satisfies nothing and is never silently
        // promoted to member. Listing is "open to any recognised role", which is not
        // the same as open to anyone authenticated.
        using var client = ClientAs("stranger", "superuser");

        var response = await client.GetAsync(new Uri("/projects", UriKind.Relative));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task AC2c_an_unauthenticated_caller_gets_401_not_403()
    {
        // The distinction matters to a client: 401 means "authenticate", 403 means
        // "you did, and it was not enough".
        using var anonymous = _factory.CreateClient();

        var listed = await anonymous.GetAsync(new Uri("/projects", UriKind.Relative));
        var created = await anonymous.PostAsJsonAsync(
            new Uri("/projects", UriKind.Relative), new { key = "ANON", name = "Anonymous" });

        Assert.Equal(HttpStatusCode.Unauthorized, listed.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, created.StatusCode);
    }

    [Theory]
    [InlineData("""{"key":"NONAME"}""", "name")]
    [InlineData("""{"key":"NONAME","name":""}""", "name")]
    [InlineData("""{"name":"No key"}""", "key")]
    public async Task AC3_invalid_input_returns_a_problem_document_naming_the_field(
        string body, string offendingField)
    {
        using var client = Admin();

        var response = await client.PostAsync(new Uri("/projects", UriKind.Relative), Json(body));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(
            "application/problem+json",
            response.Content.Headers.ContentType?.MediaType);

        // Previously this asserted only that the body was non-empty, which `{}` and an
        // HTML error page both satisfy — the criterion requires the document to name
        // the offending field, so that is what is asserted.
        AssertNamesField(await response.Content.ReadAsStringAsync(), offendingField);
    }

    /// <summary>
    /// Asserts an RFC 9457 validation problem identifies <paramref name="field"/> as the
    /// offender, by the <c>errors</c> member being keyed on the property name rather
    /// than by the field name appearing anywhere in the payload.
    /// </summary>
    private static void AssertNamesField(string body, string field)
    {
        using var document = JsonDocument.Parse(body);

        Assert.True(
            document.RootElement.TryGetProperty("errors", out var errors),
            $"The problem document carries no 'errors' member, so it names nothing: {body}");

        var named = errors.EnumerateObject()
            .Any(e => string.Equals(e.Name, field, StringComparison.OrdinalIgnoreCase));

        Assert.True(
            named,
            $"The problem document does not name '{field}' as the offending field. "
            + $"Members present: {string.Join(", ", errors.EnumerateObject().Select(e => e.Name))}");
    }

    [Fact]
    public async Task AC4_the_list_is_paginated_and_the_caller_can_reach_the_rest()
    {
        using var client = Admin();
        for (var i = 0; i < 5; i++)
        {
            await client.PostAsJsonAsync(
                new Uri("/projects", UriKind.Relative), new { key = $"PG{i}", name = $"Project {i}" });
        }

        var first = await client.GetAsync(new Uri("/projects?page=1&pageSize=2", UriKind.Relative));
        using var document = JsonDocument.Parse(await first.Content.ReadAsStringAsync());
        var root = document.RootElement;

        Assert.Equal(2, root.GetProperty("items").GetArrayLength());
        Assert.Equal(2, root.GetProperty("pageSize").GetInt32());
        Assert.Equal(1, root.GetProperty("page").GetInt32());

        // totalCount is what tells a client there is more to fetch. Without it, a
        // full page is indistinguishable from the last page.
        Assert.Equal(5, root.GetProperty("totalCount").GetInt32());
    }

    [Fact]
    public async Task An_empty_list_is_an_empty_page_not_a_404()
    {
        using var client = Member();

        var response = await client.GetAsync(new Uri("/projects", UriKind.Relative));
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(0, document.RootElement.GetProperty("items").GetArrayLength());
        Assert.Equal(0, document.RootElement.GetProperty("totalCount").GetInt32());
    }

    [Fact]
    public async Task Two_projects_may_share_a_name()
    {
        // Decided in the ticket's Risks section and confirmed when the Examples and
        // Risks disagreed: the key is the identifier, and unique names would be a
        // constraint on people rather than on data.
        using var client = Admin();

        var first = await client.PostAsJsonAsync(
            new Uri("/projects", UriKind.Relative), new { key = "SAME1", name = "Platform" });
        var second = await client.PostAsJsonAsync(
            new Uri("/projects", UriKind.Relative), new { key = "SAME2", name = "Platform" });

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.Created, second.StatusCode);
    }
}
