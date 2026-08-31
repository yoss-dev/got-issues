using System.Net;
using GotIssues.Api.Data;
using GotIssues.Api.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GotIssues.Api.IntegrationTests;

/// <summary>
/// The role matrix and the user projection (T-0009).
///
/// The refusal cases carry the weight here. A suite that only proves permitted access
/// proves nothing about authorisation — and the distinction between 401 and 403 is
/// exactly the kind a status-only assertion blurs.
/// </summary>
[Collection(PostgresFixtureDefinition.Name)]
public sealed class RoleAuthorizationTests(PostgresContainerFixture postgres) : IAsyncLifetime
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

    private HttpClient ClientAs(string subject, string? role = null, string? name = null)
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

    private static Uri Admin => new(GuardedEndpointStartupFilter.AdminRoute, UriKind.Relative);
    private static Uri Member => new(GuardedEndpointStartupFilter.MemberRoute, UriKind.Relative);

    [Fact]
    public async Task An_admin_reaches_an_admin_endpoint()
    {
        using var client = ClientAs("admin-1", "admin");
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync(Admin)).StatusCode);
    }

    [Fact]
    public async Task A_member_is_refused_an_admin_endpoint_with_403_not_401()
    {
        // 403, not 401: the caller is authenticated and simply not permitted. The two
        // codes mean different things to a client, and a test asserting only
        // "refused" would pass while the API told the caller the wrong thing.
        using var client = ClientAs("member-1", "member");

        var response = await client.GetAsync(Admin);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory]
    [InlineData("admin")]
    [InlineData("member")]
    public async Task Either_role_reaches_a_member_endpoint(string role)
    {
        // An admin can do anything a member can (PROJECT.md §5), so the member policy
        // is a floor rather than an exact match.
        using var client = ClientAs($"{role}-2", role);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync(Member)).StatusCode);
    }

    [Theory]
    [InlineData(null, "member endpoint")]
    [InlineData("superuser", "member endpoint")]
    [InlineData("", "member endpoint")]
    [InlineData("Admin", "member endpoint")]
    public async Task An_absent_or_unrecognised_role_is_refused_never_promoted(
        string? role, string _)
    {
        // AC4. The plausible-looking implementation — "admin if the claim says admin,
        // otherwise member" — passes every test above and silently promotes all four
        // of these. The policies use an allow-list precisely so these fail.
        //
        // "Admin" is included deliberately: the comparison is ordinal, so a
        // case-different value is unrecognised rather than helpfully matched.
        using var client = ClientAs($"unknown-{role ?? "none"}", role);

        var response = await client.GetAsync(Member);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task An_unrecognised_role_is_also_refused_an_admin_endpoint()
    {
        using var client = ClientAs("unknown-admin", "superuser");
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync(Admin)).StatusCode);
    }

    [Fact]
    public async Task A_first_request_creates_the_user_projection()
    {
        using var client = ClientAs("projected-1", "member", "Sam Example");
        await client.GetAsync(Member);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<GotIssuesDbContext>();
        var user = await db.Users.SingleAsync(u => u.Subject == "projected-1");

        Assert.Equal("Sam Example", user.DisplayName);
    }

    [Fact]
    public async Task Returning_updates_the_record_rather_than_duplicating_it()
    {
        using var first = ClientAs("projected-2", "member", "Original Name");
        await first.GetAsync(Member);

        using var second = ClientAs("projected-2", "member", "Renamed In Duende");
        await second.GetAsync(Member);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<GotIssuesDbContext>();

        // One record, not two — and the name follows the token rather than sticking.
        Assert.Equal(1, await db.Users.CountAsync(u => u.Subject == "projected-2"));
        Assert.Equal("Renamed In Duende",
            (await db.Users.SingleAsync(u => u.Subject == "projected-2")).DisplayName);
    }

    [Fact]
    public async Task A_token_without_a_display_name_still_produces_a_usable_projection()
    {
        // AC8. A missing optional claim must not fail the request: the caller is still
        // a valid assignee, just an unnamed one.
        using var client = ClientAs("projected-3", "member");

        var response = await client.GetAsync(Member);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<GotIssuesDbContext>();
        var user = await db.Users.SingleAsync(u => u.Subject == "projected-3");

        Assert.Null(user.DisplayName);
    }

    [Fact]
    public async Task Projecting_a_user_logs_neither_the_display_name_nor_the_email()
    {
        // AC7. Names and email addresses belong to identifiable employees
        // (SECURITY.md, PROJECT.md Q8). Asserted against captured log output rather
        // than by reading the code, so a future log statement that leaks one fails
        // this test instead of passing review.
        var captured = new CapturingLoggerProvider();
        using var factory = _factory.WithLogCapture(captured);

        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.HeaderName, "logged-1");
        client.DefaultRequestHeaders.Add(TestAuthHandler.RoleHeaderName, "member");
        client.DefaultRequestHeaders.Add(TestAuthHandler.NameHeaderName, "Priya Confidential");

        await client.GetAsync(Member);

        var log = captured.Text;
        Assert.DoesNotContain("Priya Confidential", log, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("logged-1@", log, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Concurrent_first_requests_from_one_subject_do_not_fail()
    {
        // Both requests see no existing record and both insert; one loses on the
        // primary key. The loser must not become a 500 on a caller's first request.
        using var a = ClientAs("racer-1", "member", "Racer");
        using var b = ClientAs("racer-1", "member", "Racer");

        var responses = await Task.WhenAll(a.GetAsync(Member), b.GetAsync(Member));

        Assert.All(responses, r => Assert.Equal(HttpStatusCode.OK, r.StatusCode));

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<GotIssuesDbContext>();
        Assert.Equal(1, await db.Users.CountAsync(u => u.Subject == "racer-1"));
    }

    [Fact]
    public async Task A_subject_at_the_OIDC_limit_is_projected()
    {
        // The column was 200 characters while OpenID Connect permits a `sub` of up to
        // 255. Once the write-failure catch was correctly narrowed, a legal subject of
        // 201-255 characters became a hard failure on every request — the narrowing
        // converted a silent loss into a loud refusal of valid input. The column is
        // now 255, so a legal subject fits.
        var subject = new string('s', 255);
        using var client = ClientAs(subject, "member");

        var response = await client.GetAsync(Member);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<GotIssuesDbContext>();
        Assert.Equal(1, await db.Users.CountAsync(u => u.Subject == subject));
    }

    [Fact]
    public async Task A_subject_beyond_the_OIDC_limit_fails_loudly_rather_than_silently()
    {
        // Q1, found in acceptance. The race catch was DbUpdateException wholesale, so
        // a subject longer than the column returned 200 with no row written and
        // nothing logged — a caller told they succeeded, then permanently unusable as
        // an assignee. OIDC permits 255 characters; the column holds 200.
        //
        // The requirement is that it must NOT silently succeed. A 500 is the correct
        // loud failure here: it is a real write failure, not a race someone else won.
        // Beyond 255 is outside the specification, so a loud failure is correct —
        // what must never happen is a 200 with nothing written.
        using var client = ClientAs(new string('x', 300), "member");

        var thrown = await Record.ExceptionAsync(() => client.GetAsync(Member));

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<GotIssuesDbContext>();
        var written = await db.Users.CountAsync(u => u.Subject.StartsWith("xxxx"));

        // Either it threw, or it returned a non-success status — what must not happen
        // is a 200 with nothing written.
        Assert.True(thrown is not null || written > 0,
            "an oversized subject was silently discarded and the caller was told it worked");
    }

    [Fact]
    public async Task The_projection_stores_no_role_and_no_credential()
    {
        // AC6. Asserted against the model rather than by reading the class: the role
        // is read from the token per request, and a cached copy here could disagree
        // with the token a caller just presented.
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<GotIssuesDbContext>();

        var properties = db.Model.FindEntityType(typeof(UserRecord))!
            .GetProperties()
            .Select(p => p.Name)
            .ToList();

        Assert.DoesNotContain(properties, n => n.Contains("role", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(properties, n => n.Contains("secret", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(properties, n => n.Contains("password", StringComparison.OrdinalIgnoreCase));
        Assert.Equal(
            ["DisplayName", "FirstSeenAt", "LastSeenAt", "Subject"],
            properties.Order(StringComparer.Ordinal).ToList());
    }
}
