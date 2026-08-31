using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using GotIssues.Api.Data;
using GotIssues.Api.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GotIssues.Api.IntegrationTests;

/// <summary>
/// What happens when the insert fails after a number has already been allocated.
///
/// <para>
/// This closes two claims that were, until now, honestly recorded as unproven: that
/// the explicit transaction returns the number on failure rather than burning it, and
/// that the unique index on <c>(ProjectId, Number)</c> does anything at all. With a
/// correct allocator the index is unobservable — so the only way to observe it is to
/// hand the allocator a number that is already taken.
/// </para>
/// <para>
/// The formulation is <c>claude-rev-5c14</c>'s, from T-0005's re-review. I had recorded
/// both properties as untestable without shipping a deliberately broken allocator; they
/// are not.
/// </para>
/// </summary>
[Collection(PostgresFixtureDefinition.Name)]
public sealed class AllocationRollbackTests(PostgresContainerFixture postgres) : IAsyncLifetime
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

    [Fact]
    public async Task A_failed_insert_returns_the_number_instead_of_burning_it()
    {
        using var admin = _factory.CreateClient();
        admin.DefaultRequestHeaders.Add(TestAuthHandler.HeaderName, "rollback-admin");
        admin.DefaultRequestHeaders.Add(TestAuthHandler.RoleHeaderName, "admin");

        var project = await admin.PostAsJsonAsync(
            new Uri("/projects", UriKind.Relative), new { key = "ROLL", name = "Rollback" });
        Assert.Equal(HttpStatusCode.Created, project.StatusCode);

        Guid projectId;

        // Put the project's counter at 5 and an issue already at number 5, so the next
        // allocation collides with a row that exists. This is the only way to make the
        // allocator hand out a duplicate without shipping a broken allocator to prove it.
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<GotIssuesDbContext>();
            var record = await db.Projects.SingleAsync(p => p.Key == "ROLL");
            projectId = record.Id;
            record.NextIssueNumber = 5;
            db.Issues.Add(new IssueRecord
            {
                Id = Guid.NewGuid(),
                ProjectId = projectId,
                Number = 5,
                Title = "Already holds number five",
                CreatedAt = DateTimeOffset.UtcNow,
            });
            await db.SaveChangesAsync();
        }

        using var member = _factory.CreateClient();
        member.DefaultRequestHeaders.Add(TestAuthHandler.HeaderName, "rollback-member");
        member.DefaultRequestHeaders.Add(TestAuthHandler.RoleHeaderName, "member");

        var response = await member.PostAsJsonAsync(
            new Uri("/projects/ROLL/issues", UriKind.Relative), new { title = "Collides" });

        // The index refused the duplicate. Without it, this request would have returned
        // 201 and the project would hold two issues numbered 5 — a silent corruption of
        // the identity the whole ticket is about, rather than a loud failure.
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<GotIssuesDbContext>();

            // The number was returned, not burned: the counter increment was inside the
            // same transaction as the insert, so it rolled back with it. A sequence
            // would have left the counter at 6 and skipped number 5 forever.
            var counter = await db.Projects.AsNoTracking()
                .Where(p => p.Id == projectId)
                .Select(p => p.NextIssueNumber)
                .SingleAsync();
            Assert.Equal(5, counter);

            // And nothing was written.
            Assert.Equal(1, await db.Issues.CountAsync(i => i.ProjectId == projectId));
        }
    }

    [Fact]
    public async Task A_project_that_has_exhausted_its_numbers_is_refused_rather_than_given_an_unusable_key()
    {
        // Found in acceptance (claude-qa-8f52) as a spec inconsistency: `number`
        // declared no maximum while `key` allows nine digits, so a create above
        // 999,999,999 returned 201 with a key violating the pattern this API
        // publishes — and unreadable through GET, which rejects it with 400. The same
        // defect as the GOTI-0 backfill, arriving from the other end of the range.
        //
        // Reachable only by seeding the counter, which is exactly how the two
        // properties above are proved: a billion issues is not a test fixture.
        using var admin = _factory.CreateClient();
        admin.DefaultRequestHeaders.Add(TestAuthHandler.HeaderName, "exhausted-admin");
        admin.DefaultRequestHeaders.Add(TestAuthHandler.RoleHeaderName, "admin");

        Assert.Equal(
            HttpStatusCode.Created,
            (await admin.PostAsJsonAsync(
                new Uri("/projects", UriKind.Relative),
                new { key = "FULL", name = "Exhausted" })).StatusCode);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<GotIssuesDbContext>();
            var project = await db.Projects.SingleAsync(p => p.Key == "FULL");
            project.NextIssueNumber = 999_999_999;
            await db.SaveChangesAsync();
        }

        using var member = _factory.CreateClient();
        member.DefaultRequestHeaders.Add(TestAuthHandler.HeaderName, "exhausted-member");
        member.DefaultRequestHeaders.Add(TestAuthHandler.RoleHeaderName, "member");

        // The last number a key can express is still allocated normally.
        var last = await member.PostAsJsonAsync(
            new Uri("/projects/FULL/issues", UriKind.Relative), new { title = "The last one" });
        Assert.Equal(HttpStatusCode.Created, last.StatusCode);

        using (var document = JsonDocument.Parse(await last.Content.ReadAsStringAsync()))
        {
            Assert.Equal("FULL-999999999", document.RootElement.GetProperty("key").GetString());
        }

        // And it is readable — which carries a second, less obvious job.
        //
        // **This assertion is load-bearing for constant-versus-pattern agreement.**
        // `MaximumIssueNumber` lives in the controller and the nine-digit limit lives
        // in `spec/openapi.yaml`'s key pattern; nothing declares them equal. Narrow the
        // pattern to eight digits and leave the constant alone — the drift direction
        // that reintroduces the defect this test was written for — and *this line* goes
        // red with `Expected: OK, Actual: BadRequest`. Measured by `claude-rev-5c14`.
        //
        // **T-0022 must not lose it.** A layering refactor is exactly the change that
        // deletes an assertion whose second purpose nobody wrote down, at the moment
        // the constant moves into the domain.
        Assert.Equal(
            HttpStatusCode.OK,
            (await member.GetAsync(new Uri("/issues/FULL-999999999", UriKind.Relative))).StatusCode);

        // One past it is refused, rather than issued a key the contract rejects.
        var overflow = await member.PostAsJsonAsync(
            new Uri("/projects/FULL/issues", UriKind.Relative), new { title = "One too many" });

        Assert.Equal(HttpStatusCode.Conflict, overflow.StatusCode);
        Assert.Equal("application/problem+json", overflow.Content.Headers.ContentType?.MediaType);

        using var scope2 = _factory.Services.CreateScope();
        var db2 = scope2.ServiceProvider.GetRequiredService<GotIssuesDbContext>();

        // Nothing written, and the number returned rather than burned - the refusal
        // happens inside the same transaction as the allocation.
        Assert.Equal(1, await db2.Issues.CountAsync(i => i.Project.Key == "FULL"));
        Assert.Equal(
            1_000_000_000,
            await db2.Projects.Where(p => p.Key == "FULL").Select(p => p.NextIssueNumber).SingleAsync());
    }
}
