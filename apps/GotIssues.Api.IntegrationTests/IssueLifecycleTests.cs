using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using GotIssues.Api.Data;
using GotIssues.Api.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GotIssues.Api.IntegrationTests;

/// <summary>T-0006: an issue's type, status, priority and assignee, and how they change.</summary>
[Collection(PostgresFixtureDefinition.Name)]
public sealed class IssueLifecycleTests(PostgresContainerFixture postgres) : IAsyncLifetime
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

    private HttpClient ClientAs(string subject, string? role, string? name = null)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.HeaderName, subject);
        if (role is not null)
        {
            client.DefaultRequestHeaders.Add(TestAuthHandler.RoleHeaderName, role);
        }

        if (name is not null)
        {
            client.DefaultRequestHeaders.Add(TestAuthHandler.NameHeaderName, name);
        }

        return client;
    }

    private HttpClient Member(string subject = "member-1", string? name = null) =>
        ClientAs(subject, "member", name);

    private static StringContent Json(string body) => new(body, Encoding.UTF8, "application/json");

    /// <summary>Creates a project and one issue, returning the issue's key.</summary>
    private async Task<string> SeedIssueAsync(string projectKey = "GOTI")
    {
        using var admin = ClientAs($"seed-{projectKey}", "admin");
        Assert.Equal(
            HttpStatusCode.Created,
            (await admin.PostAsJsonAsync(
                new Uri("/projects", UriKind.Relative),
                new { key = projectKey, name = $"Project {projectKey}" })).StatusCode);

        var created = await admin.PostAsJsonAsync(
            new Uri($"/projects/{projectKey}/issues", UriKind.Relative), new { title = "Seeded" });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);

        using var document = JsonDocument.Parse(await created.Content.ReadAsStringAsync());
        return document.RootElement.GetProperty("key").GetString()!;
    }

    private static async Task<JsonElement> BodyOf(HttpResponseMessage response)
    {
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return document.RootElement.Clone();
    }

    private static async Task<JsonElement> ReadAsync(HttpClient client, string issueKey)
    {
        var response = await client.GetAsync(new Uri($"/issues/{issueKey}", UriKind.Relative));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await BodyOf(response);
    }

    [Fact]
    public async Task AC7_a_new_issue_carries_the_declared_defaults()
    {
        var key = await SeedIssueAsync();
        using var client = Member();

        var issue = await ReadAsync(client, key);

        Assert.Equal("task", issue.GetProperty("type").GetString());
        Assert.Equal("open", issue.GetProperty("status").GetString());
        Assert.Equal("normal", issue.GetProperty("priority").GetString());
        Assert.Equal(JsonValueKind.Null, issue.GetProperty("assignee").ValueKind);
    }

    [Theory]
    [InlineData("type", "bug")]
    [InlineData("status", "in_progress")]
    [InlineData("priority", "high")]
    public async Task AC1_a_field_changes_and_stays_changed(string field, string value)
    {
        var key = await SeedIssueAsync();
        using var client = Member();

        var patched = await client.PatchAsync(
            new Uri($"/issues/{key}", UriKind.Relative), Json($$"""{"{{field}}":"{{value}}"}"""));

        Assert.Equal(HttpStatusCode.OK, patched.StatusCode);
        Assert.Equal(value, (await BodyOf(patched)).GetProperty(field).GetString());

        // Read back through a separate request: the response echoing the change proves
        // the handler, not the persistence.
        Assert.Equal(value, (await ReadAsync(client, key)).GetProperty(field).GetString());
    }

    [Fact]
    public async Task AC1_an_omitted_field_is_left_alone()
    {
        // The distinction that makes this a PATCH: changing the status must not reset
        // the priority to its default, which is what a replace would do.
        var key = await SeedIssueAsync();
        using var client = Member();

        await client.PatchAsync(new Uri($"/issues/{key}", UriKind.Relative), Json("""{"priority":"high"}"""));
        await client.PatchAsync(new Uri($"/issues/{key}", UriKind.Relative), Json("""{"status":"done"}"""));

        var issue = await ReadAsync(client, key);
        Assert.Equal("high", issue.GetProperty("priority").GetString());
        Assert.Equal("done", issue.GetProperty("status").GetString());
        Assert.Equal("task", issue.GetProperty("type").GetString());
    }

    [Theory]
    [InlineData("""{"status":"cancelled"}""", "a status outside the declared set")]
    [InlineData("""{"type":"epic"}""", "a type outside the declared set")]
    [InlineData("""{"priority":"critical"}""", "a priority outside the declared set")]
    public async Task AC2_a_value_outside_the_declared_set_is_rejected(string body, string _)
    {
        var key = await SeedIssueAsync();
        using var client = Member();

        var response = await client.PatchAsync(new Uri($"/issues/{key}", UriKind.Relative), Json(body));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(
            "application/problem+json",
            response.Content.Headers.ContentType?.MediaType);

        // And nothing changed.
        Assert.Equal("open", (await ReadAsync(client, key)).GetProperty("status").GetString());
    }

    [Fact]
    public async Task AC3_an_issue_is_assigned_reassigned_and_unassigned()
    {
        var key = await SeedIssueAsync();

        // A user exists only once their token has been seen: the projection is written
        // by the authentication pipeline, not by an admin endpoint.
        using var alice = Member("alice", "Alice Example");
        using var bob = Member("bob", "Bob Example");
        await alice.GetAsync(new Uri($"/issues/{key}", UriKind.Relative));
        await bob.GetAsync(new Uri($"/issues/{key}", UriKind.Relative));

        using var client = Member();

        var assigned = await client.PatchAsync(
            new Uri($"/issues/{key}", UriKind.Relative),
            Json("""{"assignment":{"subject":"alice"}}"""));
        Assert.Equal(HttpStatusCode.OK, assigned.StatusCode);

        var afterAssign = (await BodyOf(assigned)).GetProperty("assignee");
        Assert.Equal("alice", afterAssign.GetProperty("subject").GetString());
        Assert.Equal("Alice Example", afterAssign.GetProperty("displayName").GetString());

        var reassigned = await client.PatchAsync(
            new Uri($"/issues/{key}", UriKind.Relative),
            Json("""{"assignment":{"subject":"bob"}}"""));
        Assert.Equal(HttpStatusCode.OK, reassigned.StatusCode);
        Assert.Equal("bob", (await BodyOf(reassigned)).GetProperty("assignee").GetProperty("subject").GetString());

        var unassigned = await client.PatchAsync(
            new Uri($"/issues/{key}", UriKind.Relative),
            Json("""{"assignment":{"subject":null}}"""));

        // Asserted before the body is read: without this, a refused unassign surfaces
        // as a missing property on a problem document, which reads like a serialisation
        // bug rather than the rejection it is. It cost a diagnosis here.
        Assert.Equal(HttpStatusCode.OK, unassigned.StatusCode);
        Assert.Equal(
            JsonValueKind.Null,
            (await BodyOf(unassigned)).GetProperty("assignee").ValueKind);

        Assert.Equal(JsonValueKind.Null, (await ReadAsync(client, key)).GetProperty("assignee").ValueKind);
    }

    [Fact]
    public async Task AC3_omitting_the_assignment_leaves_the_holder_alone()
    {
        // The reason the contract wraps assignment in an object: absent and null have
        // to mean different things, and a bare nullable subject could not say both.
        var key = await SeedIssueAsync();
        using var alice = Member("alice", "Alice Example");
        await alice.GetAsync(new Uri($"/issues/{key}", UriKind.Relative));

        using var client = Member();
        await client.PatchAsync(
            new Uri($"/issues/{key}", UriKind.Relative), Json("""{"assignment":{"subject":"alice"}}"""));

        // A patch that says nothing about assignment must not unassign.
        await client.PatchAsync(new Uri($"/issues/{key}", UriKind.Relative), Json("""{"status":"done"}"""));

        var issue = await ReadAsync(client, key);
        Assert.Equal("alice", issue.GetProperty("assignee").GetProperty("subject").GetString());
        Assert.Equal("done", issue.GetProperty("status").GetString());
    }

    [Fact]
    public async Task AC4_assigning_to_an_unknown_subject_is_rejected_and_changes_nothing()
    {
        var key = await SeedIssueAsync();
        using var client = Member();

        var response = await client.PatchAsync(
            new Uri($"/issues/{key}", UriKind.Relative),
            Json("""{"status":"done","assignment":{"subject":"nobody-here"}}"""));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(
            "application/problem+json",
            response.Content.Headers.ContentType?.MediaType);

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.True(
            document.RootElement.TryGetProperty("errors", out var errors)
            && errors.EnumerateObject().Any(e => e.Name.Contains("Subject", StringComparison.Ordinal)),
            "The problem document does not name the offending field.");

        // The status change in the same request must not have landed either: a rejected
        // request changes nothing at all.
        Assert.Equal("open", (await ReadAsync(client, key)).GetProperty("status").GetString());
    }

    [Fact]
    public async Task A_subject_carrying_a_control_character_is_rejected_at_the_boundary()
    {
        // Found in review: `subject` was the one free-text request string in the
        // contract with no pattern, so a NUL reached PostgreSQL and came back as
        // `22021: invalid byte sequence` — an unhandled 500 where AC4 requires a 400
        // naming the field. T-0004 shipped this exact defect in a project name; this
        // is it recurring in a field added two tickets later.
        var key = await SeedIssueAsync();
        using var client = Member();

        var response = await client.PatchAsync(
            new Uri($"/issues/{key}", UriKind.Relative),
            Json("{\"assignment\":{\"subject\":\"\u0000bad\"}}"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(
            "application/problem+json",
            response.Content.Headers.ContentType?.MediaType);

        // AC4 says the document names the offending field, so assert that rather than
        // stopping at the status — the shortfall review caught in the first version of
        // this test.
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.True(document.RootElement.TryGetProperty("errors", out var errors), "No 'errors' member.");
        Assert.True(
            errors.EnumerateObject().Any(e =>
                e.Name.Contains("subject", StringComparison.OrdinalIgnoreCase)),
            "The problem document does not name the offending field. Members present: "
            + string.Join(", ", errors.EnumerateObject().Select(e => e.Name)));
    }

    [Fact]
    public async Task An_assignment_object_without_a_subject_unassigns_as_the_contract_says()
    {
        // Review found this behaviour undocumented: `{"assignment":{}}` unassigns and
        // returns 200, because an absent subject is indistinguishable from an explicit
        // null — the same limitation this wrapper exists to work around one level up.
        // The contract now states it, so this asserts what the document promises rather
        // than what the code happens to do.
        var key = await SeedIssueAsync();
        using var alice = Member("alice", "Alice Example");
        await alice.GetAsync(new Uri($"/issues/{key}", UriKind.Relative));

        using var client = Member();
        await client.PatchAsync(
            new Uri($"/issues/{key}", UriKind.Relative), Json("""{"assignment":{"subject":"alice"}}"""));

        var response = await client.PatchAsync(
            new Uri($"/issues/{key}", UriKind.Relative), Json("""{"assignment":{}}"""));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(JsonValueKind.Null, (await BodyOf(response)).GetProperty("assignee").ValueKind);

        // And the neighbouring case the contract also states: a null assignment object
        // leaves the holder alone, rather than unassigning.
        await client.PatchAsync(
            new Uri($"/issues/{key}", UriKind.Relative), Json("""{"assignment":{"subject":"alice"}}"""));
        var untouched = await client.PatchAsync(
            new Uri($"/issues/{key}", UriKind.Relative), Json("""{"assignment":null}"""));

        Assert.Equal(HttpStatusCode.OK, untouched.StatusCode);
        Assert.Equal(
            "alice",
            (await BodyOf(untouched)).GetProperty("assignee").GetProperty("subject").GetString());
    }

    [Fact]
    public async Task AC5_any_status_may_follow_any_other_including_backwards()
    {
        // A criterion against gold-plating. Transition rules are a later product goal,
        // and an implementer who "improves" this by adding them breaks it deliberately.
        var key = await SeedIssueAsync();
        using var client = Member();

        foreach (var status in new[] { "done", "open", "done", "in_progress", "open" })
        {
            var response = await client.PatchAsync(
                new Uri($"/issues/{key}", UriKind.Relative), Json($$"""{"status":"{{status}}"}"""));

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(status, (await BodyOf(response)).GetProperty("status").GetString());
        }
    }

    [Fact]
    public async Task AC8_a_member_may_change_every_field_and_assign()
    {
        var key = await SeedIssueAsync();
        using var alice = Member("alice", "Alice Example");
        await alice.GetAsync(new Uri($"/issues/{key}", UriKind.Relative));

        using var client = Member("plain-member");

        var response = await client.PatchAsync(
            new Uri($"/issues/{key}", UriKind.Relative),
            Json("""{"type":"bug","status":"in_progress","priority":"high","assignment":{"subject":"alice"}}"""));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task AC8_a_caller_with_an_unrecognised_role_is_refused()
    {
        var key = await SeedIssueAsync();
        using var client = ClientAs("stranger", "superuser");

        var response = await client.PatchAsync(
            new Uri($"/issues/{key}", UriKind.Relative), Json("""{"status":"done"}"""));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task An_unauthenticated_caller_is_refused_and_changes_nothing()
    {
        var key = await SeedIssueAsync();
        using var anonymous = _factory.CreateClient();

        var response = await anonymous.PatchAsync(
            new Uri($"/issues/{key}", UriKind.Relative), Json("""{"status":"done"}"""));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        using var client = Member();
        Assert.Equal("open", (await ReadAsync(client, key)).GetProperty("status").GetString());
    }

    [Fact]
    public async Task Patching_an_issue_that_does_not_exist_is_404()
    {
        await SeedIssueAsync();
        using var client = Member();

        var response = await client.PatchAsync(
            new Uri("/issues/GOTI-99", UriKind.Relative), Json("""{"status":"done"}"""));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(
            "application/problem+json",
            response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task An_assignee_who_never_set_a_display_name_reads_back_with_a_null_one()
    {
        // T-0009 AC8: a token may carry no name, and the caller stays usable as an
        // assignee. The contract declares displayName nullable for exactly this.
        var key = await SeedIssueAsync();
        using var nameless = Member("nameless");
        await nameless.GetAsync(new Uri($"/issues/{key}", UriKind.Relative));

        using var client = Member();
        var response = await client.PatchAsync(
            new Uri($"/issues/{key}", UriKind.Relative),
            Json("""{"assignment":{"subject":"nameless"}}"""));

        var assignee = (await BodyOf(response)).GetProperty("assignee");
        Assert.Equal("nameless", assignee.GetProperty("subject").GetString());
        Assert.Equal(JsonValueKind.Null, assignee.GetProperty("displayName").ValueKind);
    }

    [Fact]
    public async Task Deleting_a_user_cannot_delete_the_work_they_hold()
    {
        // The foreign key is Restrict, not Cascade. Nothing deletes users yet, so this
        // asserts the decision rather than a code path — if someone later adds user
        // deletion, this test tells them the issues are in the way, which is the point.
        var key = await SeedIssueAsync();
        using var alice = Member("alice", "Alice Example");
        await alice.GetAsync(new Uri($"/issues/{key}", UriKind.Relative));

        using var client = Member();
        await client.PatchAsync(
            new Uri($"/issues/{key}", UriKind.Relative), Json("""{"assignment":{"subject":"alice"}}"""));

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<GotIssuesDbContext>();
        db.Users.Remove(await db.Users.SingleAsync(u => u.Subject == "alice"));

        await Assert.ThrowsAnyAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }
}
