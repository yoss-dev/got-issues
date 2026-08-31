using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using GotIssues.Api.Data;
using GotIssues.Api.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GotIssues.Api.IntegrationTests;

/// <summary>T-0005's acceptance criteria: creating and reading issues, and how they are numbered.</summary>
[Collection(PostgresFixtureDefinition.Name)]
public sealed class IssuesTests(PostgresContainerFixture postgres) : IAsyncLifetime
{
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

    private async Task CreateProjectAsync(string key, string name)
    {
        using var admin = Admin($"seed-{key}");
        var response = await admin.PostAsJsonAsync(
            new Uri("/projects", UriKind.Relative), new { key, name });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    private static async Task<JsonElement> BodyOf(HttpResponseMessage response)
    {
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return document.RootElement.Clone();
    }

    [Fact]
    public async Task AC1_an_issue_is_created_in_a_project_and_carries_a_key_of_project_and_number()
    {
        await CreateProjectAsync("GOTI", "Got Issues");
        using var client = Member();

        var response = await client.PostAsJsonAsync(
            new Uri("/projects/GOTI/issues", UriKind.Relative),
            new { title = "The first issue", description = "Line one\nLine two" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await BodyOf(response);
        Assert.Equal("GOTI-1", body.GetProperty("key").GetString());
        Assert.Equal("GOTI", body.GetProperty("projectKey").GetString());
        Assert.Equal(1, body.GetProperty("number").GetInt32());
        Assert.Equal("The first issue", body.GetProperty("title").GetString());
        Assert.Equal("Line one\nLine two", body.GetProperty("description").GetString());
    }

    [Fact]
    public async Task AC1b_numbering_starts_at_one_in_each_project()
    {
        await CreateProjectAsync("GOTI", "Got Issues");
        using var client = Member();

        var first = await client.PostAsJsonAsync(
            new Uri("/projects/GOTI/issues", UriKind.Relative), new { title = "First" });

        Assert.Equal(1, (await BodyOf(first)).GetProperty("number").GetInt32());
    }

    [Fact]
    public async Task AC1c_two_projects_number_independently()
    {
        await CreateProjectAsync("GOTI", "Got Issues");
        await CreateProjectAsync("PROJ", "Other");
        using var client = Member();

        // Three in the first project, so a global counter would put the second
        // project's first issue at 4 rather than 1 - the failure this catches.
        for (var i = 0; i < 3; i++)
        {
            await client.PostAsJsonAsync(
                new Uri("/projects/GOTI/issues", UriKind.Relative), new { title = $"Issue {i}" });
        }

        var other = await client.PostAsJsonAsync(
            new Uri("/projects/PROJ/issues", UriKind.Relative), new { title = "First elsewhere" });

        var body = await BodyOf(other);
        Assert.Equal("PROJ-1", body.GetProperty("key").GetString());
        Assert.Equal(1, body.GetProperty("number").GetInt32());
    }

    [Fact]
    public async Task AC1d_ten_concurrent_creates_produce_ten_distinct_consecutive_numbers()
    {
        // The criterion this ticket exists for. Its Risks say it plainly: the obvious
        // implementation - MAX(number)+1, or an in-memory counter - duplicates here
        // and passes every test above. Only concurrency can tell the difference.
        await CreateProjectAsync("RACE", "Concurrent");

        var clients = Enumerable.Range(0, 10).Select(i => Member($"racer-{i}")).ToList();

        try
        {
            var responses = await Task.WhenAll(clients.Select(c =>
                c.PostAsJsonAsync(
                    new Uri("/projects/RACE/issues", UriKind.Relative), new { title = "Concurrent" })));

            Assert.All(responses, r => Assert.Equal(HttpStatusCode.Created, r.StatusCode));

            var numbers = new List<int>();
            foreach (var response in responses)
            {
                numbers.Add((await BodyOf(response)).GetProperty("number").GetInt32());
            }

            // Distinct, and 1..10 with nothing skipped - the criterion asks for both,
            // and an allocator can satisfy the first while failing the second.
            Assert.Equal(10, numbers.Distinct().Count());
            Assert.Equal(Enumerable.Range(1, 10), numbers.OrderBy(n => n));

            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<GotIssuesDbContext>();
            Assert.Equal(10, await db.Issues.CountAsync());
        }
        finally
        {
            foreach (var client in clients)
            {
                client.Dispose();
            }
        }
    }

    [Fact]
    public async Task AC2_an_issue_is_read_by_its_key()
    {
        await CreateProjectAsync("GOTI", "Got Issues");
        using var client = Member();

        await client.PostAsJsonAsync(
            new Uri("/projects/GOTI/issues", UriKind.Relative),
            new { title = "Readable", description = "With detail" });

        var response = await client.GetAsync(new Uri("/issues/GOTI-1", UriKind.Relative));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await BodyOf(response);
        Assert.Equal("GOTI-1", body.GetProperty("key").GetString());
        Assert.Equal("Readable", body.GetProperty("title").GetString());
        Assert.Equal("With detail", body.GetProperty("description").GetString());
    }

    [Fact]
    public async Task AC3_creating_in_an_unknown_project_is_404_and_writes_nothing()
    {
        using var client = Member();

        var response = await client.PostAsJsonAsync(
            new Uri("/projects/NOPE/issues", UriKind.Relative), new { title = "Orphan" });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<GotIssuesDbContext>();
        Assert.Equal(0, await db.Issues.CountAsync());
    }

    [Fact]
    public async Task AC4_reading_an_unknown_key_is_404()
    {
        await CreateProjectAsync("GOTI", "Got Issues");
        using var client = Member();

        // A well-formed key in a real project that has no such issue: the case a
        // "does the project exist" check would wrongly answer 200 or 500.
        var response = await client.GetAsync(new Uri("/issues/GOTI-99", UriKind.Relative));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task AC5_an_unauthenticated_caller_is_refused_and_nothing_is_written()
    {
        await CreateProjectAsync("GOTI", "Got Issues");
        using var anonymous = _factory.CreateClient();

        var created = await anonymous.PostAsJsonAsync(
            new Uri("/projects/GOTI/issues", UriKind.Relative), new { title = "Anonymous" });
        var read = await anonymous.GetAsync(new Uri("/issues/GOTI-1", UriKind.Relative));

        Assert.Equal(HttpStatusCode.Unauthorized, created.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, read.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<GotIssuesDbContext>();
        Assert.Equal(0, await db.Issues.CountAsync());
    }

    [Fact]
    public async Task A_caller_with_an_unrecognised_role_is_refused()
    {
        await CreateProjectAsync("GOTI", "Got Issues");
        using var client = ClientAs("stranger", "superuser");

        var response = await client.PostAsJsonAsync(
            new Uri("/projects/GOTI/issues", UriKind.Relative), new { title = "Refused" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory]
    [InlineData("\"description\":\"No title\"", "title missing")]
    [InlineData("\"title\":\"\"", "title empty")]
    public async Task Invalid_input_returns_a_problem_document_naming_the_field(string fields, string why)
    {
        await CreateProjectAsync("GOTI", "Got Issues");
        using var client = Member();

        var response = await client.PostAsync(
            new Uri("/projects/GOTI/issues", UriKind.Relative), Json("{" + fields + "}"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.True(
            document.RootElement.TryGetProperty("errors", out var errors)
            && errors.EnumerateObject().Any(e =>
                string.Equals(e.Name, "Title", StringComparison.OrdinalIgnoreCase)),
            $"The problem document does not name 'title' ({why}).");
    }

    [Fact]
    public async Task A_title_may_not_span_lines_but_a_description_may()
    {
        // The distinction T-0004's review recorded for this ticket: the constraint on
        // a title is about being one line; the constraint on a description is about
        // being storable. Copying T-0004's pattern onto both would reject ordinary
        // multi-line text, and applying neither would let U+0000 reach PostgreSQL.
        await CreateProjectAsync("GOTI", "Got Issues");
        using var client = Member();

        var titleWithNewline = await client.PostAsync(
            new Uri("/projects/GOTI/issues", UriKind.Relative),
            Json("{\"title\":\"one\\ntwo\"}"));
        Assert.Equal(HttpStatusCode.BadRequest, titleWithNewline.StatusCode);

        var multiLine = await client.PostAsync(
            new Uri("/projects/GOTI/issues", UriKind.Relative),
            Json("{\"title\":\"Fine\",\"description\":\"para one\\n\\npara two\\n\"}"));
        Assert.Equal(HttpStatusCode.Created, multiLine.StatusCode);

        // ...and the one character neither may carry, because nothing can store it.
        var descriptionWithNul = await client.PostAsync(
            new Uri("/projects/GOTI/issues", UriKind.Relative),
            Json("{\"title\":\"Fine\",\"description\":\"bad\\u0000text\"}"));
        Assert.Equal(HttpStatusCode.BadRequest, descriptionWithNul.StatusCode);
    }

    [Fact]
    public async Task An_omitted_description_comes_back_as_null()
    {
        await CreateProjectAsync("GOTI", "Got Issues");
        using var client = Member();

        var created = await client.PostAsJsonAsync(
            new Uri("/projects/GOTI/issues", UriKind.Relative), new { title = "No detail" });

        var body = await BodyOf(created);
        Assert.Equal(JsonValueKind.Null, body.GetProperty("description").ValueKind);
    }
}
