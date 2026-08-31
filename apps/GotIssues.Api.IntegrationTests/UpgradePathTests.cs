using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using GotIssues.Api.Data;
using GotIssues.Api.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace GotIssues.Api.IntegrationTests;

/// <summary>
/// Behaviour of a database that existed before a migration, rather than one created by it.
///
/// <para>
/// Every other test in this suite migrates an empty schema, so nothing any of them do
/// depends on what a migration does to <em>existing rows</em>. That blind spot hid a
/// real defect: the column backfilling the issue counter defaulted to 0, so the first
/// issue in any project created before T-0005 would have been numbered 0 — a key of
/// <c>GOTI-0</c>, which violates the pattern and the <c>minimum: 1</c> the contract
/// declares, and which the read path then refuses with 400. Found in review by
/// reverting a live stack and running the real migrator against it.
/// </para>
/// </summary>
[Collection(PostgresFixtureDefinition.Name)]
public sealed class UpgradePathTests(PostgresContainerFixture postgres)
{
    /// <summary>The last migration before issues existed.</summary>
    private const string BeforeIssues = "20260831162646_AddProjectsDropPlaceholder";

    [Fact]
    public async Task A_project_that_predates_the_issues_migration_numbers_its_first_issue_one()
    {
        var connectionString = await postgres.CreateDatabaseAsync();
        using var factory = new ApiFactory(connectionString, withTestAuthentication: true);

        // Bring the database up to the schema as it stood before this ticket, and put a
        // project in it — the state every existing deployment is in.
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<GotIssuesDbContext>();
            var migrator = db.Database.GetService<Microsoft.EntityFrameworkCore.Migrations.IMigrator>();
            await migrator.MigrateAsync(BeforeIssues);

            await db.Database.ExecuteSqlRawAsync(
                """
                INSERT INTO projects ("Id", "Key", "Name", "CreatedAt")
                VALUES (gen_random_uuid(), 'OLD', 'Predates issues', now())
                """);
        }

        // Now upgrade, exactly as the compose stack's migration step does.
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<GotIssuesDbContext>();
            await db.Database.MigrateAsync();
        }

        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.HeaderName, "upgrade-1");
        client.DefaultRequestHeaders.Add(TestAuthHandler.RoleHeaderName, "member");

        var created = await client.PostAsJsonAsync(
            new Uri("/projects/OLD/issues", UriKind.Relative), new { title = "First after upgrade" });

        Assert.Equal(HttpStatusCode.Created, created.StatusCode);

        using var document = JsonDocument.Parse(await created.Content.ReadAsStringAsync());
        var body = document.RootElement;

        Assert.Equal(1, body.GetProperty("number").GetInt32());
        Assert.Equal("OLD-1", body.GetProperty("key").GetString());

        // And it is readable, which OLD-0 would not have been: the contract's key
        // pattern rejects a zero, so the issue would have existed and been unreachable
        // through the only declared read path.
        var read = await client.GetAsync(new Uri("/issues/OLD-1", UriKind.Relative));
        Assert.Equal(HttpStatusCode.OK, read.StatusCode);
    }
}
